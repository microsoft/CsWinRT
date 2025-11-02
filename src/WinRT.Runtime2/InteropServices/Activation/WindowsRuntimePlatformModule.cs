// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A Windows Runtime module handling manifest-based activation, via <c>RoGetActivationFactory</c>.
/// </summary>
internal sealed unsafe class WindowsRuntimePlatformModule
{
    /// <summary>
    /// The lazy-loaded, globally-cached <see cref="WindowsRuntimePlatformModule"/> instance.
    /// </summary>
    private static volatile WindowsRuntimePlatformModule? instance;

    /// <summary>
    /// The cookie to decrement the MTA usage count when the current instance is finalized.
    /// </summary>
    private readonly CO_MTA_USAGE_COOKIE _mtaCookie;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimePlatformModule"/> instance.
    /// </summary>
    public WindowsRuntimePlatformModule()
    {
        CO_MTA_USAGE_COOKIE mtaCookie;

        HRESULT hresult = WindowsRuntimeImports.CoIncrementMTAUsage(&mtaCookie);

        // If we failed to increment the MTA usage, we want to suppress the finalizer.
        // We only need to run that if we actually have a cookie to use to decrement.
        if (!WellKnownErrorCodes.Succeeded(hresult))
        {
            GC.SuppressFinalize(this);

            Marshal.ThrowExceptionForHR(hresult);
        }

        _mtaCookie = mtaCookie;
    }

    /// <summary>
    /// Finalizes the current <see cref="WindowsRuntimePlatformModule"/> instance.
    /// </summary>
    ~WindowsRuntimePlatformModule()
    {
        // This finalizer won't run for the globally cached instance, as the static field will keep
        // it alive. However, we might have instantiated multiple instances if more than one thread
        // tried to concurrently access the static property the first time. So this finalizer still
        // helps remove those unwanted MTA usage increments for transient, discarded instances.
        WindowsRuntimeImports.CoDecrementMTAUsage(_mtaCookie).Assert();
    }

    /// <summary>
    /// Gets the globally-cached <see cref="WindowsRuntimePlatformModule"/> instance.
    /// </summary>
    public static WindowsRuntimePlatformModule Instance
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static WindowsRuntimePlatformModule InitializeInstance()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref instance,
                    value: new WindowsRuntimePlatformModule(),
                    comparand: null);

                return instance;
            }

            return instance ?? InitializeInstance();
        }
    }

    /// <summary>
    /// Tries to activate a Windows Runtime type from the current module.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name for the type to activate.</param>
    /// <param name="iid">The IID of the interface to retrieve for the given class.</param>
    /// <param name="activationFactory">The resulting <see cref="WindowsRuntimeObjectReference"/> instance.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    public HRESULT GetActivationFactory(string runtimeClassName, in Guid iid, out WindowsRuntimeObjectReference? activationFactory)
    {
        HRESULT hresult = GetActivationFactoryUnsafe(runtimeClassName, in iid, out void* activationFactoryPtr);

        // If the operation succeeded, wrap the activation factory into a managed reference
        activationFactory = hresult.Succeeded()
            ? WindowsRuntimeObjectReference.AttachUnsafe(ref activationFactoryPtr, in WellKnownWindowsInterfaceIIDs.IID_IActivationFactory)
            : null;

        return hresult;
    }

    /// <summary>
    /// Tries to activate a Windows Runtime type from the current module.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name for the type to activate.</param>
    /// <param name="iid">The IID of the interface to retrieve for the given class.</param>
    /// <param name="activationFactory">The resulting activation factory instance.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    public HRESULT GetActivationFactoryUnsafe(string runtimeClassName, in Guid iid, out void* activationFactory)
    {
        // Just delegate to the platform to handle manifest-based activation
        fixed (char* runtimeClassIdPtr = runtimeClassName)
        fixed (Guid* riid = &iid)
        fixed (void** activationFactoryPtr = &activationFactory)
        {
            HStringMarshaller.ConvertToUnmanagedUnsafe(runtimeClassIdPtr, runtimeClassName.Length, out HStringReference reference);

            return WindowsRuntimeImports.RoGetActivationFactory(reference.HString, riid, activationFactoryPtr);
        }
    }
}

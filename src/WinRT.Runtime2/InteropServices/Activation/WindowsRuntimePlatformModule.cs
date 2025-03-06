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
    private static volatile WindowsRuntimePlatformModule? _instance;

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

        Marshal.ThrowExceptionForHR(hresult);

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
        HRESULT hresult = WindowsRuntimeImports.CoDecrementMTAUsage(_mtaCookie);

        Marshal.ThrowExceptionForHR(hresult);
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
                    location1: ref _instance,
                    value: new WindowsRuntimePlatformModule(),
                    comparand: null);

                return _instance;
            }

            return _instance ?? InitializeInstance();
        }
    }

    /// <summary>
    /// Tries to activate a Windows Runtime type from the current module.
    /// </summary>
    /// <param name="runtimeClassId">The runtime class name for the type to activate.</param>
    /// <param name="iid">The IID of the interface to retrieve for the given class.</param>
    /// <param name="activationFactory">The resulting <see cref="WindowsRuntimeObjectReference"/> instance.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    public static HRESULT GetActivationFactory(string runtimeClassId, in Guid iid, out WindowsRuntimeObjectReference? activationFactory)
    {
        HRESULT hresult = GetActivationFactoryUnsafe(runtimeClassId, in iid, out void* activationFactoryPtr);

        // If the operation succeeded, wrap the activation factory into a managed reference
        activationFactory = WellKnownErrorCodes.Succeeded(hresult)
            ? WindowsRuntimeObjectReference.AttachUnsafe(ref activationFactoryPtr, in WellKnownInterfaceIds.IID_IActivationFactory)
            : null;

        return hresult;
    }

    /// <summary>
    /// Tries to activate a Windows Runtime type from the current module.
    /// </summary>
    /// <param name="runtimeClassId">The runtime class name for the type to activate.</param>
    /// <param name="iid">The IID of the interface to retrieve for the given class.</param>
    /// <param name="activationFactory">The resulting activation factory instance.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    public static HRESULT GetActivationFactoryUnsafe(string runtimeClassId, in Guid iid, out void* activationFactory)
    {
        // Ensure COM is initialized
        WindowsRuntimePlatformModule module = Instance;

        // Just delegate to the platform to handle manifest-based activation
        fixed (char* runtimeClassIdPtr = runtimeClassId)
        fixed (Guid* riid = &iid)
        fixed (void** activationFactoryPtr = &activationFactory)
        {
            HStringMarshaller.ConvertToUnmanagedUnsafe(runtimeClassIdPtr, runtimeClassId.Length, out HStringReference reference);

            return WindowsRuntimeImports.RoGetActivationFactory(reference.HString, riid, activationFactoryPtr);
        }
    }
}

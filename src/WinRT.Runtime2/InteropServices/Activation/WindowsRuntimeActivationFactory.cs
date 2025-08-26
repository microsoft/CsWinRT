// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable CS1573, IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides support for retrieving activation factories for Windows Runtime types.
/// </summary>
public static unsafe class WindowsRuntimeActivationFactory
{
    /// <summary>
    /// The registered hook into activation to hook/mock activation of Windows Runtime types.
    /// </summary>
    private static WindowsRuntimeActivationHandler? activationHandler;

    /// <summary>
    /// Set the <see cref="WindowsRuntimeActivationHandler"/> callback for activating Windows Runtime types.
    /// This handler is the first attempt to activate any Windows Runtime types for the entire application.
    /// </summary>
    /// <param name="activationHandler">The <see cref="WindowsRuntimeActivationHandler"/> handler to set.</param>
    /// <remarks>
    /// This handler can only be set once. Trying to register a second handler fails with <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="activationHandler"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a resolver has already been set.</exception>
    public static void SetWindowsRuntimeActivationHandler(WindowsRuntimeActivationHandler activationHandler)
    {
        ArgumentNullException.ThrowIfNull(activationHandler);

        // Set the new handler, if this is the first time the method is called
        WindowsRuntimeActivationHandler? previousActivationHandler = Interlocked.CompareExchange(
            location1: ref WindowsRuntimeActivationFactory.activationHandler,
            value: activationHandler,
            comparand: null);

        // We only allow setting this once
        if (previousActivationHandler is not null)
        {
            throw new InvalidOperationException("The activation handler has already been set (it can only be set once).");
        }
    }

    /// <summary>
    /// Gets the activation factory for a Windows Runtime type with the specified runtime class name.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name of the type to activate (ie. the fully qualified type name).</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type.</returns>
    /// <remarks>
    /// <para>
    /// This method will try to activate the target type as follows:
    /// <list type="bullet">
    ///   <item>If <see cref="SetWindowsRuntimeActivationHandler"/> has been called, the registered activation handler will be used first.</item>
    ///   <item>Otherwise, <a href="https://learn.microsoft.com/windows/win32/api/roapi/nf-roapi-rogetactivationfactory"><c>RoGetActivationFactory</c></a> will be used.</item>
    ///   <item>Otherwise, the manifest-free fallback path will be used to try to resolve the target .dll to load based on <paramref name="runtimeClassName"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Note that if <c>CsWinRTEnableManifestFreeActivation</c> is disabled, the manifest-free fallback path will not be used. In that
    /// scenario, failure to activate the type after the first two steps will result in failure to resolve the activation factory.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="runtimeClassName"/> is not registered, <c>CsWinRTEnableManifestFreeActivation</c> is disabled, and <c>CsWinRTManifestFreeActivationReportOriginalException</c> is not set.</exception>
    /// <exception cref="Exception">Thrown for any failure to activate the specified type (the exact exception type might be a derived type).</exception>
    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName)
    {
        void* activationFactory = GetActivationFactoryUnsafe(runtimeClassName);

        return WindowsRuntimeObjectReference.AttachUnsafe(ref activationFactory, in WellKnownInterfaceIds.IID_IActivationFactory)!;
    }

    /// <param name="iid">The IID of the interface pointer (from the resolved activation factory) to wrap in the returned object reference.</param>
    /// <inheritdoc cref="GetActivationFactory(string)"/>
    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName, in Guid iid)
    {
        void* activationFactory = GetActivationFactoryUnsafe(runtimeClassName, in iid);

        return WindowsRuntimeObjectReference.AttachUnsafe(ref activationFactory, in WellKnownInterfaceIds.IID_IActivationFactory)!;
    }

    /// <returns>A pointer to the activation factory for the specified Windows Runtime type.</returns>
    /// <inheritdoc cref="GetActivationFactory(string)"/>
    public static void* GetActivationFactoryUnsafe(string runtimeClassName)
    {
        HRESULT hresult = GetActivationFactoryFromAnySourceUnsafe(runtimeClassName, in *(Guid*)null, out void* activationFactory);

        ThrowIfActivationFailed(runtimeClassName, hresult);

        return activationFactory;
    }

    /// <param name="iid">The IID of the interface pointer (from the resolved activation factory) to return.</param>
    /// <returns>A pointer to the activation factory for the specified Windows Runtime type.</returns>
    /// <inheritdoc cref="GetActivationFactory(string)"/>
    public static void* GetActivationFactoryUnsafe(string runtimeClassName, in Guid iid)
    {
        HRESULT hresult = GetActivationFactoryFromAnySourceUnsafe(runtimeClassName, in iid, out void* activationFactory);

        ThrowIfActivationFailed(runtimeClassName, hresult);

        return activationFactory;
    }

    /// <summary>
    /// Tries to get the activation factory for a Windows Runtime type with the specified runtime class name.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name of the type to activate (ie. the fully qualified type name).</param>
    /// <param name="activationFactory">A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="activationFactory"/> was successfully retrieved.</returns>
    /// <remarks><inheritdoc cref="GetActivationFactory(string)" path="/remarks/node()"/></remarks>
    public static bool TryGetActivationFactory(string runtimeClassName, [NotNullWhen(true)] out WindowsRuntimeObjectReference? activationFactory)
    {
        if (!TryGetActivationFactoryUnsafe(runtimeClassName, out void* activationFactoryPtr))
        {
            activationFactory = null;

            return false;
        }

        activationFactory = WindowsRuntimeObjectReference.AttachUnsafe(ref activationFactoryPtr, in WellKnownInterfaceIds.IID_IActivationFactory)!;

        return true;
    }

    /// <param name="iid">The IID of the interface pointer (from the resolved activation factory) to wrap in the returned object reference.</param>
    /// <inheritdoc cref="TryGetActivationFactory(string, out WindowsRuntimeObjectReference?)"/>
    public static bool TryGetActivationFactory(string runtimeClassName, in Guid iid, [NotNullWhen(true)] out WindowsRuntimeObjectReference? activationFactory)
    {
        if (!TryGetActivationFactoryUnsafe(runtimeClassName, in iid, out void* activationFactoryPtr))
        {
            activationFactory = null;

            return false;
        }

        activationFactory = WindowsRuntimeObjectReference.AttachUnsafe(ref activationFactoryPtr, in iid)!;

        return true;
    }

    /// <param name="activationFactory">A pointer to the activation factory for the specified Windows Runtime type, if successfully retrieved.</param>
    /// <inheritdoc cref="TryGetActivationFactory(string, out WindowsRuntimeObjectReference?)"/>
    public static bool TryGetActivationFactoryUnsafe(string runtimeClassName, out void* activationFactory)
    {
        HRESULT hresult = GetActivationFactoryFromAnySourceUnsafe(runtimeClassName, in *(Guid*)null, out activationFactory);

        return WellKnownErrorCodes.Succeeded(hresult);
    }

    /// <param name="iid">The IID of the interface pointer (from the resolved activation factory) to wrap in the returned object reference.</param>
    /// <param name="activationFactory">A pointer to the activation factory for the specified Windows Runtime type, if successfully retrieved.</param>
    /// <inheritdoc cref="TryGetActivationFactory(string, out WindowsRuntimeObjectReference?)"/>
    public static bool TryGetActivationFactoryUnsafe(string runtimeClassName, in Guid iid, out void* activationFactory)
    {
        HRESULT hresult = GetActivationFactoryFromAnySourceUnsafe(runtimeClassName, in iid, out activationFactory);

        return WellKnownErrorCodes.Succeeded(hresult);
    }

    /// <inheritdoc cref="TryGetActivationFactoryUnsafe(string, in Guid, out void*)"/>
    private static HRESULT GetActivationFactoryFromAnySourceUnsafe(string runtimeClassName, in Guid iid, out void* activationFactory)
    {
        // If we have no input IID, it means that callers just expect whatever the default interface pointer
        // returned by each attempted API is. For the activation handler and 'RoGetActivationFactory', we
        // still need to pass an IID in all cases. Unless specified, this should be 'IID_IActivationFactory'.
        ref readonly Guid defaultIid = ref Unsafe.IsNullRef(in iid)
            ? ref WellKnownInterfaceIds.IID_IActivationFactory
            : ref iid;

        // Attempt activation with the activation handler, if any (1)
        HRESULT hresult = GetActivationFactoryFromActivationHandlerUnsafe(runtimeClassName, in defaultIid, out activationFactory);

        if (WellKnownErrorCodes.Succeeded(hresult))
        {
            return hresult;
        }

        // Attempt manifest-based activation using 'RoGetActivationFactory' (2)
        hresult = WindowsRuntimePlatformModule.Instance.GetActivationFactoryUnsafe(runtimeClassName, in defaultIid, out activationFactory);

        if (WellKnownErrorCodes.Succeeded(hresult))
        {
            return hresult;
        }

        // If manifest free activation is enabled, we must stop here.
        // At this point the 'HRESULT' should be 'REGDB_E_CLASSNOTREG'.
        if (!WindowsRuntimeFeatureSwitches.EnableManifestFreeActivation)
        {
            return hresult;
        }

        // Attempt manifest-free activation as a last resort (3)
        return Unsafe.IsNullRef(in iid)
            ? GetActivationFactoryFromDllUnsafe(runtimeClassName, hresult, out activationFactory)
            : GetActivationFactoryFromDllUnsafe(runtimeClassName, in iid, hresult, out activationFactory);
    }

    /// <summary>Tries to get the activation factory for a Windows Runtime type with the specified runtime class name, using only the registered activation handler, if present.</summary>
    /// <inheritdoc cref="TryGetActivationFactoryUnsafe(string, in Guid, out void*)"/>
    private static HRESULT GetActivationFactoryFromActivationHandlerUnsafe(string runtimeClassName, in Guid iid, out void* activationFactory)
    {
        // Check the activation handler first, immediately stop if we don't have one
        if (WindowsRuntimeActivationFactory.activationHandler is not { } activationHandler)
        {
            activationFactory = null;

            return WellKnownErrorCodes.E_ABORT;
        }

        // Delegate to the registered handler (it should not throw an exception)
        return activationHandler(runtimeClassName, in iid, out activationFactory);
    }

    /// <summary>Tries to get the activation factory for a Windows Runtime type with the specified runtime class name, using only manifest-free activation.</summary>
    /// <param name="hresult">The previous <c>HRESULT</c> from attempts to get the activation factory using manifest-based activation, for the same type.</param>
    /// <inheritdoc cref="TryGetActivationFactoryUnsafe(string, out void*)"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static HRESULT GetActivationFactoryFromDllUnsafe(
        string runtimeClassName,
        HRESULT hresult,
        out void* activationFactory)
    {
        activationFactory = null;

        ReadOnlySpan<char> moduleName = runtimeClassName;

        // This method is performing manifest-free Windows Runtime activation. That is, we don't have
        // information in the APPX manifest that indicates the implementation .dll for each registered
        // runtime class (which is how 'RoGetActivationFactory' works). Instead, the logic used here is
        // that the runtime class name is used to determine the .dll name to load. The assumption is that
        // the .dll name will have some common prefix with the runtime class name.
        //
        // For instance, consider this case:
        //   - Runtime class name: 'Microsoft.UI.Xaml.Controls.Button'
        //   - Implementation .dll: 'Microsoft.UI.Xaml.dll'
        //
        // What we do is we iteratively try to find the last '.' delimiter, trim the rest, and then try to
        // load a .dll with that name. If that fails, we go to the previous segment, and keep repeating.
        //
        // In the example above, we'd try to load the following .dll-s:
        //   - 'Microsoft.UI.Xaml.Controls.dll' (fails)
        //   - 'Microsoft.UI.Xaml.dll' (succeeds)
        //
        // This is a fallback mechanism, and it is not as efficient as using 'RoGetActivationFactory'.
        // Consumers should prefer manifest-based Windows Runtime activation whenever possible. The
        // main reason this approach also exists is to provide a way for unpackaged apps to also work.
        while (!WellKnownErrorCodes.Succeeded(hresult))
        {
            int lastSegmentIndex = moduleName.LastIndexOf('.');

            // We have run out of segments, so we have to give up
            if (lastSegmentIndex <= 0)
            {
                break;
            }

            moduleName = moduleName[..lastSegmentIndex];

            // The filename for the .dll module is just the current namespace plus '.dll'.
            // We are fine with this 'string' allocation, we need it to be null-terminated.
            string fileName = string.Concat(moduleName, ".dll");

            // Try to load with this new .dll attempt, and if it succeeds, we're done
            if (WindowsRuntimeDllModule.TryLoad(fileName, out WindowsRuntimeDllModule? dllModule))
            {
                hresult = dllModule.GetActivationFactoryUnsafe(runtimeClassName, out activationFactory);
            }
        }

        return hresult;
    }

    /// <summary>Tries to get the activation factory for a Windows Runtime type with the specified runtime class name, using only manifest-free activation.</summary>
    /// <param name="hresult">The previous <c>HRESULT</c> from attempts to get the activation factory using manifest-based activation, for the same type.</param>
    /// <inheritdoc cref="TryGetActivationFactoryUnsafe(string, in Guid, out void*)"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static HRESULT GetActivationFactoryFromDllUnsafe(
        string runtimeClassName,
        in Guid iid,
        HRESULT hresult,
        out void* activationFactory)
    {
        activationFactory = null;

        // First, try to get the activation factory with the same logic for when we don't have an IID
        hresult = GetActivationFactoryFromDllUnsafe(runtimeClassName, hresult, out void* activationFactoryUnknown);

        // If the activation failed, we can't do anything else, just return that 'HRESULT'
        if (!WellKnownErrorCodes.Succeeded(hresult))
        {
            return hresult;
        }

        // At this point we have an activation factory, and we need to 'QueryInterface' for the exact IID
        hresult = IUnknownVftbl.QueryInterfaceUnsafe(activationFactoryUnknown, in iid, out activationFactory);

        // We can always release the original activation factory at this point
        _ = IUnknownVftbl.ReleaseUnsafe(activationFactoryUnknown);

        return hresult;
    }

    /// <summary>
    /// Throws an exception if getting the activation factory failed.
    /// </summary>
    /// <param name="runtimeClassName"><inheritdoc cref="GetActivationFactory(string)" path="/param[@name='runtimeClassName']/node()"/></param>
    /// <param name="hresult">The <c>HRESULT</c> for the activation factory retrieval result.</param>
    /// <exception cref="NotSupportedException">Thrown if activation failed due to type not being registered, <c>CsWinRTEnableManifestFreeActivation</c> is disabled, and <c>CsWinRTManifestFreeActivationReportOriginalException</c> is not set.</exception>
    /// <exception cref="Exception">Thrown for any other failure case.</exception>
    [StackTraceHidden]
    private static void ThrowIfActivationFailed(string runtimeClassName, HRESULT hresult)
    {
        // Throws the appropriate exception depending on the 'HRESULT' we got.
        // If it is 'REGDB_E_CLASSNOTREG', we also include a detailed message.
        static Exception GetException(string runtimeClassName, HRESULT hresult)
        {
            Exception exception = Marshal.GetExceptionForHR(hresult)!;

            // Throw the exception directly, if the user requested to preserve it
            if (WindowsRuntimeFeatureSwitches.ManifestFreeActivationReportOriginalException)
            {
                return exception;
            }

            // Special case 'REGDB_E_CLASSNOTREG' to provide a better exception message
            if (hresult == WellKnownErrorCodes.REGDB_E_CLASSNOTREG)
            {
                return new NotSupportedException(
                    $"Failed to activate type with runtime class name '{runtimeClassName}' with 'RoGetActivationFactory' (it returned 0x80040154, ie. 'REGDB_E_CLASSNOTREG'). Make sure to add the activatable class id for the type " +
                    "to the APPX manifest, or enable the manifest free activation fallback path by setting the 'CsWinRTEnableManifestFreeActivation' property (note: the fallback path incurs a performance hit).", exception);
            }

            return exception;
        }

        // Throw helper to improve codegen. We can't just thow in the parent, as we have a few conditional
        // branches which make it unlikely the JIT would classify the whole method as a throw helper.
        [DoesNotReturn]
        [StackTraceHidden]
        static void ThrowException(string runtimeClassName, HRESULT hresult) => throw GetException(runtimeClassName, hresult);

        // If the activation failed, throw the appropriate exception
        if (!WellKnownErrorCodes.Succeeded(hresult))
        {
            ThrowException(runtimeClassName, hresult);
        }
    }
}

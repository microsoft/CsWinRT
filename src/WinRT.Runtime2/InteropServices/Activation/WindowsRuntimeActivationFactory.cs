// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides support for activating Windows Runtime types.
/// </summary>
public static unsafe class WindowsRuntimeActivationFactory
{
    /// <summary>
    /// This provides a hook into activation to hook/mock activation of Windows Runtime types.
    /// </summary>
    public static WindowsRuntimeActivationHandler? ActivationHandler { get; set; }

    public static bool TryGetActivationFactory(string runtimeClassName, [NotNullWhen(true)] out WindowsRuntimeObjectReference activationFactory)
    {
    }

    public static bool TryGetActivationFactory(string runtimeClassName, in Guid iid, [NotNullWhen(true)] out WindowsRuntimeObjectReference activationFactory)
    {
    }

    public static bool TryGetActivationFactoryUnsafe(string runtimeClassName, out void* activationFactory)
    {
    }

    public static bool TryGetActivationFactoryUnsafe(string runtimeClassName, in Guid iid, out void* activationFactory)
    {
    }

    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName)
    {
    }

    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName, in Guid iid)
    {
    }

    public static void* GetActivationFactoryUnsafe(string runtimeClassName)
    {
    }

    public static void* GetActivationFactoryUnsafe(string runtimeClassName, in Guid iid)
    {
    }

    /// <summary>
    /// Activates a WinRT type with the specified runtime class name.
    /// </summary>
    /// <param name="typeName">The ID of the activatable class (the fully qualified type name).</param>
    /// <returns>An <see cref="IObjectReference"/> instance wrapping an instance of the activated WinRT type.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="typeName"/> is not registered, <c>CsWinRTEnableManifestFreeActivation</c> is disabled, and <c>CsWinRTManifestFreeActivationReportOriginalException</c> is not set.</exception>
    /// <exception cref="Exception">Thrown for any failure to activate the specified type (the exact exception type might be a derived type).</exception>
    /// <remarks>
    /// This method will try to activate the target type as follows:
    /// <list type="bullet">
    ///   <item>If <see cref="ActivationHandler"/> is set, it will be used first.</item>
    ///   <item>Otherwise, <a href="https://learn.microsoft.com/windows/win32/api/roapi/nf-roapi-rogetactivationfactory"><c>RoGetActivationFactory</c></a> will be used.</item>
    ///   <item>Otherwise, the manifest-free fallback path will be used to try to resolve the target .dll to load based on <paramref name="typeName"/>.</item>
    /// </list>
    /// </remarks>
    public static IObjectReference GetActivationFactory(string typeName)
    {
#if NET
        // Check hook first
        if (ActivationHandler != null)
        {
            var factoryFromhandler = GetFromActivationHandler(typeName, IID.IID_IActivationFactory);
            if (factoryFromhandler != null)
            {
                return factoryFromhandler;
            }
        }
#endif

        // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
        int hr;
        ObjectReference<IUnknownVftbl> factory;
        (factory, hr) = WinRTModule.GetActivationFactory<IUnknownVftbl>(typeName, IID.IID_IActivationFactory);
        if (factory != null)
        {
            return factory;
        }

        ThrowIfClassNotRegisteredAndManifestFreeActivationDisabled(typeName, hr);

        return ManifestFreeGet(typeName, hr);
    }

    /// <summary>
    /// Activates a WinRT type with the specified runtime class name and interface ID.
    /// </summary>
    /// <param name="typeName">The ID of the activatable class (the fully qualified type name).</param>
    /// <param name="iid">The interface ID to use to dispatch the activated type.</param>
    /// <returns>An <see cref="IObjectReference"/> instance wrapping an instance of the activated WinRT type, dispatched with the specified interface ID.</returns>
    /// <inheritdoc cref="Get(string)"/>
#if NET
    public static IObjectReference Get(string typeName, Guid iid)
#else
    public static ObjectReference<I> Get<I>(string typeName, Guid iid)
#endif
    {
#if NET
        // Check hook first
        if (ActivationHandler != null)
        {
            var factoryFromhandler = GetFromActivationHandler(typeName, iid);
            if (factoryFromhandler != null)
            {
                return factoryFromhandler;
            }
        }
#endif

        // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
        int hr;
#if NET
        ObjectReference<IUnknownVftbl> factory;
        (factory, hr) = WinRTModule.GetActivationFactory<IUnknownVftbl>(typeName, iid);
        if (factory != null)
        {
            return factory;
        }

        ThrowIfClassNotRegisteredAndManifestFreeActivationDisabled(typeName, hr);

        return ManifestFreeGet(typeName, iid, hr);
#else
        ObjectReference<I> factory;
        (factory, hr) = WinRTModule.GetActivationFactory<I>(typeName, iid);
        if (factory != null)
        {
            return factory;
        }

        ThrowIfClassNotRegisteredAndManifestFreeActivationDisabled(typeName, hr);

        return ManifestFreeGet<I>(typeName, iid, hr);
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IObjectReference ManifestFreeGet(string typeName, int hr)
    {
        var moduleName = typeName;
        while (true)
        {
            var lastSegment = moduleName.LastIndexOf(".", StringComparison.Ordinal);
            if (lastSegment <= 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            moduleName = moduleName.Remove(lastSegment);

            DllModule? module = null;
            if (DllModule.TryLoad(moduleName + ".dll", out module))
            {
                (ObjectReference<IUnknownVftbl> factory, hr) = module.GetActivationFactory(typeName);
                if (factory != null)
                {
                    return factory;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#if NET
    private static IObjectReference ManifestFreeGet(string typeName, Guid iid, int hr)
#else
    private static ObjectReference<I> ManifestFreeGet<I>(string typeName, Guid iid, int hr)
#endif
    {
        var moduleName = typeName;
        while (true)
        {
            var lastSegment = moduleName.LastIndexOf(".", StringComparison.Ordinal);
            if (lastSegment <= 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            moduleName = moduleName.Remove(lastSegment);

            if (DllModule.TryLoad(moduleName + ".dll", out DllModule module))
            {
                ObjectReference<IUnknownVftbl> activationFactory;
                (activationFactory, hr) = module.GetActivationFactory(typeName);
                if (activationFactory != null)
                {
                    using (activationFactory)
                    {
#if NET
                        return activationFactory.As<IUnknownVftbl>(iid);
#else
                        return activationFactory.As<I>(iid);
#endif
                    }
                }
            }
        }
    }

    private static void ThrowIfClassNotRegisteredAndManifestFreeActivationDisabled(string runtimeClassName, HRESULT hresult)
    {
        // If manifest free activation is enabled, we never throw here.
        // Callers will always try the fallback path if we didn't succeed.
        if (WindowsRuntimeFeatureSwitches.EnableManifestFreeActivation)
        {
            return;
        }

        // Throw the exception directly, if the user requested to preserve it
        if (WindowsRuntimeFeatureSwitches.ManifestFreeActivationReportOriginalException)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Exception GetException(string runtimeClassName, HRESULT hresult)
        {
            Exception exception = Marshal.GetExceptionForHR(hresult)!;

            if (hresult == WellKnownErrorCodes.REGDB_E_CLASSNOTREG)
            {
                throw new NotSupportedException(
                    $"Failed to activate type with runtime class name '{runtimeClassName}' with 'RoGetActivationFactory' (it returned 0x80040154, ie. 'REGDB_E_CLASSNOTREG'). Make sure to add the activatable class id for the type " +
                    "to the APPX manifest, or enable the manifest free activation fallback path by setting the 'CsWinRTEnableManifestFreeActivation' property (note: the fallback path incurs a performance hit).", exception);
            }

            return exception;
        }

        // Explicit throw so the JIT can see it and correctly optimize for always throwing paths.
        // The exception instantiation is in a separate method so that codegen remains separate.
        throw GetException(runtimeClassName, hresult);
    }

#if NET
    private static IObjectReference? GetFromActivationHandler(string typeName, Guid iid)
    {
        var activationHandler = ActivationHandler;
        if (activationHandler != null)
        {
            IntPtr instancePtr = IntPtr.Zero;
            try
            {
                instancePtr = activationHandler(typeName, iid);
                if (instancePtr != IntPtr.Zero)
                {
                    return ObjectReference<IUnknownVftbl>.Attach(ref instancePtr, iid);
                }
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(instancePtr);
            }
        }

        return null;
    }
#endif
}
#nullable restore


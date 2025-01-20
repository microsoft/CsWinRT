﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
    internal unsafe sealed class DllModule
    {
        private static readonly string _currentModuleDirectory = AppContext.BaseDirectory;

        private static readonly Dictionary<string, DllModule> _cache = new Dictionary<string, DllModule>(StringComparer.Ordinal);

        private readonly string _fileName;
        private readonly IntPtr _moduleHandle;
        private readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> _GetActivationFactory;
        private readonly delegate* unmanaged[Stdcall]<int> _CanUnloadNow; // TODO: Eventually periodically call

        public static bool TryLoad(string fileName, out DllModule module)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(fileName, out module))
                {
                    return true;
                }
                else if (TryCreate(fileName, out module))
                {
                    _cache[fileName] = module;
                    return true;
                }
                return false;
            }
        }

        private static bool TryCreate(string fileName, out DllModule module)
        {
            // Explicitly look for module in the same directory as this one, and
            // use altered search path to ensure any dependencies in the same directory are found.
            IntPtr moduleHandle = IntPtr.Zero;
            moduleHandle = Platform.LoadLibraryExW(System.IO.Path.Combine(_currentModuleDirectory, fileName), IntPtr.Zero, /* LOAD_WITH_ALTERED_SEARCH_PATH */ 8);
#if NET
            if (moduleHandle == IntPtr.Zero)
            {
                NativeLibrary.TryLoad(fileName, typeof(DllModule).Assembly, null, out moduleHandle);
            }
#endif
            if (moduleHandle == IntPtr.Zero)
            {
                module = null;
                return false;
            }

            void* getActivationFactory = null;
            ReadOnlySpan<byte> functionName =
#if NET7_0_OR_GREATER || CsWinRT_LANG_11_FEATURES
                "DllGetActivationFactory"u8;
#else
                Encoding.ASCII.GetBytes("DllGetActivationFactory");
#endif
            getActivationFactory = (void*)Platform.TryGetProcAddress(moduleHandle, functionName);
            if (getActivationFactory == null)
            {
                module = null;
                return false;
            }

            module = new DllModule(
                fileName,
                moduleHandle,
                getActivationFactory);
            return true;
        }

        private DllModule(string fileName, IntPtr moduleHandle, void* getActivationFactory)
        {
            _fileName = fileName;
            _moduleHandle = moduleHandle;
            _GetActivationFactory = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)getActivationFactory;

            void* canUnloadNow = null;
            ReadOnlySpan<byte> functionName =
#if NET7_0_OR_GREATER || CsWinRT_LANG_11_FEATURES
                "DllCanUnloadNow"u8;
#else
                Encoding.ASCII.GetBytes("DllCanUnloadNow");
#endif
            canUnloadNow = (void*)Platform.TryGetProcAddress(_moduleHandle, functionName);

            if (canUnloadNow != null)
            {
                _CanUnloadNow = (delegate* unmanaged[Stdcall]<int>)canUnloadNow;
            }
        }

        public (ObjectReference<IUnknownVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
        {
            IntPtr instancePtr = IntPtr.Zero;
            try
            {
                MarshalString.Pinnable __runtimeClassId = new(runtimeClassId);
                fixed (void* ___runtimeClassId = __runtimeClassId)
                {
                    int hr = _GetActivationFactory(MarshalString.GetAbi(ref __runtimeClassId), &instancePtr);
                    if (hr == 0)
                    {
                        var objRef = ObjectReference<IUnknownVftbl>.Attach(ref instancePtr, IID.IID_IActivationFactory);
                        return (objRef, hr);
                    }
                    else
                    {
                        return (null, hr);
                    }
                }
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(instancePtr);
            }
        }

        ~DllModule()
        {
            System.Diagnostics.Debug.Assert(_CanUnloadNow == null || _CanUnloadNow() == 0); // S_OK
            lock (_cache)
            {
                _cache.Remove(_fileName);
            }
            if ((_moduleHandle != IntPtr.Zero) && !Platform.FreeLibrary(_moduleHandle))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }

    internal sealed class WinRTModule
    {
        private static volatile WinRTModule _instance;
        private readonly IntPtr _mtaCookie;
        private static WinRTModule MakeWinRTModule()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _instance, new WinRTModule(), null);
            return _instance;
        }
        public static WinRTModule Instance => _instance ?? MakeWinRTModule();

        public unsafe WinRTModule()
        {
            IntPtr mtaCookie;
            Marshal.ThrowExceptionForHR(Platform.CoIncrementMTAUsage(&mtaCookie));
            _mtaCookie = mtaCookie;
        }

        public static unsafe (ObjectReference<I> obj, int hr) GetActivationFactory<I>(string runtimeClassId, Guid iid)
        {
            var module = Instance; // Ensure COM is initialized
            IntPtr instancePtr = IntPtr.Zero;
            try
            {
                MarshalString.Pinnable __runtimeClassId = new(runtimeClassId);
                fixed (void* ___runtimeClassId = __runtimeClassId)
                {
                    int hr = Platform.RoGetActivationFactory(MarshalString.GetAbi(ref __runtimeClassId), &iid, &instancePtr);
                    if (hr == 0)
                    {
                        var objRef = ObjectReference<I>.Attach(ref instancePtr, iid);
                        return (objRef, hr);
                    }
                    else
                    {
                        return (null, hr);
                    }
                }
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(instancePtr);
            }
        }

        ~WinRTModule()
        {
            Marshal.ThrowExceptionForHR(Platform.CoDecrementMTAUsage(_mtaCookie));
        }
    }

#nullable enable
    /// <summary>
    /// Provides support for activating WinRT types.
    /// </summary>
#if EMBED
    internal
#else
    public
#endif
    static class ActivationFactory
    {
#if NET
        /// <summary>
        /// This provides a hook into activation to hook/mock activation of WinRT types.
        /// </summary>
        public static Func<string, Guid, IntPtr>? ActivationHandler { get; set; }
#endif

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
        public static IObjectReference Get(string typeName)
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

        private static void ThrowIfClassNotRegisteredAndManifestFreeActivationDisabled(string typeName, int hr)
        {
            // If manifest free activation is enabled, we never throw here.
            // Callers will always try the fallback path if we didn't succeed.
            if (FeatureSwitches.EnableManifestFreeActivation)
            {
                return;
            }

            // Throw the exception directly, if the user requested to preserve it
            if (FeatureSwitches.ManifestFreeActivationReportOriginalException)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Exception GetException(string typeName, int hr)
            {
                Exception exception = Marshal.GetExceptionForHR(hr)!;

                if (hr == ExceptionHelpers.REGDB_E_CLASSNOTREG)
                {
                    throw new NotSupportedException(
                        $"Failed to activate type with runtime class name '{typeName}' with 'RoGetActivationFactory' (it returned 0x80040154, ie. 'REGDB_E_CLASSNOTREG'). Make sure to add the activatable class id for the type " +
                        "to the APPX manifest, or enable the manifest free activation fallback path by setting the 'CsWinRTEnableManifestFreeActivation' property (note: the fallback path incurs a performance hit).", exception);
                }

                return exception;
            }

            // Explicit throw so the JIT can see it and correctly optimize for always throwing paths.
            // The exception instantiation is in a separate method so that codegen remains separate.
            throw GetException(typeName, hr);
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
}

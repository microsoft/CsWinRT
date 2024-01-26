// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
                NativeLibrary.TryLoad(fileName, Assembly.GetExecutingAssembly(), null, out moduleHandle);
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
            getActivationFactory = Platform.TryGetProcAddress(moduleHandle, functionName);
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
            canUnloadNow = Platform.TryGetProcAddress(_moduleHandle, functionName);

            if (canUnloadNow != null)
            {
                _CanUnloadNow = (delegate* unmanaged[Stdcall]<int>)canUnloadNow;
            }
        }

        public (ObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
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
                        var objRef = ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr);
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

        public static unsafe (ObjectReference<I> obj, int hr) GetActivationFactory<
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        I>(string runtimeClassId, Guid iid)
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
                        var objRef = ObjectReference<I>.Attach(ref instancePtr);
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

#if EMBED
    internal
#else
    public
#endif
    static class ActivationFactory
    {
        public static IObjectReference Get(string typeName)
        {
            // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
            int hr;
            ObjectReference<IActivationFactoryVftbl> factory;
            (factory, hr) = WinRTModule.GetActivationFactory<IActivationFactoryVftbl>(typeName, InterfaceIIDs.IActivationFactory_IID);
            if (factory != null)
            {
                return factory;
            }

            var moduleName = typeName;
            while (true)
            {
                var lastSegment = moduleName.LastIndexOf(".", StringComparison.Ordinal);
                if (lastSegment <= 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                moduleName = moduleName.Remove(lastSegment);

                DllModule module = null;
                if (DllModule.TryLoad(moduleName + ".dll", out module))
                {
                    (factory, hr) = module.GetActivationFactory(typeName);
                    if (factory != null)
                    {
                        return factory;
                    }
                }
            }
        }

#if NET
        public static IObjectReference Get(string typeName, Guid iid)
#else
        public static ObjectReference<I> Get<I>(string typeName, Guid iid)
#endif
        {
            // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
            int hr;
#if NET
            ObjectReference<IUnknownVftbl> factory;
            (factory, hr) = WinRTModule.GetActivationFactory<IUnknownVftbl>(typeName, iid);
            if (factory != null)
            {
                return factory;
            }
#else
            ObjectReference<I> factory;
            (factory, hr) = WinRTModule.GetActivationFactory<I>(typeName, iid);
            if (factory != null)
            {
                return factory;
            }
#endif

            var moduleName = typeName;
            while (true)
            {
                var lastSegment = moduleName.LastIndexOf(".", StringComparison.Ordinal);
                if (lastSegment <= 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                moduleName = moduleName.Remove(lastSegment);

                DllModule module = null;
                if (DllModule.TryLoad(moduleName + ".dll", out module))
                {
                    ObjectReference<IActivationFactoryVftbl> activationFactory;
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
    }
}

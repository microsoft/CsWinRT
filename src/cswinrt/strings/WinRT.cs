// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value
#pragma warning disable CA1060

namespace WinRT
{
    internal static class DelegateExtensions
    {
        public static void DynamicInvokeAbi(this System.Delegate del, object[] invoke_params)
        {
            Marshal.ThrowExceptionForHR((int)del.DynamicInvoke(invoke_params));
        }

        // These methods below can be used to efficiently change the signature of arbitrary delegate types
        // to one that has just 'object' as any of the type arguments, without the need to generate a new
        // closure and display class. The C# compiler will lower the method group expression over one of
        // the extension methods below into a direct constructor call of the new delegate types, passing
        // a function pointer for the target delegate, along with any target, if present. This is more
        // compact in binary size (and better in perf) than eg. 'return (object arg) => function(arg)';

        public static Func<object, TResult> WithObjectT<T, TResult>(this Func<T, TResult> function)
        {
            return function.InvokeWithObjectT;
        }

        public static Func<T, object> WithObjectTResult<T, TResult>(this Func<T, TResult> function)
        {
            return function.InvokeWithObjectTResult;
        }

        public static Func<object, object> WithObjectParams<T, TResult>(this Func<T, TResult> func)
        {
            return func.InvokeWithObjectParams;
        }

        public static Action<object> WithObjectParams<T>(this Action<T> action)
        {
            return action.InvokeWithObjectParams;
        }

        public static Action<object, T2> WithObjectT1<T1, T2>(this Action<T1, T2> action)
        {
            return action.InvokeWithObjectT1;
        }

        private static object InvokeWithObjectTResult<T, TResult>(this Func<T, TResult> func, T arg)
        {
            return func.Invoke(arg);
        }

        private static TResult InvokeWithObjectT<T, TResult>(this Func<T, TResult> func, object arg)
        {
            return func.Invoke((T)arg);
        }

        private static object InvokeWithObjectParams<T, TResult>(this Func<T, TResult> func, object arg)
        {
            return func.Invoke((T)arg);
        }

        private static void InvokeWithObjectParams<T>(this Action<T> func, object arg)
        {
            func.Invoke((T)arg);
        }

        private static void InvokeWithObjectT1<T1, T2>(this Action<T1, T2> action, object arg1, T2 arg2)
        {
            action.Invoke((T1)arg1, arg2);
        }
    }

    internal sealed class Platform
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoCreateInstance(Guid* clsid, IntPtr outer, uint clsContext, Guid* iid, IntPtr* instance);

        internal static unsafe int CoCreateInstance(ref Guid clsid, IntPtr outer, uint clsContext, ref Guid iid, IntPtr* instance)
        {
            fixed (Guid* lpClsid = &clsid)
            {
                fixed (Guid* lpIid = &iid)
                {
                    return CoCreateInstance(lpClsid, outer, clsContext, lpIid, instance);
                }
            }
        }

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern int CoDecrementMTAUsage(IntPtr cookie);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

#if NET6_0_OR_GREATER
        internal static bool FreeLibrary(IntPtr moduleHandle)
        {
            int lastError;
            bool returnValue;
            int nativeReturnValue;
            {
                Marshal.SetLastSystemError(0);
                nativeReturnValue = PInvoke(moduleHandle);
                lastError = Marshal.GetLastSystemError();
            }

            // Unmarshal - Convert native data to managed data.
            returnValue = nativeReturnValue != 0;
            Marshal.SetLastPInvokeError(lastError);
            return returnValue;

            // Local P/Invoke
            [DllImportAttribute("kernel32.dll", EntryPoint = "FreeLibrary", ExactSpelling = true)]
            static extern unsafe int PInvoke(IntPtr nativeModuleHandle);
        }

        internal static unsafe void* TryGetProcAddress(IntPtr moduleHandle, sbyte* functionName)
        {
            int lastError;
            void* returnValue;
            {
                Marshal.SetLastSystemError(0);
                returnValue = PInvoke(moduleHandle, functionName);
                lastError = Marshal.GetLastSystemError();
            }

            Marshal.SetLastPInvokeError(lastError);
            return returnValue;

            // Local P/Invoke
            [DllImportAttribute("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true)]
            static extern unsafe void* PInvoke(IntPtr nativeModuleHandle, sbyte* nativeFunctionName);
        }
#else
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr moduleHandle);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, BestFitMapping = false)]
        internal static unsafe extern void* TryGetProcAddress(IntPtr moduleHandle, sbyte* functionName);
#endif

        internal static unsafe void* TryGetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
        {
            fixed (byte* lpFunctionName = functionName)
            {
                return TryGetProcAddress(moduleHandle, (sbyte*)lpFunctionName);
            }
        }

        internal static unsafe void* TryGetProcAddress(IntPtr moduleHandle, string functionName)
        {
            bool allocated = false;
            Span<byte> buffer = stackalloc byte[0x100];
            if (functionName.Length * 3 >= 0x100) // Maximum of 3 bytes per UTF-8 character, stack allocation limit of 256 bytes (including the null terminator)
            {
                // Calculate accurate byte count when the provided stack-allocated buffer is not sufficient
                int exactByteCount = checked(Encoding.UTF8.GetByteCount(functionName) + 1); // + 1 for null terminator
                if (exactByteCount > 0x100)
                {
#if NET6_0_OR_GREATER
                    buffer = new((byte*)NativeMemory.Alloc((nuint)exactByteCount), exactByteCount);
#else
                    buffer = new((byte*)Marshal.AllocHGlobal(exactByteCount), exactByteCount);
#endif
                    allocated = true;
                }
            }

            var rawByte = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

            int byteCount;

#if NET
            byteCount = Encoding.UTF8.GetBytes(functionName, buffer);
#else
            fixed (char* lpFunctionName = functionName)
            {
                byteCount = Encoding.UTF8.GetBytes(lpFunctionName, functionName.Length, rawByte, buffer.Length);
            }
#endif
            buffer[byteCount] = 0;

            void* functionPtr = TryGetProcAddress(moduleHandle, (sbyte*)rawByte);

            if (allocated)
#if NET6_0_OR_GREATER
                NativeMemory.Free(rawByte);
#else
                Marshal.FreeHGlobal((IntPtr)rawByte);
#endif

            return functionPtr;
        }

        internal static unsafe void* GetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
        {
            fixed (byte* lpFunctionName = functionName)
            {
                void* functionPtr = Platform.TryGetProcAddress(moduleHandle, (sbyte*)lpFunctionName);
                if (functionPtr == null)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                }
                return functionPtr;
            }
        }

        internal static unsafe void* GetProcAddress(IntPtr moduleHandle, string functionName)
        {
            void* functionPtr = Platform.TryGetProcAddress(moduleHandle, functionName);
            if (functionPtr == null)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }
            return functionPtr;
        }

#if NET6_0_OR_GREATER
        internal static unsafe IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags)
        {
            int lastError;
            IntPtr returnValue;
            {
                Marshal.SetLastSystemError(0);
                returnValue = PInvoke(fileName, fileHandle, flags);
                lastError = Marshal.GetLastSystemError();
            }

            Marshal.SetLastPInvokeError(lastError);
            return returnValue;

            // Local P/Invoke
            [DllImportAttribute("kernel32.dll", EntryPoint = "LoadLibraryExW", ExactSpelling = true)]
            static extern unsafe IntPtr PInvoke(ushort* nativeFileName, IntPtr nativeFileHandle, uint nativeFlags);
        }
#else
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static unsafe extern IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags);
#endif
        internal static unsafe IntPtr LoadLibraryExW(string fileName, IntPtr fileHandle, uint flags)
        {
            fixed (char* lpFileName = fileName)
                return LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);
        }

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, Guid* iid, IntPtr* factory);

        internal static unsafe int RoGetActivationFactory(IntPtr runtimeClassId, ref Guid iid, IntPtr* factory)
        {
            fixed (Guid* lpIid = &iid)
            {
                return RoGetActivationFactory(runtimeClassId, lpIid, factory);
            }
        }

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateString(ushort* sourceString,
                                                  int length,
                                                  IntPtr* hstring);

        internal static unsafe int WindowsCreateString(string sourceString, int length, IntPtr* hstring)
        {
            fixed (char* lpSourceString = sourceString)
            {
                return WindowsCreateString((ushort*)lpSourceString, length, hstring);
            }
        }

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateStringReference(ushort* sourceString,
                                                  int length,
                                                  IntPtr* hstring_header,
                                                  IntPtr* hstring);

        internal static unsafe int WindowsCreateStringReference(char* sourceString, int length, IntPtr* hstring_header, IntPtr* hstring)
        {
            return WindowsCreateStringReference((ushort*)sourceString, length, hstring_header, hstring);
        }

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsDuplicateString(IntPtr sourceString,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

        [DllImport("api-ms-win-core-com-l1-1-1.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int RoGetAgileReference(uint options, Guid* iid, IntPtr unknown, IntPtr* agileReference);

        internal static unsafe int RoGetAgileReference(uint options, ref Guid iid, IntPtr unknown, IntPtr* agileReference)
        {
            fixed (Guid* lpIid = &iid)
            {
                return RoGetAgileReference(options, lpIid, unknown, agileReference);
            }
        }
    }

    internal struct VftblPtr
    {
        public IntPtr Vftbl;
    }
    internal static partial class Context
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        private static extern unsafe int CoGetContextToken(IntPtr* contextToken);

        public unsafe static IntPtr GetContextToken()
        {
            IntPtr contextToken;
            Marshal.ThrowExceptionForHR(CoGetContextToken(&contextToken));
            return contextToken;
        }
    }

    internal unsafe sealed class DllModule
    {
        readonly string _fileName;
        readonly IntPtr _moduleHandle;
        readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> _GetActivationFactory;
        readonly delegate* unmanaged[Stdcall]<int> _CanUnloadNow; // TODO: Eventually periodically call

        static readonly string _currentModuleDirectory = AppContext.BaseDirectory;

        static Dictionary<string, DllModule> _cache = new System.Collections.Generic.Dictionary<string, DllModule>(StringComparer.Ordinal);

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

        private static unsafe bool TryCreate(string fileName, out DllModule module)
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

#if NET7_0_OR_GREATER || CsWinRT_LANG_11_FEATURES
            ReadOnlySpan<byte> functionName = "DllGetActivationFactory"u8;
#else
            string functionName = "DllGetActivationFactory";
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
#if NET7_0_OR_GREATER || CsWinRT_LANG_11_FEATURES
            ReadOnlySpan<byte> functionName = "DllCanUnloadNow"u8;
#else
            string functionName = "DllCanUnloadNow";
#endif
            canUnloadNow = Platform.TryGetProcAddress(_moduleHandle, functionName);

            if (canUnloadNow != null)
            {
                _CanUnloadNow = (delegate* unmanaged[Stdcall]<int>)canUnloadNow;
            }
        }

        public unsafe (FactoryObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
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
                        var objRef = FactoryObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr);
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

    internal sealed class WinrtModule
    {
        readonly IntPtr _mtaCookie;
        volatile static WinrtModule _instance;
        private static WinrtModule MakeWinRTModule()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _instance, new WinrtModule(), null);
            return _instance;
        }
        public static WinrtModule Instance => _instance ?? MakeWinRTModule();

        public unsafe WinrtModule()
        {
            IntPtr mtaCookie;
            Marshal.ThrowExceptionForHR(Platform.CoIncrementMTAUsage(&mtaCookie));
            _mtaCookie = mtaCookie;
        }

        public static unsafe (FactoryObjectReference<I> obj, int hr) GetActivationFactory<I>(string runtimeClassId, Guid iid)
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
                        var objRef = FactoryObjectReference<I>.Attach(ref instancePtr);
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

        ~WinrtModule()
        {
            Marshal.ThrowExceptionForHR(Platform.CoDecrementMTAUsage(_mtaCookie));
        }
    }

    internal sealed class FactoryObjectReference<
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        T> : IObjectReference
    {
        private readonly IntPtr _contextToken;

        public static FactoryObjectReference<T> Attach(ref IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new FactoryObjectReference<T>(thisPtr);
            thisPtr = IntPtr.Zero;
            return obj;
        }

        internal FactoryObjectReference(IntPtr thisPtr) :
            base(thisPtr)
        {
            if (!IsFreeThreaded(this))
            {
                _contextToken = Context.GetContextToken();
            }
        }

        internal FactoryObjectReference(IntPtr thisPtr, IntPtr contextToken)
            : base(thisPtr)
        {
            _contextToken = contextToken;
        }

        public static new unsafe FactoryObjectReference<T> FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new FactoryObjectReference<T>(thisPtr);
            obj.VftblIUnknown.AddRef(obj.ThisPtr);
            return obj;
        }

        public bool IsObjectInContext()
        {
            return _contextToken == IntPtr.Zero || _contextToken == Context.GetContextToken();
        }

        // If we are free threaded, we do not need to keep track of context.
        // This can either be if the object implements IAgileObject or the free threaded marshaler.
        // We only check IAgileObject for now as the necessary code to check the
        // free threaded marshaler is not exposed from WinRT.Runtime.
        private unsafe static bool IsFreeThreaded(IObjectReference objRef)
        {
            if (objRef.TryAs(InterfaceIIDs.IAgileObject_IID, out var agilePtr) >= 0)
            {
                Marshal.Release(agilePtr);
                return true;
            }
            return false;
        }
    }

    internal static class IActivationFactoryMethods
    {
        public static unsafe ObjectReference<I> ActivateInstance<I>(IObjectReference obj)
        {
            IntPtr instancePtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)obj.ThisPtr)[6](obj.ThisPtr, &instancePtr));
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface<I>(instancePtr);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(instancePtr);
            }
        }
    }

    internal static class ActivationFactory
    {
        public static FactoryObjectReference<IActivationFactoryVftbl> Get(string typeName)
        {
            // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
            int hr;
            FactoryObjectReference<IActivationFactoryVftbl> factory;
            (factory, hr) = WinrtModule.GetActivationFactory<IActivationFactoryVftbl>(typeName, InterfaceIIDs.IActivationFactory_IID);
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
        public static FactoryObjectReference<I> Get<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)]
#else
        public static ObjectReference<I> Get<
#endif
        I>(string typeName, Guid iid)
        {
            // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
            int hr;
            FactoryObjectReference<I> factory;
            (factory, hr) = WinrtModule.GetActivationFactory<I>(typeName, iid);
            if (factory != null)
            {
#if NET
                return factory;
#else
                using (factory)
                {
                    return factory.As<I>(iid);
                }
#endif
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
                    FactoryObjectReference<IActivationFactoryVftbl> activationFactory;
                    (activationFactory, hr) = module.GetActivationFactory(typeName);
                    if (activationFactory != null)
                    {
                        using (activationFactory)
                        {
#if NET
                            if (activationFactory.TryAs(iid, out IntPtr iidPtr) >= 0)
                            {
                                return FactoryObjectReference<I>.Attach(ref iidPtr);
                            }
#else
                            return activationFactory.As<I>(iid);
#endif
                        }
                    }
                }
            }
        }
    }

    internal class ComponentActivationFactory : global::WinRT.Interop.IActivationFactory
    {
        public IntPtr ActivateInstance()
        {
            throw new NotImplementedException();
        }
    }

    internal class ActivatableComponentActivationFactory<T> : ComponentActivationFactory, global::WinRT.Interop.IActivationFactory where T : class, new()
    {
        public new IntPtr ActivateInstance()
        {
            T comp = new T();
            return MarshalInspectable<T>.FromManaged(comp);
        }
    }

#pragma warning disable CA2002

    // Registration state and delegate cached separately to survive EventSource garbage collection
    // and to prevent the generated event delegate from impacting the lifetime of the
    // event source.
    internal abstract class State : IDisposable
    {
        public EventRegistrationToken token;
        public System.Delegate del;
        public System.Delegate eventInvoke;
        private bool disposedValue;
        private readonly IntPtr obj;
        private readonly int index;
        private readonly System.WeakReference<State> cacheEntry;
        private IntPtr eventInvokePtr;
        private IntPtr referenceTrackerTargetPtr;

        protected State(IntPtr obj, int index)
        {
            this.obj = obj;
            this.index = index;
            eventInvoke = GetEventInvoke();
            cacheEntry = new System.WeakReference<State>(this);
        }

        // The lifetime of this object is managed by the delegate / eventInvoke
        // through its target reference to it.  Once the delegate no longer has
        // any references, this object will also no longer have any references.
        ~State()
        {
            Dispose(false);
        }

        // Allows to retrieve a singleton like weak reference to use
        // with the cache to allow for proper removal with comparision.
        public System.WeakReference<State> GetWeakReferenceForCache()
        {
            return cacheEntry;
        }

        protected abstract System.Delegate GetEventInvoke();

        protected virtual void Dispose(bool disposing)
        {
            // Uses the dispose pattern to ensure we only remove
            // from the cache once: either via unsubscribe or via
            // the finalizer.
            if (!disposedValue)
            {
                Cache.Remove(obj, index, cacheEntry);
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void InitalizeReferenceTracking(IntPtr ptr)
        {
            eventInvokePtr = ptr;
            Guid iid = IReferenceTrackerTargetVftbl.IID;
            int hr = Marshal.QueryInterface(ptr, ref iid, out referenceTrackerTargetPtr);
            if (hr != 0)
            {
                referenceTrackerTargetPtr = default;
            }
            else
            {
                // We don't want to keep ourselves alive and as long as this object
                // is alive, the CCW still exists.
                Marshal.Release(referenceTrackerTargetPtr);
            }
        }

        public unsafe bool HasComReferences()
        {
            if (eventInvokePtr != default)
            {
                IUnknownVftbl vftblIUnknown = **(IUnknownVftbl**)eventInvokePtr;
                vftblIUnknown.AddRef(eventInvokePtr);
                uint comRefCount = vftblIUnknown.Release(eventInvokePtr);
                if (comRefCount != 0)
                {
                    return true;
                }
            }

            if (referenceTrackerTargetPtr != default)
            {
                IReferenceTrackerTargetVftbl vftblReferenceTracker = **(IReferenceTrackerTargetVftbl**)referenceTrackerTargetPtr;
                vftblReferenceTracker.AddRefFromReferenceTracker(referenceTrackerTargetPtr);
                uint refTrackerCount = vftblReferenceTracker.ReleaseFromReferenceTracker(referenceTrackerTargetPtr);
                if (refTrackerCount != 0)
                {
                    // Note we can't tell if the reference tracker ref is pegged or not, so this is best effort where if there
                    // are any reference tracker references, we assume the event has references.
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class Cache
    {
        private Cache(IWeakReference target, int index, System.WeakReference<State> state)
        {
            this.target = target;
            SetState(index, state);
        }

        private IWeakReference target;
        private readonly ConcurrentDictionary<int, System.WeakReference<State>> states = new ConcurrentDictionary<int, System.WeakReference<State>>();

        private static readonly ReaderWriterLockSlim cachesLock = new ReaderWriterLockSlim();
        private static readonly ConcurrentDictionary<IntPtr, Cache> caches = new ConcurrentDictionary<IntPtr, Cache>();

        private Cache Update(IWeakReference target, int index, System.WeakReference<State> state)
        {
            // If target no longer exists, destroy cache
            lock (this)
            {
                using var resolved = this.target.Resolve(InterfaceIIDs.IUnknown_IID);
                if (resolved == null)
                {
                    this.target = target;
                    states.Clear();
                }
            }
            SetState(index, state);
            return this;
        }

        private System.WeakReference<State> GetState(int index)
        {
            // If target no longer exists, destroy cache
            lock (this)
            {
                using var resolved = this.target.Resolve(InterfaceIIDs.IUnknown_IID);
                if (resolved == null)
                {
                    return null;
                }
            }

            if (states.TryGetValue(index, out var weakState))
            {
                return weakState;
            }
            return null;
        }

        private void SetState(int index, System.WeakReference<State> state)
        {
            states[index] = state;
        }

        public static void Create(IObjectReference obj, int index, System.WeakReference<State> state)
        {
            // If event source implements weak reference support, track event registrations so that
            // unsubscribes will work across garbage collections.  Note that most static/factory classes
            // do not implement IWeakReferenceSource, so static codegen caching approach is also used.
            IWeakReference target = null;
#if !NET
                try
                {
                    var weakRefSource = (IWeakReferenceSource)typeof(IWeakReferenceSource).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { obj });
                    if (weakRefSource == null)
                    {
                        return;
                    }
                    target = weakRefSource.GetWeakReference();
                }
                catch(Exception)
                {
                    return;
                }
#else
            int hr = obj.TryAs<IUnknownVftbl>(InterfaceIIDs.IWeakReferenceSource_IID, out var weakRefSource);
            if (hr != 0)
            {
                return;
            }

            target = ABI.WinRT.Interop.IWeakReferenceSourceMethods.GetWeakReference(weakRefSource);
#endif

            cachesLock.EnterReadLock();
            try
            {
                caches.AddOrUpdate(obj.ThisPtr,
                    (IntPtr ThisPtr) => new Cache(target, index, state),
                    (IntPtr ThisPtr, Cache cache) => cache.Update(target, index, state));
            }
            finally
            {
                cachesLock.ExitReadLock();
            }
        }

        public static System.WeakReference<State> GetState(IObjectReference obj, int index)
        {
            if (caches.TryGetValue(obj.ThisPtr, out var cache))
            {
                return cache.GetState(index);
            }

            return null;
        }

        public static void Remove(IntPtr thisPtr, int index, System.WeakReference<State> state)
        {
            if (caches.TryGetValue(thisPtr, out var cache))
            {
#if !NET
                    // https://devblogs.microsoft.com/pfxteam/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
                    ((ICollection<KeyValuePair<int, System.WeakReference<State>>>)cache.states).Remove(
                        new KeyValuePair<int, System.WeakReference<State>>(index, state));
#else
                cache.states.TryRemove(new KeyValuePair<int, System.WeakReference<State>>(index, state));
#endif
                // using double-checked lock idiom
                if (cache.states.IsEmpty)
                {
                    cachesLock.EnterWriteLock();
                    try
                    {
                        if (cache.states.IsEmpty)
                        {
                            caches.TryRemove(thisPtr, out var _);
                        }
                    }
                    finally
                    {
                        cachesLock.ExitWriteLock();
                    }
                }
            }
        }
    }

    internal unsafe abstract class EventSource<TDelegate>
        where TDelegate : class, MulticastDelegate
    {
        protected readonly IObjectReference _obj;
        protected readonly int _index;
#if NET
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, WinRT.EventRegistrationToken*, int> _addHandler;
#else
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> _addHandler;
#endif
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> _removeHandler;
        protected System.WeakReference<State> _state;
        private readonly (Action<TDelegate>, Action<TDelegate>) _handlerTuple;

        protected EventSource(IObjectReference obj,
#if NET
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, WinRT.EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler,
            int index = 0)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _index = index;
            _state = Cache.GetState(obj, index);
            _handlerTuple = (Subscribe, Unsubscribe);
        }

        protected abstract ObjectReferenceValue CreateMarshaler(TDelegate del);

        protected abstract State CreateEventState();

        public void Subscribe(TDelegate del)
        {
            lock (this)
            {
                State state = null;
                bool registerHandler =
                    _state is null ||
                    !_state.TryGetTarget(out state) ||
                    // We have a wrapper delegate, but no longer has any references from any event source.
                    !state.HasComReferences();
                if (registerHandler)
                {
                    state = CreateEventState();
                    _state = state.GetWeakReferenceForCache();
                    Cache.Create(_obj, _index, _state);
                }

                state.del = (TDelegate)global::System.Delegate.Combine(state.del, del);
                if (registerHandler)
                {
                    var eventInvoke = (TDelegate)state.eventInvoke;
                    var marshaler = CreateMarshaler(eventInvoke);
                    try
                    {
                        var nativeDelegate = marshaler.GetAbi();
                        state.InitalizeReferenceTracking(nativeDelegate);
#if NET
                        WinRT.EventRegistrationToken token;
                        ExceptionHelpers.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, nativeDelegate, &token));
                        state.token = token;
#else
                        ExceptionHelpers.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, nativeDelegate, out state.token));
#endif
                    }
                    finally
                    {
                        // Dispose our managed reference to the delegate's CCW.
                        // Either the native event holds a reference now or the _addHandler call failed.
                        marshaler.Dispose();
                    }
                }
            }
        }

        public void Unsubscribe(TDelegate del)
        {
            if (_state is null || !_state.TryGetTarget(out var state))
            {
                return;
            }

            lock (this)
            {
                var oldEvent = state.del;
                state.del = (TDelegate)global::System.Delegate.Remove(state.del, del);
                if (oldEvent is object && state.del is null)
                {
                    UnsubscribeFromNative(state);
                }
            }
        }

        public (Action<TDelegate>, Action<TDelegate>) EventActions => _handlerTuple;

        private void UnsubscribeFromNative(State state)
        {
            ExceptionHelpers.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, state.token));
            state.Dispose();
            _state = null;
        }
    }

    internal unsafe sealed class EventSource__EventHandler<T> : EventSource<System.EventHandler<T>>
    {
        internal EventSource__EventHandler(IObjectReference obj,
#if NET
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, WinRT.EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler,
            int index) : base(obj, addHandler, removeHandler, index)
        {
        }

        protected override ObjectReferenceValue CreateMarshaler(System.EventHandler<T> del) => 
            ABI.System.EventHandler<T>.CreateMarshaler2(del);

        protected override State CreateEventState() =>
            new EventState(_obj.ThisPtr, _index);

        private sealed class EventState : State
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override Delegate GetEventInvoke()
            {
                System.EventHandler<T> handler = (System.Object obj, T e) =>
                {
                    var localDel = (System.EventHandler<T>) del;
                    if (localDel != null)
                        localDel.Invoke(obj, e);
                };
                return handler;
            }
        }
    }

#pragma warning restore CA2002

    // An event registration token table stores mappings from delegates to event tokens, in order to support
    // sourcing WinRT style events from managed code.
    internal sealed class EventRegistrationTokenTable<T>
        where T : global::System.Delegate
    {
        /// <summary>
        /// The hashcode of the delegate type, being set in the upper 32 bits of the registration tokens.
        /// </summary>
        private static readonly int TypeOfTHashCode = GetTypeOfTHashCode();

        private static int GetTypeOfTHashCode()
        {
            int hashCode = typeof(T).GetHashCode();

            // There is a minimal but non-zero chance that the hashcode of the T type argument will be 0.
            // If that is the case, it means that it is possible for an event registration token to just
            // be 0, which will happen when the low 32 bits also wrap around and go through 0. Such a
            // registration token is not valid as per the WinRT spec, see:
            // https://learn.microsoft.com/uwp/api/windows.foundation.eventregistrationtoken.value.
            // To work around this, we just check for this edge case and return a magic constant instead.
            if (hashCode == 0)
            {
                return 0x5FC74196;
            }

            return hashCode;
        }

        // Note this dictionary is also used as the synchronization object for this table
        private readonly Dictionary<int, object> m_tokens = new Dictionary<int, object>();

        // The current counter used for the low 32 bits of the registration tokens.
        // We explicit use [int.MinValue, int.MaxValue] as the range, as this value
        // is expected to eventually wrap around, and we don't want to lose the
        // additional possible range of negative values (there's no reason for that).
        private int m_low32Bits =
#if NET6_0_OR_GREATER
            Random.Shared.Next(int.MinValue, int.MaxValue);
#else
            new Random().Next(int.MinValue, int.MaxValue);
#endif

        public EventRegistrationToken AddEventHandler(T handler)
        {
            // Windows Runtime allows null handlers.  Assign those the default token (token value 0) for simplicity
            if (handler == null)
            {
                return default;
            }

            lock (m_tokens)
            {
                return AddEventHandlerNoLock(handler);
            }
        }

        private EventRegistrationToken AddEventHandlerNoLock(T handler)
        {
            Debug.Assert(handler != null);

            // Get a registration token, making sure that we haven't already used the value. This should be quite
            // rare, but in the case it does happen, just keep trying until we find one that's unused. Note that
            // this mutable part of the token is just 32 bit wide (the lower 32 bits). The upper 32 bits are fixed.
            //
            // Note that:
            //   - If there is a handler assigned to the generated initial token value, it is not necessarily
            //     this handler.
            //   - If there is no handler assigned to the generated initial token value, the handler may still
            //     be registered under a different token.
            //
            // Effectively the only reasonable thing to do with this value is to use it as a good starting point
            // for generating a token for handler.
            //
            // We want to generate a token value that has the following properties:
            //   1. Is quickly obtained from the handler instance (in this case, it doesn't depend on it at all).
            //   2. Uses bits in the upper 32 bits of the 64 bit value, in order to avoid bugs where code
            //      may assume the value is really just 32 bits.
            //   3. Uses bits in the bottom 32 bits of the 64 bit value, in order to ensure that code doesn't
            //      take a dependency on them always being 0.
            //
            // The simple algorithm chosen here is to simply assign the upper 32 bits the metadata token of the
            // event handler type, and the lower 32 bits to an incremental counter starting from some arbitrary
            // constant. Using the metadata token for the upper 32 bits gives us at least a small chance of being
            // able to identify a totally corrupted token if we ever come across one in a minidump or other scenario.
            //
            // We should feel free to change this algorithm as other requirements / optimizations become available.
            // This implementation is sufficiently random that code cannot simply guess the value to take a dependency
            // upon it. (Simply applying the hash-value algorithm directly won't work in the case of collisions,
            // where we'll use a different token value).
            int tokenLow32Bits;

#if NET6_0_OR_GREATER
            do
            {
                // When on .NET 6+, just iterate on TryAdd, which allows skipping the extra
                // lookup on the last iteration (as the handler is added rigth away instead).
                //
                // We're doing this do-while loop here and incrementing 'm_low32Bits' on every failed insertion to work
                // around one possible (theoretical) performance problem. Suppose the candidate token was somehow already
                // used (not entirely clear when that would happen in practice). Incrementing only the local value from the
                // loop would mean we could "race past" the value in 'm_low32Bits', meaning that all subsequent registrations
                // would then also go through unnecessary extra lookups as the value of those lower 32 bits "catches up" to
                // the one that ended up being used here. So we can avoid that by simply incrementing both of them every time.
                tokenLow32Bits = m_low32Bits++;
            }
            while (!m_tokens.TryAdd(tokenLow32Bits, handler));
#else
            do
            {
                tokenLow32Bits = m_low32Bits++;
            }
            while (m_tokens.ContainsKey(tokenLow32Bits));
            m_tokens[tokenLow32Bits] = handler;
#endif
            // The real event registration token is composed this way:
            //   - The upper 32 bits are the hashcode of the T type argument.
            //   - The lower 32 bits are the valid token computed above.
            return new EventRegistrationToken { Value = (long)(((ulong)(uint)TypeOfTHashCode << 32) | (uint)tokenLow32Bits) };
        }

        // Remove the event handler from the table and
        // Get the delegate associated with an event registration token if it exists
        // If the event registration token is not registered, returns false
        public bool RemoveEventHandler(EventRegistrationToken token, out T handler)
        {
            // If the token doesn't have the upper 32 bits set to the hashcode of the delegate
            // type in use, we know that the token cannot possibly have a registered handler.
            //
            // Note that both here right after the right shift by 32 bits (since we want to read
            // the upper 32 bits to compare against the T hashcode) and below (where we want to
            // read the lower 32 bits to use as lookup index into our dictionary), we're just
            // casting to int as a simple and efficient way of truncating the input 64 bit value.
            // That is, '(int)i64' is the same as '(int)(i64 & 0xFFFFFFFF)', but more readable.
            if ((int)((ulong)token.Value >> 32) != TypeOfTHashCode)
            {
                handler = null;

                return false;
            }

            lock (m_tokens)
            {
#if NET6_0_OR_GREATER
                // On .NET 6 and above, we can use a single lookup to both check whether the token
                // exists in the table, remove it, and also retrieve the removed handler to return.
                if (m_tokens.Remove((int)token.Value, out object obj))
                {
                    handler = Unsafe.As<T>(obj);

                    return true;
                }
#else
                if (m_tokens.TryGetValue((int)token.Value, out object obj))
                {
                    m_tokens.Remove((int)token.Value);

                    handler = Unsafe.As<T>(obj);

                    return true;
                }
#endif
            }

            handler = null;

            return false;
        }
    }

    internal static class InterfaceIIDs
    {
#if NET
        internal static readonly Guid IInspectable_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xE0, 0xE2, 0x86, 0xAF, 0x2D, 0xB1, 0x6A, 0x4C, 0x9C, 0x5A, 0xD7, 0xAA, 0x65, 0x10, 0x1E, 0x90 }));
        internal static readonly Guid IUnknown_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IWeakReferenceSource_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x38, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IWeakReference_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x37, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IActivationFactory_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IAgileObject_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x94, 0x2B, 0xEA, 0x94, 0xCC, 0xE9, 0xE0, 0x49, 0xC0, 0xFF, 0xEE, 0x64, 0xCA, 0x8F, 0x5B, 0x90 }));
        internal static readonly Guid IMarshal_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IContextCallback_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xDA, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
#else
        internal static readonly Guid IInspectable_IID = new(0xAF86E2E0, 0xB12D, 0x4c6a, 0x9C, 0x5A, 0xD7, 0xAA, 0x65, 0x10, 0x1E, 0x90);
        internal static readonly Guid IUnknown_IID = new(0, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IWeakReferenceSource_IID = new(0x00000038, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IWeakReference_IID = new(0x00000037, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IActivationFactory_IID = new (0x00000035, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IAgileObject_IID = new(0x94ea2b94, 0xe9cc, 0x49e0, 0xc0, 0xff, 0xee, 0x64, 0xca, 0x8f, 0x5b, 0x90);
        internal static readonly Guid IMarshal_IID = new(0x00000003, 0, 0, 0xc0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IContextCallback_IID = new(0x000001da, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
#endif
    }
}

#if !NET
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}
#endif

namespace WinRT
{
    internal static class ProjectionInitializer
    {
        [ModuleInitializer]
        internal static void InitalizeProjection()
        {
            ComWrappersSupport.RegisterProjectionAssembly(typeof(ProjectionInitializer).Assembly);
        }
    }
}

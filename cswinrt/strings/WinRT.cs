using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    public enum TrustLevel
    {
        BaseTrust = 0,
        PartialTrust = BaseTrust + 1,
        FullTrust = PartialTrust + 1
    };

    public delegate void EventHandler();
    public delegate void EventHandler<A1>(A1 arg1);
    public delegate void EventHandler<A1, A2>(A1 arg1, A2 arg2);
    public delegate void EventHandler<A1, A2, A3>(A1 arg1, A2 arg2, A3 arg3);

    namespace Interop
    {
        // IUnknown
        [Guid("00000000-0000-0000-C000-000000000046")]
        public struct IUnknownVftbl
        {
            public unsafe delegate int _QueryInterface([In] IntPtr pThis, [In] ref Guid iid, [Out] out IntPtr vftbl);
            public delegate uint _AddRef([In] IntPtr pThis);
            public delegate uint _Release([In] IntPtr pThis);

            public _QueryInterface QueryInterface;
            public _AddRef AddRef;
            public _Release Release;
        }

        // IActivationFactory
        [Guid("00000035-0000-0000-C000-000000000046")]
        public struct IActivationFactoryVftbl
        {
            public unsafe delegate int _ActivateInstance([In] IntPtr pThis, [Out] out IntPtr instance);

            public IInspectable.Vftbl IInspectableVftbl;
            public _ActivateInstance ActivateInstance;
        }

        // standard accessors/mutators
        public unsafe delegate int _get_PropertyAsBoolean([In] IntPtr thisPtr, [Out] out byte value);
        public delegate int _put_PropertyAsBoolean([In] IntPtr thisPtr, [In] byte value);
        public unsafe delegate int _get_PropertyAsChar([In] IntPtr thisPtr, [Out] out char value);
        public delegate int _put_PropertyAsChar([In] IntPtr thisPtr, [In] char value);
        public unsafe delegate int _get_PropertyAsSByte([In] IntPtr thisPtr, [Out] out sbyte value);
        public delegate int _put_PropertyAsSByte([In] IntPtr thisPtr, [In] sbyte value);
        public unsafe delegate int _get_PropertyAsByte([In] IntPtr thisPtr, [Out] out byte value);
        public delegate int _put_PropertyAsByte([In] IntPtr thisPtr, [In] byte value);
        public unsafe delegate int _get_PropertyAsInt16([In] IntPtr thisPtr, [Out] out short value);
        public delegate int _put_PropertyAsInt16([In] IntPtr thisPtr, [In] short value);
        public unsafe delegate int _get_PropertyAsUInt16([In] IntPtr thisPtr, [Out] out ushort value);
        public delegate int _put_PropertyAsUInt16([In] IntPtr thisPtr, [In] ushort value);
        public unsafe delegate int _get_PropertyAsInt32([In] IntPtr thisPtr, [Out] out int value);
        public delegate int _put_PropertyAsInt32([In] IntPtr thisPtr, [In] int value);
        public unsafe delegate int _get_PropertyAsUInt32([In] IntPtr thisPtr, [Out] out uint value);
        public delegate int _put_PropertyAsUInt32([In] IntPtr thisPtr, [In] uint value);
        public unsafe delegate int _get_PropertyAsInt64([In] IntPtr thisPtr, [Out] out long value);
        public delegate int _put_PropertyAsInt64([In] IntPtr thisPtr, [In] long value);
        public unsafe delegate int _get_PropertyAsUInt64([In] IntPtr thisPtr, [Out] out ulong value);
        public delegate int _put_PropertyAsUInt64([In] IntPtr thisPtr, [In] ulong value);
        public unsafe delegate int _get_PropertyAsFloat([In] IntPtr thisPtr, [Out] out float value);
        public delegate int _put_PropertyAsFloat([In] IntPtr thisPtr, [In] float value);
        public unsafe delegate int _get_PropertyAsDouble([In] IntPtr thisPtr, [Out] out double value);
        public delegate int _put_PropertyAsDouble([In] IntPtr thisPtr, [In] double value);
        public unsafe delegate int _get_PropertyAsObject([In] IntPtr thisPtr, [Out] out IntPtr value);
        public delegate int _put_PropertyAsObject([In] IntPtr thisPtr, [In] IntPtr value);
        public unsafe delegate int _get_PropertyAsGuid([In] IntPtr thisPtr, [Out] out Guid value);
        public delegate int _put_PropertyAsGuid([In] IntPtr thisPtr, [In] Guid value);
        //public unsafe delegate int _get_PropertyAsString([In] IntPtr thisPtr, [Out, MarshalAs(UnmanagedType.HString)] out string value);
        //public delegate int _put_PropertyAsString([In] IntPtr thisPtr, [In, MarshalAs(UnmanagedType.HString)] string value);
        public unsafe delegate int _get_PropertyAsString([In] IntPtr thisPtr, [Out] out IntPtr value);
        public delegate int _put_PropertyAsString([In] IntPtr thisPtr, [In] IntPtr value);
        public unsafe delegate int _get_PropertyAsVector3([In] IntPtr thisPtr, [Out] out Windows.Foundation.Numerics.Vector3 value);
        public delegate int _put_PropertyAsVector3([In] IntPtr thisPtr, [In] Windows.Foundation.Numerics.Vector3 value);
        public unsafe delegate int _get_PropertyAsQuaternion([In] IntPtr thisPtr, [Out] out Windows.Foundation.Numerics.Quaternion value);
        public delegate int _put_PropertyAsQuaternion([In] IntPtr thisPtr, [In] Windows.Foundation.Numerics.Quaternion value);
        public unsafe delegate int _get_PropertyAsMatrix4x4([In] IntPtr thisPtr, [Out] out Windows.Foundation.Numerics.Matrix4x4 value);
        public delegate int _put_PropertyAsMatrix4x4([In] IntPtr thisPtr, [In] Windows.Foundation.Numerics.Matrix4x4 value);
        public unsafe delegate int _add_EventHandler([In] IntPtr thisPtr, [In] IntPtr handler, [Out] out WinRT.Interop.EventRegistrationToken token);
        public delegate int _remove_EventHandler([In] IntPtr thisPtr, [In] WinRT.Interop.EventRegistrationToken token);

        // IDelegate
        public struct IDelegateVftbl
        {
            public IntPtr QueryInterface;
            public IntPtr AddRef;
            public IntPtr Release;
            public IntPtr Invoke;
        }

        public struct EventRegistrationToken
        {
            public long Value;
        }
    }

    // IInspectable
    [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
    public class IInspectable
    {
        [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
        public struct Vftbl
        {
            public delegate int _GetIids([In] IntPtr pThis, [Out] uint iidCount, [Out] Guid[] iids);
            public delegate int _GetRuntimeClassName([In] IntPtr pThis, [Out] IntPtr className);
            public delegate int _GetTrustLevel([In] IntPtr pThis, [Out] TrustLevel trustLevel);

            public Interop.IUnknownVftbl IUnknownVftbl;
            public _GetIids GetIids;
            public _GetRuntimeClassName GetRuntimeClassName;
            public _GetTrustLevel GetTrustLevel;
        }

        private readonly WinRT.ObjectReference<Vftbl> _obj;
        public IntPtr NativePtr { get => _obj.ThisPtr; }
        public static WinRT.ObjectReference<Vftbl> FromNative(IntPtr @this) => WinRT.ObjectReference<Vftbl>.FromNative(@this);
        public static implicit operator IInspectable(WinRT.IObjectReference obj) => obj.As<Vftbl>();
        public static implicit operator IInspectable(WinRT.ObjectReference<Vftbl> obj) => new IInspectable(obj);
        public WinRT.ObjectReference<I> As<I>() => _obj.As<I>();
        public IInspectable(WinRT.ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }
        public object _WinRT_Owner { get; set; }
    }

    public static class DelegateExtensions
    {
        public static T AsDelegate<T>(this MulticastDelegate del)
        {
            return Marshal.GetDelegateForFunctionPointer<T>(
                Marshal.GetFunctionPointerForDelegate(del));
        }
    }

    internal class Platform
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern int CoDecrementMTAUsage([In] IntPtr cookie);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoIncrementMTAUsage([Out] IntPtr* cookie);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr moduleHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr moduleHandle, [MarshalAs(UnmanagedType.LPStr)] string functionName);

        internal static T GetProcAddress<T>(IntPtr moduleHandle)
        {
            IntPtr functionPtr = Platform.GetProcAddress(moduleHandle, typeof(T).Name);
            if (functionPtr == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string fileName, IntPtr fileHandle, uint flags);

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, ref Guid iid, IntPtr* factory);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString,
                                                  int length,
                                                  [Out] IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateStringReference(char* sourceString,
                                                  int length,
                                                  [Out] IntPtr* hstring_header,
                                                  [Out] IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsDuplicateString([In] IntPtr sourceString,
                                                  [Out] IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, [Out] uint* length);
    }

    internal class Mono
    {
        static Lazy<bool> _usingMono = new Lazy<bool>(() =>
        {
            var modulePtr = Platform.LoadLibraryExW("mono-2.0-bdwgc.dll", IntPtr.Zero, 0);
            if (modulePtr == IntPtr.Zero) return false;

            if (!Platform.FreeLibrary(modulePtr))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return true;
        });

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern IntPtr mono_thread_current();

        [DllImport("mono-2.0-bdwgc.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool mono_thread_is_foreign(IntPtr threadPtr);

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern void mono_unity_thread_fast_attach(IntPtr domainPtr);

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern void mono_unity_thread_fast_detach();

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern void mono_thread_pop_appdomain_ref();

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern IntPtr mono_domain_get();

        struct MonoObject
        {
            IntPtr vtable;
            IntPtr synchronisation; // sic
        }

        unsafe struct MonoThread
        {
            MonoObject obj;
            public MonoInternalThread_x64* internal_thread;
            IntPtr start_obj;
            IntPtr pending_exception;
        }

        [Flags]
        enum MonoThreadFlag : int
        {
            MONO_THREAD_FLAG_DONT_MANAGE = 1,
            MONO_THREAD_FLAG_NAME_SET = 2,
            MONO_THREAD_FLAG_APPDOMAIN_ABORT = 4,
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MonoInternalThread_x64
        {
            [FieldOffset(0xd0)]
            public MonoThreadFlag flags;
        }

        public class ThreadContext : IDisposable
        {
            static Lazy<HashSet<IntPtr>> _foreignThreads = new Lazy<HashSet<IntPtr>>();

            readonly IntPtr _threadPtr = IntPtr.Zero;

            public ThreadContext()
            {
                if (_usingMono.Value)
                {
                    // nothing to do for Mono-native threads
                    var threadPtr = mono_thread_current();
                    if (mono_thread_is_foreign(threadPtr))
                    {
                        // initialize this thread the first time it runs managed code, and remember it for future reference
                        if (_foreignThreads.Value.Add(threadPtr))
                        {
                            // clear initial appdomain ref for new foreign threads to avoid deadlock on domain unload
                            mono_thread_pop_appdomain_ref();

                            unsafe
                            {
                                // tell Mono to ignore the thread on process shutdown since there's nothing to synchronize with
                                ((MonoThread*)threadPtr)->internal_thread->flags |= MonoThreadFlag.MONO_THREAD_FLAG_DONT_MANAGE;
                            }
                        }

                        unsafe
                        {
                            // attach as Unity does to set up the proper domain for the call
                            mono_unity_thread_fast_attach(mono_domain_get());
                            _threadPtr = threadPtr;
                        }
                    }
                }
            }

            public void Dispose()
            {
                if (_threadPtr != IntPtr.Zero)
                {
                    // detach as Unity does to properly reset the domain context
                    mono_unity_thread_fast_detach();
                }
            }
        }
    }

    public class HString : ICloneable, IDisposable
    {
        public readonly IntPtr Handle;

        public HString()
        { }

        public HString(IntPtr handle)
        {
            Handle = handle;
        }

        public HString(string value)
        {
            unsafe
            {
                IntPtr handle;
                Marshal.ThrowExceptionForHR(Platform.WindowsCreateString(value, value.Length, &handle));
                Handle = handle;
            }
        }

        public static implicit operator HString(String value)
        {
            return new HStringReference(value);
        }

        public static implicit operator String(HString value)
        {
            return value.ToString();
        }

        public override string ToString()
        {
            unsafe
            {
                uint length;
                char* buffer = Platform.WindowsGetStringRawBuffer(Handle, &length);
                return new string(buffer, 0, (int)length);
            }
        }

        public object Clone()
        {
            unsafe
            {
                IntPtr handle;
                Marshal.ThrowExceptionForHR(Platform.WindowsDuplicateString(Handle, &handle));
                return new HString(handle);
            }
        }

        public virtual void Dispose()
        {
            Marshal.ThrowExceptionForHR(Platform.WindowsDeleteString(Handle));
        }
    }

    public class HStringReference : HString
    {
        // sizeof(HSTRING_HEADER)
        private unsafe struct HStringHeader
        {
            public fixed byte Reserved[24];
        };
        private HStringHeader _header;
        private GCHandle _gchandle;

        public HStringReference(String value)
        {
            // todo: does value need to be pinned?
            _gchandle = GCHandle.Alloc(value);
            unsafe
            {
                fixed (void* chars = value, pHeader = &_header, pHandle = &Handle)
                {
                    Marshal.ThrowExceptionForHR(WinRT.Platform.WindowsCreateStringReference(
                        (char*)chars, value.Length, (IntPtr*)pHeader, (IntPtr*)pHandle));
                }
            }
        }

        public override void Dispose()
        {
            // no need to delete hstring reference
            _gchandle.Free();
        }
    }

    internal struct VftblPtr
    {
        public IntPtr Vftbl;
    }

    public abstract class IObjectReference
    {
        public readonly IntPtr ThisPtr;
        protected virtual Interop.IUnknownVftbl VftblIUnknown { get; }

        protected IObjectReference(IntPtr thisPtr)
        {
            ThisPtr = thisPtr;
        }

        public ObjectReference<T> As<T>() => As<T>(GuidGenerator.GetIID(typeof(T)));
        public unsafe ObjectReference<T> As<T>(Guid iid)
        {
            IntPtr thatPtr;
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out thatPtr));
            return ObjectReference<T>.Attach(ref thatPtr);
        }

        public T AsType<T>()
        {
            var ctor = typeof(T).GetConstructor(new[] { typeof(IObjectReference) });
            if (ctor != null)
            {
                return (T)ctor.Invoke(new[] { this });
            }
            throw new InvalidOperationException("Target type is not a projected interface.");
        }
    }

    public class ObjectReference<T> : IObjectReference
    {
        protected override Interop.IUnknownVftbl VftblIUnknown => _vftblIUnknown;
        readonly Interop.IUnknownVftbl _vftblIUnknown;
        public readonly T Vftbl;

        public static ObjectReference<T> Attach(ref IntPtr thisPtr)
        {
            var obj = new ObjectReference<T>(thisPtr);
            thisPtr = IntPtr.Zero;
            return obj;
        }

        public static ObjectReference<T> FromNative(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }

            var obj = new ObjectReference<T>(thisPtr);
            obj._vftblIUnknown.AddRef(obj.ThisPtr);
            return obj;
        }

        ObjectReference(IntPtr thisPtr) :
            base(thisPtr)
        {
            var vftblPtr = Marshal.PtrToStructure<VftblPtr>(ThisPtr);
            _vftblIUnknown = Marshal.PtrToStructure<Interop.IUnknownVftbl>(vftblPtr.Vftbl);
            Vftbl = Marshal.PtrToStructure<T>(vftblPtr.Vftbl);
        }

        ~ObjectReference()
        {
            _vftblIUnknown.Release(ThisPtr);
        }
    }

    internal class DllModule
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate int DllGetActivationFactory(
            [In] IntPtr activatableClassId,
            [Out] out IntPtr activationFactory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate int DllCanUnloadNow();

        readonly string _fileName;
        readonly IntPtr _moduleHandle;
        readonly DllGetActivationFactory _GetActivationFactory;
        readonly DllCanUnloadNow _CanUnloadNow;

        static readonly string _currentModuleDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        static Dictionary<string, DllModule> _cache = new System.Collections.Generic.Dictionary<string, DllModule>();

        public static DllModule Load(string fileName)
        {
            lock (_cache)
            {
                DllModule module;
                if (!_cache.TryGetValue(fileName, out module))
                {
                    module = new DllModule(fileName);
                    _cache[fileName] = module;
                }
                return module;
            }
        }

        DllModule(string fileName)
        {
            _fileName = fileName;

            // Explicitly look for module in the same directory as this one, and
            // use altered search path to ensure any dependencies in the same directory are found.
            _moduleHandle = Platform.LoadLibraryExW(System.IO.Path.Combine(_currentModuleDirectory, fileName), IntPtr.Zero, /* LOAD_WITH_ALTERED_SEARCH_PATH */ 8);
            if (_moduleHandle == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            _GetActivationFactory = Platform.GetProcAddress<DllGetActivationFactory>(_moduleHandle);
            _CanUnloadNow = Platform.GetProcAddress<DllCanUnloadNow>(_moduleHandle); // TODO: Eventually periodically call
        }

        public unsafe ObjectReference<I> GetStaticClass<I>(HString runtimeClassId)
        {
            IntPtr instancePtr;
            Marshal.ThrowExceptionForHR(_GetActivationFactory(runtimeClassId.Handle, out instancePtr));
            return ObjectReference<I>.Attach(ref instancePtr);
        }

        public unsafe ObjectReference<Interop.IActivationFactoryVftbl> GetActivationFactory(HString runtimeClassId)
        {
            IntPtr instancePtr;
            Marshal.ThrowExceptionForHR(_GetActivationFactory(runtimeClassId.Handle, out instancePtr));
            return ObjectReference<Interop.IActivationFactoryVftbl>.Attach(ref instancePtr);
        }

        ~DllModule()
        {
            System.Diagnostics.Debug.Assert(_CanUnloadNow() == 0); // S_OK
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

    internal class WeakLazy<T> where T : class, new()
    {
        WeakReference<T> _instance = new WeakReference<T>(null);
        public T Value
        {
            get
            {
                lock (_instance)
                {
                    T value;
                    if (!_instance.TryGetTarget(out value))
                    {
                        value = new T();
                        _instance.SetTarget(value);
                    }
                    return value;
                }
            }
        }
    }

    internal class WinrtModule
    {
        readonly IntPtr _mtaCookie;
        static Lazy<WinrtModule> _instance = new Lazy<WinrtModule>();
        public static WinrtModule Instance => _instance.Value;

        public unsafe WinrtModule()
        {
            IntPtr mtaCookie;
            Marshal.ThrowExceptionForHR(Platform.CoIncrementMTAUsage(&mtaCookie));
            _mtaCookie = mtaCookie;
        }

        public static unsafe ObjectReference<Interop.IActivationFactoryVftbl> GetActivationFactory(HString runtimeClassId)
        {
            var module = Instance; // Ensure COM is initialized
            Guid iid = typeof(Interop.IActivationFactoryVftbl).GUID;
            IntPtr instancePtr;
            Marshal.ThrowExceptionForHR(Platform.RoGetActivationFactory(runtimeClassId.Handle, ref iid, &instancePtr));
            return ObjectReference<Interop.IActivationFactoryVftbl>.Attach(ref instancePtr);
        }

        ~WinrtModule()
        {
            Marshal.ThrowExceptionForHR(Platform.CoDecrementMTAUsage(_mtaCookie));
        }
    }

    internal class BaseActivationFactory
    {
        private ObjectReference<Interop.IActivationFactoryVftbl> _IActivationFactory;

        public BaseActivationFactory(string typeNamespace, string typeFullName)
        {
            using (var runtimeClassId = new HString(typeFullName.Replace("WinRT", "Windows")))
            {
                int hr = 0;
                try
                {
                    _IActivationFactory = WinrtModule.GetActivationFactory(runtimeClassId);
                    return;
                }
                catch (Exception e)
                {
                    // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
                    hr = e.HResult;
                }

                var moduleName = typeNamespace;
                while (_IActivationFactory == null)
                {
                    try
                    {
                        _IActivationFactory = DllModule.Load(moduleName + ".dll").GetActivationFactory(runtimeClassId);
                    }
                    catch (Exception)
                    {
                        var lastSegment = moduleName.LastIndexOf(".");
                        if (lastSegment <= 0)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }
                        moduleName = moduleName.Remove(lastSegment);
                    }
                }
            }
        }

        public unsafe ObjectReference<I> _ActivateInstance<I>()
        {
            IntPtr instancePtr = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(_IActivationFactory.Vftbl.ActivateInstance(_IActivationFactory.ThisPtr, out instancePtr));
            return ObjectReference<WinRT.IInspectable.Vftbl>.Attach(ref instancePtr).As<I>();
        }

        public ObjectReference<I> _As<I>() => _IActivationFactory.As<I>();
    }

    internal class ActivationFactory<T> : BaseActivationFactory
    {
        public ActivationFactory() : base(typeof(T).Namespace, typeof(T).FullName) { }

        static WeakLazy<ActivationFactory<T>> _factory = new WeakLazy<ActivationFactory<T>>();
        public static ObjectReference<I> As<I>() => _factory.Value._As<I>();
        public static ObjectReference<I> ActivateInstance<I>() => _factory.Value._ActivateInstance<I>();
    }

    public class Delegate
    {
        int _refs = 0;
        public readonly IntPtr ThisPtr;

        protected static Delegate FindObject(IntPtr thisPtr)
        {
            UnmanagedObject unmanagedObject = Marshal.PtrToStructure<UnmanagedObject>(thisPtr);
            GCHandle thisHandle = GCHandle.FromIntPtr(unmanagedObject._gchandlePtr);
            return (Delegate)thisHandle.Target;
        }

        // IUnknown
        static unsafe readonly Interop.IUnknownVftbl._QueryInterface _QueryInterface = new Interop.IUnknownVftbl._QueryInterface(QueryInterface);
        static readonly Interop.IUnknownVftbl._AddRef _AddRef = new Interop.IUnknownVftbl._AddRef(AddRef);
        static readonly Interop.IUnknownVftbl._Release _Release = new Interop.IUnknownVftbl._Release(Release);

        static unsafe int QueryInterface([In] IntPtr thisPtr, [In] ref Guid iid, [Out] out IntPtr obj)
        {
            // TODO: verify iid
            AddRef(thisPtr);
            obj = thisPtr;
            return 0; // S_OK;
        }

        static uint AddRef([In] IntPtr thisPtr)
        {
            return FindObject(thisPtr).AddRef();
        }

        static uint Release([In] IntPtr thisPtr)
        {
            return FindObject(thisPtr).Release();
        }

        // IUnknown
        uint AddRef()
        {
            return (uint)System.Threading.Interlocked.Increment(ref _refs);
        }

        uint Release()
        {
            if (_refs == 0)
            {
                throw new InvalidOperationException("WinRT.Delegate has been over-released!");
            }

            var refs = System.Threading.Interlocked.Decrement(ref _refs);
            if (refs == 0)
            {
                _Dispose();
            }
            return (uint)refs;
        }

        public static int MarshalInvoke<T>(IntPtr thisPtr, Action<T> invoke)
        {
            try
            {
                using (new Mono.ThreadContext())
                {
                    var target_invoke = (T)FindObject(thisPtr)._weakInvoker.Target;
                    if (target_invoke != null)
                    {
                        invoke(target_invoke);
                    }
                    return 0; // S_OK;
                }
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

        static Interop.IDelegateVftbl _vftblTemplate;
        static Delegate()
        {
            // lay out the vftable
            _vftblTemplate.QueryInterface = Marshal.GetFunctionPointerForDelegate(_QueryInterface);
            _vftblTemplate.AddRef = Marshal.GetFunctionPointerForDelegate(_AddRef);
            _vftblTemplate.Release = Marshal.GetFunctionPointerForDelegate(_Release);
            _vftblTemplate.Invoke = IntPtr.Zero;
        }

        struct UnmanagedObject
        {
            public IntPtr _vftblPtr;
            public IntPtr _gchandlePtr;
        }

        readonly GCHandle _thisHandle;
        readonly WeakReference _weakInvoker = new WeakReference(null);
        readonly UnmanagedObject _unmanagedObj;

        public class InitialReference : IDisposable
        {
            Delegate _delegate;
            public IntPtr DelegatePtr => _delegate.ThisPtr;
            public InitialReference(IntPtr invoke, object invoker)
            {
                _delegate = new Delegate(invoke, invoker);
                _delegate.AddRef();
            }

            ~InitialReference()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (_delegate != null)
                {
                    _delegate.Release();
                    _delegate = null;
                }
                GC.SuppressFinalize(this);
            }
        }

        public Delegate(MulticastDelegate nativeInvoke, MulticastDelegate managedDelegate) :
            this(Marshal.GetFunctionPointerForDelegate(nativeInvoke), managedDelegate)
        { }

        public Delegate(IntPtr invoke_method, object target_invoker)
        {
            var module = WinrtModule.Instance; // Ensure COM is initialized

            var vftbl = _vftblTemplate;
            vftbl.Invoke = invoke_method;

            _unmanagedObj._vftblPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(_vftblTemplate));
            Marshal.StructureToPtr(vftbl, _unmanagedObj._vftblPtr, false);

            _weakInvoker.Target = target_invoker;
            _thisHandle = GCHandle.Alloc(this);
            _unmanagedObj._gchandlePtr = GCHandle.ToIntPtr(_thisHandle);

            ThisPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(_unmanagedObj));
            Marshal.StructureToPtr(_unmanagedObj, ThisPtr, false);
        }

        ~Delegate()
        {
            _Dispose();
        }

        public void _Dispose()
        {
            if (_refs != 0)
            {
                throw new InvalidOperationException("WinRT.Delegate has been leaked!");
            }

            Marshal.FreeCoTaskMem(ThisPtr);
            _thisHandle.Free();

            GC.SuppressFinalize(this);
        }
    }

    public struct MarshaledValue<T>
    {
        public MarshaledValue(IntPtr interopValue)
        {
            this.InteropValue = interopValue;
        }

        public IntPtr InteropValue
        {
            get;
            private set;
        }
    }

    //public static T UnmarshalValue<T>(MarshaledValue<T> value) where T : unmanaged
    //{
    //    return (T)value.InteropValue;
    //}

    public static class MarshaledValueExtensions
    {
        //public static T Unmarshal<T>(this MarshaledValue<T> value)
        //{
        //    throw new ArgumentException("Unmarshal extension method not specialized for: " + typeof(T).Name);
        //}

        public static T UnmarshalFromNative<T>(this MarshaledValue<T> value) //where T : unmanaged
        {
            return (T)(object)value.InteropValue;
        }

        public static WinRT.HString UnmarshalFromNative(this MarshaledValue<WinRT.HString> value)
        {
            return new WinRT.HString(value.InteropValue);
        }
    }

    internal class EventSource
    {
        delegate void Managed_Invoke();
        delegate int Native_Invoke([In] IntPtr thisPtr);
        static Native_Invoke native_invoke = (IntPtr thisPtr) =>
            Delegate.MarshalInvoke(thisPtr, (Managed_Invoke managed_invoke) => managed_invoke());

        readonly IObjectReference _obj;
        readonly Interop._add_EventHandler _addHandler;
        readonly Interop._remove_EventHandler _removeHandler;

        private Interop.EventRegistrationToken _token;
        private event EventHandler _event;
        public event EventHandler Event
        {
            add
            {
                lock (this)
                {
                    if (_event == null)
                        using (var reference = new Delegate.InitialReference(Marshal.GetFunctionPointerForDelegate(native_invoke), new Managed_Invoke(Invoke)))
                        {
                            Interop.EventRegistrationToken token;
                            unsafe { Marshal.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, reference.DelegatePtr, out token)); }
                            _token = token;
                        }
                    _event += value;
                }
            }
            remove
            {
                _event -= value;
                if (_event == null)
                {
                    _Unsubscribe();
                }
            }
        }

        internal EventSource(IObjectReference obj, Interop._add_EventHandler addHandler, Interop._remove_EventHandler removeHandler)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
        }

        ~EventSource()
        {
            _Unsubscribe();
        }

        void Invoke()
        {
            _event?.Invoke();
        }

        void _Unsubscribe()
        {
            Marshal.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, _token));
            _token.Value = 0;
        }
    }

    delegate int Native_Invoke1([In] IntPtr thisPtr, [In] IntPtr arg1);
    internal class EventSource<A1>
    {
        delegate void Managed_Invoke(IntPtr arg1Ptr);
        static Native_Invoke1 native_invoke = (IntPtr thisPtr, IntPtr arg1Ptr) =>
            Delegate.MarshalInvoke(thisPtr, (Managed_Invoke managed_invoke) => managed_invoke(arg1Ptr));

        internal delegate A1 UnmarshalArg1(IntPtr arg1Ptr);

        readonly IObjectReference _obj;
        readonly Interop._add_EventHandler _addHandler;
        readonly Interop._remove_EventHandler _removeHandler;
        readonly UnmarshalArg1 _unmarshalArg1;

        private Interop.EventRegistrationToken _token;
        private event EventHandler<A1> _event;
        public event EventHandler<A1> Event
        {
            add
            {
                lock (this)
                {
                    if (_event == null)
                        using (var reference = new Delegate.InitialReference(Marshal.GetFunctionPointerForDelegate(native_invoke), new Managed_Invoke(Invoke)))
                        {
                            Interop.EventRegistrationToken token;
                            unsafe { Marshal.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, reference.DelegatePtr, out token)); }
                            _token = token;
                        }
                    _event += value;
                }
            }
            remove
            {
                _event -= value;
                if (_event == null)
                {
                    _Unsubscribe();
                }
            }
        }

        internal EventSource(IObjectReference obj, Interop._add_EventHandler addHandler, Interop._remove_EventHandler removeHandler, UnmarshalArg1 unmarshalArg1)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _unmarshalArg1 = unmarshalArg1;
        }

        ~EventSource()
        {
            _Unsubscribe();
        }

        void Invoke(IntPtr arg1Ptr)
        {
            _event?.Invoke(_unmarshalArg1(arg1Ptr));
        }

        void _Unsubscribe()
        {
            Marshal.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, _token));
            _token.Value = 0;
        }
    }

    delegate int Native_Invoke2([In] IntPtr thisPtr, [In] IntPtr arg1, [In] IntPtr arg2);
    internal class EventSource<A1, A2>
    {
        delegate void Managed_Invoke(IntPtr arg1Ptr, IntPtr arg2Ptr);
        static Native_Invoke2 native_invoke = (IntPtr thisPtr, IntPtr arg1Ptr, IntPtr arg2Ptr) =>
            Delegate.MarshalInvoke(thisPtr, (Managed_Invoke managed_invoke) => managed_invoke(arg1Ptr, arg2Ptr));

        internal delegate A1 UnmarshalArg1(IntPtr arg1Ptr);
        internal delegate A2 UnmarshalArg2(IntPtr arg2Ptr);

        readonly IObjectReference _obj;
        readonly Interop._add_EventHandler _addHandler;
        readonly Interop._remove_EventHandler _removeHandler;
        readonly UnmarshalArg1 _unmarshalArg1;
        readonly UnmarshalArg2 _unmarshalArg2;

        private Interop.EventRegistrationToken _token;
        private event EventHandler<A1, A2> _event;
        public event EventHandler<A1, A2> Event
        {
            add
            {
                lock (this)
                {
                    if (_event == null)
                        using (var reference = new Delegate.InitialReference(Marshal.GetFunctionPointerForDelegate(native_invoke), new Managed_Invoke(Invoke)))
                        {
                            Interop.EventRegistrationToken token;
                            unsafe { Marshal.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, reference.DelegatePtr, out token)); }
                            _token = token;
                        }
                    _event += value;
                }
            }
            remove
            {
                _event -= value;
                if (_event == null)
                {
                    _Unsubscribe();
                }
            }
        }

        internal EventSource(IObjectReference obj, Interop._add_EventHandler addHandler, Interop._remove_EventHandler removeHandler, UnmarshalArg1 unmarshalArg1, UnmarshalArg2 unmarshalArg2)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _unmarshalArg1 = unmarshalArg1;
            _unmarshalArg2 = unmarshalArg2;
        }

        ~EventSource()
        {
            _Unsubscribe();
        }

        void Invoke(IntPtr arg1Ptr, IntPtr arg2Ptr)
        {
            _event?.Invoke(_unmarshalArg1(arg1Ptr), _unmarshalArg2(arg2Ptr));
            //_event?.Invoke(Sender, new MarshaledValue<A>(argsPtr).Unmarshal());
        }

        void _Unsubscribe()
        {
            Marshal.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, _token));
            _token.Value = 0;
        }
    }

    delegate int Native_Invoke3([In] IntPtr thisPtr, [In] IntPtr arg1, [In] IntPtr arg2, [In] IntPtr arg3);
    internal class EventSource<A1, A2, A3>
    {
        delegate void Managed_Invoke(IntPtr arg1Ptr, IntPtr arg2Ptr, IntPtr arg3Ptr);
        static Native_Invoke3 native_invoke = (IntPtr thisPtr, IntPtr arg1Ptr, IntPtr arg2Ptr, IntPtr arg3Ptr) =>
            Delegate.MarshalInvoke(thisPtr, (Managed_Invoke managed_invoke) => managed_invoke(arg1Ptr, arg2Ptr, arg3Ptr));

        internal delegate A1 UnmarshalArg1(IntPtr arg1Ptr);
        internal delegate A2 UnmarshalArg2(IntPtr arg2Ptr);
        internal delegate A3 UnmarshalArg3(IntPtr arg3Ptr);

        readonly IObjectReference _obj;
        readonly Interop._add_EventHandler _addHandler;
        readonly Interop._remove_EventHandler _removeHandler;
        readonly UnmarshalArg1 _unmarshalArg1;
        readonly UnmarshalArg2 _unmarshalArg2;
        readonly UnmarshalArg3 _unmarshalArg3;

        private Interop.EventRegistrationToken _token;
        private event EventHandler<A1, A2, A3> _event;
        public event EventHandler<A1, A2, A3> Event
        {
            add
            {
                lock (this)
                {
                    if (_event == null)
                        using (var reference = new Delegate.InitialReference(Marshal.GetFunctionPointerForDelegate(native_invoke), new Managed_Invoke(Invoke)))
                        {
                            Interop.EventRegistrationToken token;
                            unsafe { Marshal.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, reference.DelegatePtr, out token)); }
                            _token = token;
                        }
                    _event += value;
                }
            }
            remove
            {
                _event -= value;
                if (_event == null)
                {
                    _Unsubscribe();
                }
            }
        }

        internal EventSource(IObjectReference obj, Interop._add_EventHandler addHandler, Interop._remove_EventHandler removeHandler, UnmarshalArg1 unmarshalArg1, UnmarshalArg2 unmarshalArg2, UnmarshalArg3 unmarshalArg3)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _unmarshalArg1 = unmarshalArg1;
            _unmarshalArg2 = unmarshalArg2;
            _unmarshalArg3 = unmarshalArg3;
        }

        ~EventSource()
        {
            _Unsubscribe();
        }

        void Invoke(IntPtr arg1Ptr, IntPtr arg2Ptr, IntPtr arg3Ptr)
        {
            _event?.Invoke(_unmarshalArg1(arg1Ptr), _unmarshalArg2(arg2Ptr), _unmarshalArg3(arg2Ptr));
        }

        void _Unsubscribe()
        {
            Marshal.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, _token));
            _token.Value = 0;
        }
    }

    public static class TypeExtensions
    {
        public static bool IsDelegate(this Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }

    public class Marshaler<T>
    {
        public Marshaler()
        {
            Type type = typeof(T);
            if (!type.IsClass)
            {
                throw new InvalidOperationException("marshaling not needed for value types (todo: structs)");
            }
            Type factory = type.IsDelegate() ? Type.GetType(type.FullName + "Extensions") : type;
            FromNative = MakeFromNativeCall(factory);
            ToNative = MakeToNativeCall(factory);
        }
        public readonly Func<IntPtr, T> FromNative;
        public readonly Func<T, IntPtr> ToNative;

        private static Func<IntPtr, T> MakeFromNativeCall(Type type)
        {
            var method = type.GetMethod("FromNative");
            var methodParams = new[] { Expression.Parameter(typeof(IntPtr), "@this") };
            var methodCall = Expression.Lambda<Func<IntPtr, T>>(
                Expression.Call(method, methodParams), methodParams).Compile();
            return methodCall;
        }

        private static Func<T, IntPtr> MakeToNativeCall(Type type)
        {
            var method = type.GetMethod("ToNative");
            var methodParams = new[] { Expression.Parameter(typeof(T), "@this") };
            var methodCall = Expression.Lambda<Func<T, IntPtr>>(
                Expression.Call(method, methodParams), methodParams).Compile();
            return methodCall;
        }
    }

    public static class GuidGenerator
    {
        private static Type GetGuidType(Type type)
        {
            if (type.IsDelegate())
            {
                var type_name = type.FullName;
                if (type.IsGenericType)
                {
                    var backtick = type_name.IndexOf('`');
                    type_name = type_name.Substring(0, backtick) + "Helper`" + type_name.Substring(backtick + 1);
                }
                else
                {
                    type_name += "Helper";
                }
                return Type.GetType(type_name);
            }
            return type;
        }

        public static Guid GetGUID(Type type)
        {
            return GetGuidType(type).GUID;
        }

        public static Guid GetIID(Type type)
        {
            type = GetGuidType(type);
            if (!type.IsGenericType)
            {
                return type.GUID;
            }
            return (Guid)type.GetField("PIID").GetValue(null);
        }

        public static string GetSignature(Type type)
        {
            if (type == typeof(IInspectable))
            {
                return "cinterface(IInspectable)";
            }

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments().Select(t => GetSignature(t));
                return "pinterface({" + GetGUID(type) + "};" + String.Join(";", args) + ")";
            }

            if (type.IsValueType)
            {
                switch (type.Name)
                {
                    case "SByte": return "i1";
                    case "Byte": return "u1";
                    case "Int16": return "i2";
                    case "UInt16": return "u2";
                    case "Int32": return "i4";
                    case "UInt32": return "u4";
                    case "Int64": return "i8";
                    case "UInt64": return "u8";
                    case "Single": return "f4";
                    case "Double": return "f8";
                    case "Boolean": return "b1";
                    case "Char": return "c2";
                    case "Guid": return "g16";
                    default:
                        {
                            if (type.IsEnum)
                            {
                                var isFlags = type.CustomAttributes.Any(cad => cad.AttributeType == typeof(FlagsAttribute));
                                return "enum(" + type.FullName + ";" + (isFlags ? "u4" : "i4") + ")";
                            }
                            if (!type.IsPrimitive)
                            {
                                var args = type.GetFields().Select(fi => GetSignature(fi.FieldType));
                                return "struct(" + type.FullName + ";" + String.Join(";", args) + ")";
                            }
                            throw new InvalidOperationException("unsupported value type");
                        }
                }
            }

            if (type == typeof(String))
            {
                return "string";
            }

            var _default = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault((FieldInfo fi) => fi.Name == "_default");
            if (_default != null)
            {
                return "rc(" + type.FullName + ";" + GetSignature(_default.FieldType) + ")";
            }

            if (type.IsDelegate())
            {
                return "delegate({" + GetGUID(type) + "})";
            }

            return "{" + type.GUID.ToString() + "}";
        }

        private static Guid encode_guid(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
            {
                // swap bytes of int a
                byte t = data[0];
                data[0] = data[3];
                data[3] = t;
                t = data[1];
                data[1] = data[2];
                data[2] = t;
                // swap bytes of short b
                t = data[4];
                data[4] = data[5];
                data[5] = t;
                // swap bytes of short c and encode rfc time/version field
                t = data[6];
                data[6] = data[7];
                data[7] = (byte)((t & 0x0f) | (5 << 4));
                // encode rfc clock/reserved field
                data[8] = (byte)((data[8] & 0x3f) | 0x80);
            }
            return new Guid(data.Take(16).ToArray());
        }

        private static Guid wrt_pinterface_namespace = new Guid("d57af411-737b-c042-abae-878b1e16adee");

        public static Guid CreateIID(Type type)
        {
            var sig = GetSignature(type);
            if (!type.IsGenericType)
            {
                return new Guid(sig);
            }
            var data = wrt_pinterface_namespace.ToByteArray().Concat(UTF8Encoding.UTF8.GetBytes(sig)).ToArray();
            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                var hash = sha.ComputeHash(data);
                return encode_guid(hash);
            }
        }
    }
}

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
    using System.Diagnostics;
    using WinRT.Interop;

    public enum TrustLevel
    {
        BaseTrust = 0,
        PartialTrust = BaseTrust + 1,
        FullTrust = PartialTrust + 1
    };


    public static class TypeExtensions
    {
        public static Type FindHelperType(this Type type)
        {
            return Type.GetType($"ABI.{type.FullName}");
        }

        public static Type GetHelperType(this Type type)
        {
            return type.FindHelperType() ?? throw new InvalidOperationException("Target type is not a projected type.");
        }
    }

    public static class DelegateExtensions
    {
        public static bool IsDelegate(this Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }

        public static void DynamicInvokeAbi(this System.Delegate del, object[] invoke_params)
        {
            Marshal.ThrowExceptionForHR((int)del.DynamicInvoke(invoke_params));
        }

        public static T AsDelegate<T>(this MulticastDelegate del)
        {
            return Marshal.GetDelegateForFunctionPointer<T>(
                Marshal.GetFunctionPointerForDelegate(del));
        }
    }

    namespace Interop
    {
        // IUnknown
        [Guid("00000000-0000-0000-C000-000000000046")]
        public struct IUnknownVftbl
        {
            public unsafe delegate int _QueryInterface(IntPtr pThis, ref Guid iid, out IntPtr vftbl);
            public delegate uint _AddRef(IntPtr pThis);
            public delegate uint _Release(IntPtr pThis);

            public _QueryInterface QueryInterface;
            public _AddRef AddRef;
            public _Release Release;
        }

        // IActivationFactory
        [Guid("00000035-0000-0000-C000-000000000046")]
        public struct IActivationFactoryVftbl
        {
            public unsafe delegate int _ActivateInstance(IntPtr pThis, out IntPtr instance);

            public IInspectable.Vftbl IInspectableVftbl;
            public _ActivateInstance ActivateInstance;
        }

        // standard accessors/mutators
        public unsafe delegate int _get_PropertyAsBoolean(IntPtr thisPtr, out byte value);
        public delegate int _put_PropertyAsBoolean(IntPtr thisPtr, byte value);
        public unsafe delegate int _get_PropertyAsChar(IntPtr thisPtr, out char value);
        public delegate int _put_PropertyAsChar(IntPtr thisPtr, char value);
        public unsafe delegate int _get_PropertyAsSByte(IntPtr thisPtr, out sbyte value);
        public delegate int _put_PropertyAsSByte(IntPtr thisPtr, sbyte value);
        public unsafe delegate int _get_PropertyAsByte(IntPtr thisPtr, out byte value);
        public delegate int _put_PropertyAsByte(IntPtr thisPtr, byte value);
        public unsafe delegate int _get_PropertyAsInt16(IntPtr thisPtr, out short value);
        public delegate int _put_PropertyAsInt16(IntPtr thisPtr, short value);
        public unsafe delegate int _get_PropertyAsUInt16(IntPtr thisPtr, out ushort value);
        public delegate int _put_PropertyAsUInt16(IntPtr thisPtr, ushort value);
        public unsafe delegate int _get_PropertyAsInt32(IntPtr thisPtr, out int value);
        public delegate int _put_PropertyAsInt32(IntPtr thisPtr, int value);
        public unsafe delegate int _get_PropertyAsUInt32(IntPtr thisPtr, out uint value);
        public delegate int _put_PropertyAsUInt32(IntPtr thisPtr, uint value);
        public unsafe delegate int _get_PropertyAsInt64(IntPtr thisPtr, out long value);
        public delegate int _put_PropertyAsInt64(IntPtr thisPtr, long value);
        public unsafe delegate int _get_PropertyAsUInt64(IntPtr thisPtr, out ulong value);
        public delegate int _put_PropertyAsUInt64(IntPtr thisPtr, ulong value);
        public unsafe delegate int _get_PropertyAsFloat(IntPtr thisPtr, out float value);
        public delegate int _put_PropertyAsFloat(IntPtr thisPtr, float value);
        public unsafe delegate int _get_PropertyAsDouble(IntPtr thisPtr, out double value);
        public delegate int _put_PropertyAsDouble(IntPtr thisPtr, double value);
        public unsafe delegate int _get_PropertyAsObject(IntPtr thisPtr, out IntPtr value);
        public delegate int _put_PropertyAsObject(IntPtr thisPtr, IntPtr value);
        public unsafe delegate int _get_PropertyAsGuid(IntPtr thisPtr, out Guid value);
        public delegate int _put_PropertyAsGuid(IntPtr thisPtr, Guid value);
        public unsafe delegate int _get_PropertyAsString(IntPtr thisPtr, out IntPtr value);
        public delegate int _put_PropertyAsString(IntPtr thisPtr, IntPtr value);
        public unsafe delegate int _get_PropertyAsVector3(IntPtr thisPtr, out Windows.Foundation.Numerics.Vector3 value);
        public delegate int _put_PropertyAsVector3(IntPtr thisPtr, Windows.Foundation.Numerics.Vector3 value);
        public unsafe delegate int _get_PropertyAsQuaternion(IntPtr thisPtr, out Windows.Foundation.Numerics.Quaternion value);
        public delegate int _put_PropertyAsQuaternion(IntPtr thisPtr, Windows.Foundation.Numerics.Quaternion value);
        public unsafe delegate int _get_PropertyAsMatrix4x4(IntPtr thisPtr, out Windows.Foundation.Numerics.Matrix4x4 value);
        public delegate int _put_PropertyAsMatrix4x4(IntPtr thisPtr, Windows.Foundation.Numerics.Matrix4x4 value);
        public unsafe delegate int _add_EventHandler(IntPtr thisPtr, IntPtr handler, out EventRegistrationToken token);
        public delegate int _remove_EventHandler(IntPtr thisPtr, EventRegistrationToken token);

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
            public delegate int _GetIids(IntPtr pThis, uint iidCount, Guid[] iids);
            public delegate int _GetRuntimeClassName(IntPtr pThis, IntPtr className);
            public delegate int _GetTrustLevel(IntPtr pThis, TrustLevel trustLevel);

            public IUnknownVftbl IUnknownVftbl;
            public _GetIids GetIids;
            public _GetRuntimeClassName GetRuntimeClassName;
            public _GetTrustLevel GetTrustLevel;
        }

        // Factor IInspectable as 'object' projection, and move out to ABI.WinRT.IInspectable:
        public static IInspectable FromAbi(IntPtr thisPtr) =>
            new IInspectable(ObjectReference<Vftbl>.FromAbi(thisPtr));
        public static void DisposeAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero) return;
            // TODO: this should be a direct v-table call when function pointers are a thing
            ObjectReference<IInspectable.Vftbl>.Attach(ref thisPtr);
            thisPtr = IntPtr.Zero;
        }
        public static IntPtr FromManaged(IInspectable value) => value._obj.GetRef();

        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public static implicit operator IInspectable(IObjectReference obj) => obj.As<Vftbl>();
        public static implicit operator IInspectable(ObjectReference<Vftbl> obj) => new IInspectable(obj);
        public ObjectReference<I> As<I>() => _obj.As<I>();
        public IInspectable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IInspectable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }
        public object _WinRT_Owner { get; set; }
    }

    internal class Platform
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern int CoDecrementMTAUsage(IntPtr cookie);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

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
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateStringReference(char* sourceString,
                                                  int length,
                                                  IntPtr* hstring_header,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsDuplicateString(IntPtr sourceString,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);
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

        public IntPtr GetRef()
        {
            VftblIUnknown.AddRef(ThisPtr);
            return ThisPtr;
        }
    }

    public class ObjectReference<T> : IObjectReference
    {
        protected override IUnknownVftbl VftblIUnknown => _vftblIUnknown;
        readonly IUnknownVftbl _vftblIUnknown;
        public readonly T Vftbl;

        public static ObjectReference<T> Attach(ref IntPtr thisPtr)
        {
            var obj = new ObjectReference<T>(thisPtr);
            thisPtr = IntPtr.Zero;
            return obj;
        }

        ObjectReference(IntPtr thisPtr, IUnknownVftbl vftblIUnknown, T vftblT) :
            base(thisPtr)
        {
            _vftblIUnknown = vftblIUnknown;
            Vftbl = vftblT;
        }

        ObjectReference(IntPtr thisPtr) :
            this(thisPtr, GetVtables(thisPtr))
        {
        }

        ObjectReference(IntPtr thisPtr, (IUnknownVftbl vftblIUnknown, T vftblT) vtables) :
            this(thisPtr, vtables.vftblIUnknown, vtables.vftblT)
        {
        }

        public static ObjectReference<T> FromAbi(IntPtr thisPtr, IUnknownVftbl vftblIUnknown, T vftblT)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new ObjectReference<T>(thisPtr, vftblIUnknown, vftblT);
            obj._vftblIUnknown.AddRef(obj.ThisPtr);
            return obj;
        }

        public static ObjectReference<T> FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var (vftblIUnknown, vftblT) = GetVtables(thisPtr);
            return FromAbi(thisPtr, vftblIUnknown, vftblT);
        }

        // C# doesn't allow us to express that T contains IUnknownVftbl, so we'll use a tuple
        private static (IUnknownVftbl vftblIUnknown, T vftblT) GetVtables(IntPtr thisPtr)
        {
            var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
            var vftblIUnknown = Marshal.PtrToStructure<IUnknownVftbl>(vftblPtr.Vftbl);
            // TODO: need to delegate back to the T implementation for generics ...
            T vftblT;
            if (typeof(T).IsGenericType)
            {
                vftblT = (T)typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new[] { typeof(IntPtr) }, null).Invoke(new object[] { thisPtr });
            }
            else
            {
                vftblT = Marshal.PtrToStructure<T>(vftblPtr.Vftbl);
            }
            return (vftblIUnknown, vftblT);
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
            IntPtr activatableClassId,
            out IntPtr activationFactory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate int DllCanUnloadNow();

        readonly string _fileName;
        readonly IntPtr _moduleHandle;
        readonly DllGetActivationFactory _GetActivationFactory;
        readonly DllCanUnloadNow _CanUnloadNow; // TODO: Eventually periodically call

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

            var canUnloadNow = Platform.GetProcAddress(_moduleHandle, "DllCanUnloadNow");
            if (canUnloadNow != IntPtr.Zero)
            {
                _CanUnloadNow = Marshal.GetDelegateForFunctionPointer<DllCanUnloadNow>(canUnloadNow);
            }
        }

        public unsafe (ObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
        {
            IntPtr instancePtr;
            var hstrRuntimeClassId = MarshalString.CreateMarshaler(runtimeClassId);
            int hr = _GetActivationFactory(MarshalString.GetAbi(hstrRuntimeClassId), out instancePtr);
            return (hr == 0 ? ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr) : null, hr);
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

        public static unsafe (ObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
        {
            var module = Instance; // Ensure COM is initialized
            Guid iid = typeof(IActivationFactoryVftbl).GUID;
            IntPtr instancePtr;
            var hstrRuntimeClassId = MarshalString.CreateMarshaler(runtimeClassId);
            int hr = Platform.RoGetActivationFactory(MarshalString.GetAbi(hstrRuntimeClassId), ref iid, &instancePtr);
            return (hr == 0 ? ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr) : null, hr);
        }

        ~WinrtModule()
        {
            Marshal.ThrowExceptionForHR(Platform.CoDecrementMTAUsage(_mtaCookie));
        }
    }

    internal class BaseActivationFactory
    {
        private ObjectReference<IActivationFactoryVftbl> _IActivationFactory;

        public BaseActivationFactory(string typeNamespace, string typeFullName)
        {
            var runtimeClassId = typeFullName.Replace("WinRT", "Windows");

            // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
            int hr;
            (_IActivationFactory, hr) = WinrtModule.GetActivationFactory(runtimeClassId);
            if (_IActivationFactory != null) { return; }

            var moduleName = typeNamespace;
            while (true)
            {
                try
                {
                    (_IActivationFactory, _) = DllModule.Load(moduleName + ".dll").GetActivationFactory(runtimeClassId);
                    if (_IActivationFactory != null) { return; }
                }
                catch (Exception) { }

                var lastSegment = moduleName.LastIndexOf(".");
                if (lastSegment <= 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                moduleName = moduleName.Remove(lastSegment);
            }
        }

        public unsafe ObjectReference<I> _ActivateInstance<I>()
        {
            IntPtr instancePtr = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(_IActivationFactory.Vftbl.ActivateInstance(_IActivationFactory.ThisPtr, out instancePtr));
            return ObjectReference<IInspectable.Vftbl>.Attach(ref instancePtr).As<I>();
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
        int _refs = 1;
        public readonly IntPtr ThisPtr;

        public static Delegate FindObject(IntPtr thisPtr)
        {
            UnmanagedObject unmanagedObject = Marshal.PtrToStructure<UnmanagedObject>(thisPtr);
            GCHandle thisHandle = GCHandle.FromIntPtr(unmanagedObject._gchandlePtr);
            return (Delegate)thisHandle.Target;
        }

        // IUnknown
        static unsafe readonly IUnknownVftbl._QueryInterface _QueryInterface = new IUnknownVftbl._QueryInterface(QueryInterface);
        static readonly IUnknownVftbl._AddRef _AddRef = new IUnknownVftbl._AddRef(AddRef);
        static readonly IUnknownVftbl._Release _Release = new IUnknownVftbl._Release(Release);

        static unsafe int QueryInterface(IntPtr thisPtr, ref Guid iid, out IntPtr obj)
        {
            const int E_NOINTERFACE = unchecked((int)0x80040002);

            if (iid == typeof(IUnknownVftbl).GUID)
            {
                AddRef(thisPtr);
                obj = thisPtr;
                return 0; // S_OK;
            }

            obj = IntPtr.Zero;
            return E_NOINTERFACE;
        }

        static uint AddRef(IntPtr thisPtr)
        {
            return FindObject(thisPtr).AddRef();
        }

        static uint Release(IntPtr thisPtr)
        {
            return FindObject(thisPtr).Release();
        }

        // IUnknown
        uint AddRef()
        {
            return (uint)System.Threading.Interlocked.Increment(ref _refs);
        }

        // Release is public to enable users of this instance to release the managed reference.
        public uint Release()
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

        static IDelegateVftbl _vftblTemplate;
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

    internal class EventSource<TDelegate>
        where TDelegate : class, MulticastDelegate
    {
        readonly IObjectReference _obj;
        readonly _add_EventHandler _addHandler;
        readonly _remove_EventHandler _removeHandler;

        private EventRegistrationToken _token;
        private TDelegate _event;

        public void Subscribe(TDelegate del)
        {
            lock (this)
            {
                if (_event == null)
                {
                    var marshaler = Marshaler<TDelegate>.CreateMarshaler((TDelegate)EventInvoke);
                    try
                    {
                        var nativeDelegate = (IntPtr)Marshaler<TDelegate>.GetAbi(marshaler);
                        Marshal.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, nativeDelegate, out EventRegistrationToken token));
                        _token = token;
                    }
                    finally
                    {
                        // Dispose our managed reference to the delegate's CCW.
                        // The either native event holds a reference now or the _addHandler call failed.
                        Marshaler<TDelegate>.DisposeMarshaler(marshaler);
                    }
                }
                _event = (TDelegate)global::System.Delegate.Combine(_event, del);
            }
        }

        public void Unsubscribe(TDelegate del)
        {
            lock (this)
            {
                _event = (TDelegate)global::System.Delegate.Remove(_event, del);
            }
            if (_event == null)
            {
                _UnsubscribeFromNative();
            }
        }

        private System.Delegate _eventInvoke;
        private System.Delegate EventInvoke
        {
            get
            {
                if (_eventInvoke is object)
                {
                    return _eventInvoke;
                }

                MethodInfo invoke = typeof(TDelegate).GetMethod("Invoke");
                ParameterInfo[] invokeParameters = invoke.GetParameters();
                ParameterExpression[] parameters = new ParameterExpression[invokeParameters.Length];
                for (int i = 0; i < invokeParameters.Length; i++)
                {
                    parameters[i] = Expression.Parameter(invokeParameters[i].ParameterType, invokeParameters[i].Name);
                }

                ParameterExpression delegateLocal = Expression.Parameter(typeof(TDelegate), "event");

                _eventInvoke = Expression.Lambda(typeof(TDelegate),
                    Expression.Block(
                        new[] { delegateLocal },
                        Expression.Assign(delegateLocal, Expression.Field(Expression.Constant(this), typeof(EventSource<TDelegate>).GetField(nameof(_event), BindingFlags.Instance | BindingFlags.NonPublic))),
                        Expression.IfThen(
                            Expression.ReferenceNotEqual(delegateLocal, Expression.Constant(null, typeof(TDelegate))), Expression.Call(delegateLocal, invoke, parameters))),
                    parameters).Compile();

                return _eventInvoke;
            }
        }

        internal EventSource(IObjectReference obj, _add_EventHandler addHandler, _remove_EventHandler removeHandler)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
        }

        ~EventSource()
        {
            _UnsubscribeFromNative();
        }

        void _UnsubscribeFromNative()
        {
            Marshal.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, _token));
            _token.Value = 0;
        }
    }
}

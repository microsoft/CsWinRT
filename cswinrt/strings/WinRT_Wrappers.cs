using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    public static partial class ComWrappersSupport
    {
        public static TReturn MarshalDelegateInvoke<TDelegate, TReturn>(IntPtr thisPtr, Func<TDelegate, TReturn> invoke)
            where TDelegate : class, System.Delegate
        {
            using (new Mono.ThreadContext())
            {
                var target_invoke = FindDelegate<TDelegate>(thisPtr);
                if (target_invoke != null)
                {
                    return invoke(target_invoke);
                }
                return default;
            }
        }

        public static void MarshalDelegateInvoke<T>(IntPtr thisPtr, Action<T> invoke)
            where T : class, System.Delegate
        {
            using (new Mono.ThreadContext())
            {
                var target_invoke = FindDelegate<T>(thisPtr);
                if (target_invoke != null)
                {
                    invoke(target_invoke);
                }
            }
        }

        internal static List<ComInterfaceEntry> GetInterfaceTableEntries(object obj)
        {
            var entries = new List<ComInterfaceEntry>();
            var interfaces = obj.GetType().GetInterfaces();
            foreach (var iface in interfaces)
            {
                var ifaceAbiType = iface.Assembly.GetType("ABI." + iface.FullName);
                if (ifaceAbiType == null)
                {
                    // This interface isn't a WinRT interface.
                    // TODO: Handle WinRT -> .NET projected interfaces.
                    continue;
                }

                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(ifaceAbiType),
                    Vtable = (IntPtr)ifaceAbiType.GetNestedType("Vftbl").GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }
            return entries;
        }

        internal static (InspectableInfo inspectableInfo, List<ComInterfaceEntry> interfaceTableEntries) PregenerateNativeTypeInformation(object obj)
        {
            string typeName = obj.GetType().FullName;
            if (typeName.StartsWith("ABI.")) // If our type is an ABI type, get the real type name
            {
                typeName = typeName.Substring("ABI.".Length);
            }

            var interfaceTableEntries = GetInterfaceTableEntries(obj);
            var iids = new Guid[interfaceTableEntries.Count];
            for (int i = 0; i < interfaceTableEntries.Count; i++)
            {
                iids[i] = interfaceTableEntries[i].IID;
            }

            return (
                new InspectableInfo(typeName, iids),
                interfaceTableEntries);
        }

        internal class InspectableInfo
        {
            public readonly string RuntimeClassName;
            public readonly Guid[] IIDs;

            internal InspectableInfo(string runtimeClassName, Guid[] iids)
            {
                RuntimeClassName = runtimeClassName;
                IIDs = iids;
            }

        }
    }

#if MANUAL_IUNKNOWN
    public static partial class ComWrappersSupport
    {
        private static ConditionalWeakTable<object, ComCallableWrapper> ComWrapperCache = new ConditionalWeakTable<object, ComCallableWrapper>();
        private static ConditionalWeakTable<object, Delegate> DelegateWrapperCache = new ConditionalWeakTable<object, Delegate>();

        internal static InspectableInfo GetInspectableInfo(IntPtr pThis) => UnmanagedObject.FindObject<ComCallableWrapper>(pThis).InspectableInfo;

        public static IObjectReference CreateCCWForObject(object obj)
        {
            if (obj is global::System.Delegate del)
            {
                // TODO: Handle delegate passed as IInspectable
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(ComWrapperCache.GetValue(obj, _ => new ComCallableWrapper(obj)).IdentityPtr);
        }

        public static IObjectReference CreateCCWForDelegate(IntPtr invoke, global::System.Delegate del)
        {
            return ObjectReference<IDelegateVftbl>.FromAbi(DelegateWrapperCache.GetValue(del, _ => new Delegate(invoke, del)).ThisPtr);
        }

        public static T FindObject<T>(IntPtr thisPtr)
            where T : class =>
            (T)UnmanagedObject.FindObject<ComCallableWrapper>(thisPtr).ManagedObject;

        internal static T FindDelegate<T>(IntPtr thisPtr)
            where T : class, System.Delegate => (T)(UnmanagedObject.FindObject<Delegate>(thisPtr).Target);

        public static void RegisterObjectForInterface(object obj, IntPtr thisPtr)
        {
        }

        public static IUnknownVftbl IUnknownVftbl { get; } = new IUnknownVftbl
        {
            QueryInterface = Do_Abi_QueryInterface,
            AddRef = Do_Abi_AddRef,
            Release = Do_Abi_Release
        };

        private static int Do_Abi_QueryInterface(IntPtr pThis, ref Guid iid, out IntPtr ptr)
        {
            return UnmanagedObject.FindObject<ComCallableWrapper>(pThis).QueryInterface(iid, out ptr);
        }

        private static uint Do_Abi_AddRef(IntPtr pThis)
        {
            return UnmanagedObject.FindObject<ComCallableWrapper>(pThis).AddRef();
        }

        private static uint Do_Abi_Release(IntPtr pThis)
        {
            return UnmanagedObject.FindObject<ComCallableWrapper>(pThis).Release();
        }

        internal static void RemoveReleasedComWrapper(object obj) => ComWrapperCache.Remove(obj);

        internal static void RemoveReleasedComWrapper(global::System.Delegate del) => DelegateWrapperCache.Remove(del);
    }

    struct ComInterfaceEntry
    {
        public IntPtr Vtable;
        public Guid IID;
    }

    struct UnmanagedObject
    {
        public IntPtr _vftblPtr;
        public IntPtr _gchandlePtr;

        internal static T FindObject<T>(IntPtr thisPtr)
        {
            UnmanagedObject unmanagedObject = Marshal.PtrToStructure<UnmanagedObject>(thisPtr);
            GCHandle thisHandle = GCHandle.FromIntPtr(unmanagedObject._gchandlePtr);
            return (T)thisHandle.Target;
        }
    }

    internal class ComCallableWrapper
    {
        private readonly IntPtr _handle;
        private int _refs = 0;
        private Dictionary<Guid, IntPtr> _managedQITable;

        public object ManagedObject { get; }
        public ComWrappersSupport.InspectableInfo InspectableInfo { get; }
        public IntPtr IdentityPtr { get; }

        internal ComCallableWrapper(object obj)
        {
            ManagedObject = obj;
            var (inspectableInfo, interfaceTableEntries) = ComWrappersSupport.PregenerateNativeTypeInformation(ManagedObject);

            InspectableInfo = inspectableInfo;

            interfaceTableEntries.Add(new ComInterfaceEntry
            {
                IID = typeof(IUnknownVftbl).GUID,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });

            interfaceTableEntries.Add(new ComInterfaceEntry
            {
                IID = typeof(IInspectable).GUID,
                Vtable = IInspectable.Vftbl.AbiToProjectionVftablePtr
            });

            _handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
            InitializeManagedQITable(interfaceTableEntries);

            IdentityPtr = _managedQITable[typeof(IUnknownVftbl).GUID];
        }

        private void InitializeManagedQITable(List<ComInterfaceEntry> entries)
        {
            _managedQITable = new Dictionary<Guid, IntPtr>();
            foreach (var entry in entries)
            {
                unsafe
                {
                    UnmanagedObject* ifaceTearOff = (UnmanagedObject*)Marshal.AllocCoTaskMem(sizeof(UnmanagedObject));
                    ifaceTearOff->_vftblPtr = entry.Vtable;
                    ifaceTearOff->_gchandlePtr = _handle;
                    _managedQITable.Add(entry.IID, (IntPtr)ifaceTearOff);
                }
            }
        }

        internal uint AddRef()
        {
            return (uint)System.Threading.Interlocked.Increment(ref _refs);
        }

        internal uint Release()
        {
            if (_refs == 0)
            {
                throw new InvalidOperationException("WinRT.ComCallableWrapper has been over-released!");
            }

            var refs = (uint)System.Threading.Interlocked.Decrement(ref _refs);
            if (refs == 0)
            {
                Cleanup();
            }
            return refs;
        }

        internal int QueryInterface(Guid iid, out IntPtr ptr)
        {
            const int E_NOINTERFACE = unchecked((int)0x80040002);
            if (_managedQITable.TryGetValue(iid, out ptr))
            {
                AddRef();
                return 0;
            }
            return E_NOINTERFACE;
        }

        private void Cleanup()
        {
            ComWrappersSupport.RemoveReleasedComWrapper(ManagedObject);
            foreach (var obj in _managedQITable.Values)
            {
                Marshal.FreeCoTaskMem(obj);
            }
            _managedQITable.Clear();
            GCHandle.FromIntPtr(_handle).Free();
        }
    }

    partial class Delegate
    {
        private static Delegate FindObject(IntPtr thisPtr) => UnmanagedObject.FindObject<Delegate>(thisPtr);

        // IUnknown
        static unsafe readonly IUnknownVftbl._QueryInterface _QueryInterface = new IUnknownVftbl._QueryInterface(QueryInterface);
        static readonly IUnknownVftbl._AddRef _AddRef = new IUnknownVftbl._AddRef(AddRef);
        static readonly IUnknownVftbl._Release _Release = new IUnknownVftbl._Release(Release);

        static unsafe int QueryInterface([In] IntPtr thisPtr, [In] ref Guid iid, [Out] out IntPtr obj)
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

        static IDelegateVftbl _vftblTemplate;
        static Delegate()
        {
            // lay out the vftable
            _vftblTemplate.QueryInterface = Marshal.GetFunctionPointerForDelegate(_QueryInterface);
            _vftblTemplate.AddRef = Marshal.GetFunctionPointerForDelegate(_AddRef);
            _vftblTemplate.Release = Marshal.GetFunctionPointerForDelegate(_Release);
            _vftblTemplate.Invoke = IntPtr.Zero;
        }
    
        int _refs = 0;
        readonly GCHandle _thisHandle;
        readonly UnmanagedObject _unmanagedObj;
        public readonly IntPtr ThisPtr;
        public global::System.Delegate Target { get; }

        public Delegate(MulticastDelegate abiInvoke, MulticastDelegate managedDelegate) :
            this(Marshal.GetFunctionPointerForDelegate(abiInvoke), managedDelegate)
        { }

        public Delegate(IntPtr invoke_method, global::System.Delegate target_invoker)
        {
            _ = WinrtModule.Instance; // Ensure COM is initialized

            var vftbl = _vftblTemplate;
            vftbl.Invoke = invoke_method;

            _unmanagedObj._vftblPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(_vftblTemplate));
            Marshal.StructureToPtr(vftbl, _unmanagedObj._vftblPtr, false);

            Target = target_invoker;
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

            ComWrappersSupport.RemoveReleasedComWrapper(Target);

            Marshal.FreeCoTaskMem(ThisPtr);
            _thisHandle.Free();

            GC.SuppressFinalize(this);
        }
    }
#endif
}

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
        private readonly static ConcurrentDictionary<string, Func<IInspectable, object>> TypedObjectFactoryCache = new ConcurrentDictionary<string, Func<IInspectable, object>>();

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

        internal static Func<IInspectable, object> CreateTypedRcwFactory(string runtimeClassName)
        {
            var (implementationType, _) = FindTypeByName(runtimeClassName);

            Type classType;
            Type interfaceType;
            Type vftblType;
            if (implementationType.IsInterface)
            {
                classType = null;
                interfaceType = FindTypeByName("ABI." + runtimeClassName).type ??
                    throw new TypeLoadException($"Unable to find an ABI implementation for the type '{runtimeClassName}'");
                vftblType = interfaceType.GetNestedType("Vftbl") ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
                if (vftblType.IsGenericTypeDefinition)
                {
                    vftblType = vftblType.MakeGenericType(interfaceType.GetGenericArguments());
                }
            }
            else
            {
                classType = implementationType;
                interfaceType = classType.GetField("_default", BindingFlags.Instance | BindingFlags.NonPublic)?.FieldType;
                if (interfaceType is null)
                {
                    throw new TypeLoadException($"Unable to create a runtime wrapper for a WinRT object of type '{runtimeClassName}'. This type is not a projected type.");
                }
                vftblType = interfaceType.GetNestedType("Vftbl") ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
            }

            ParameterExpression[] parms = new[] { Expression.Parameter(typeof(IInspectable), "inspectable") };
            var createInterfaceInstanceExpression = Expression.New(interfaceType.GetConstructor(new[] { typeof(ObjectReference<>).MakeGenericType(vftblType) }),
                    Expression.Call(parms[0],
                        typeof(IInspectable).GetMethod(nameof(IInspectable.As)).MakeGenericMethod(vftblType)));

            if (classType is null)
            {
                return Expression.Lambda<Func<IInspectable, object>>(createInterfaceInstanceExpression, parms).Compile();
            }

            return Expression.Lambda<Func<IInspectable, object>>(
                Expression.New(classType.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { interfaceType }, null),
                    createInterfaceInstanceExpression),
                parms).Compile();
        }

        private static (Type type, int remaining) FindTypeByName(string runtimeClassName)
        {
            var (genericTypeName, genericTypes, remaining) = ParseGenericTypeName(runtimeClassName);
            return (FindTypeByNameCore(genericTypeName, genericTypes), remaining);
        }

        private static Type FindTypeByNameCore(string runtimeClassName, Type[] genericTypes)
        {
            // TODO: This implementation is a strawman implementation.
            // It's missing support for types not loaded in the default ALC.
            if (genericTypes is null)
            {
                Type primitiveType = ResolvePrimitiveType(runtimeClassName);
                if (primitiveType is object)
                {
                    return primitiveType;
                }
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(runtimeClassName);
                if (type is object)
                {
                    if (genericTypes != null)
                    {
                        type = type.MakeGenericType(genericTypes);
                    }
                    return type;
                }
            }
            throw new TypeLoadException($"Unable to find a type named '{runtimeClassName}'");
        }

        private static Type ResolvePrimitiveType(string primitiveTypeName)
        {
            return primitiveTypeName switch
            {
                "UInt8" => typeof(byte),
                "Int8" => typeof(sbyte),
                "UInt16" => typeof(ushort),
                "Int16" => typeof(short),
                "UInt32" => typeof(uint),
                "Int32" => typeof(int),
                "UInt64" => typeof(ulong),
                "Int64" => typeof(long),
                "Boolean" => typeof(bool),
                "String" => typeof(string),
                "Char" => typeof(char),
                "Single" => typeof(float),
                "Double" => typeof(double),
                "Guid" => typeof(Guid),
                "Object" => typeof(object),
                _ => null
            };
        }

        // TODO: Use types from System.Memory to eliminate allocations of intermediate strings.
        private static (string genericTypeName, Type[] genericTypes, int remaining) ParseGenericTypeName(string partialTypeName)
        {
            int possibleEndOfSimpleTypeName = partialTypeName.IndexOfAny(new[] { ',', '>' });
            int endOfSimpleTypeName = partialTypeName.Length;
            if (possibleEndOfSimpleTypeName != -1)
            {
                endOfSimpleTypeName = possibleEndOfSimpleTypeName;
            }
            string typeName = partialTypeName.Substring(0, endOfSimpleTypeName);

            if (!typeName.Contains('`'))
            {
                return (typeName.ToString(), null, endOfSimpleTypeName);
            }

            int genericTypeListStart = partialTypeName.IndexOf('<');
            string genericTypeName = partialTypeName.Substring(0, genericTypeListStart);
            string remaining = partialTypeName.Substring(genericTypeListStart + 1);
            int remainingIndex = genericTypeListStart + 1;
            List<Type> genericTypes = new List<Type>();
            while (true)
            {
                var (genericType, endOfGenericArgument) = FindTypeByName(remaining);
                remainingIndex += endOfGenericArgument;
                genericTypes.Add(genericType);
                remaining = remaining.Substring(endOfGenericArgument);
                if (remaining[0] == ',')
                {
                    // Skip the comma and the space in the type name.
                    remainingIndex += 2;
                    remaining = remaining.Substring(2);
                    continue;
                }
                else if (remaining[0] == '>')
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException("The provided type name is invalid.");
                }
            }
            return (genericTypeName, genericTypes.ToArray(), partialTypeName.Length - remaining.Length);
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

        private static ConcurrentDictionary<IntPtr, WeakReference<object>> RuntimeWrapperCache = new ConcurrentDictionary<IntPtr, WeakReference<object>>();

        internal static InspectableInfo GetInspectableInfo(IntPtr pThis) => UnmanagedObject.FindObject<ComCallableWrapper>(pThis).InspectableInfo;

        public static object CreateRcwForComObject(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            IObjectReference identity = ObjectReference<IUnknownVftbl>.Attach(ref ptr).As<IUnknownVftbl>();

            object keepAliveSentinel = null;

            Func<IntPtr, IObjectReference, WeakReference<object>> rcwFactory = (_, objRef) =>
            {
                var inspectable = new IInspectable(objRef);
                string runtimeClassName = inspectable.GetRuntimeClassName();
                var runtimeWrapper = TypedObjectFactoryCache.GetOrAdd(runtimeClassName, className => CreateTypedRcwFactory(className))(inspectable);
                keepAliveSentinel = runtimeWrapper; // We don't take a strong reference on runtimeWrapper at any point, so we need to make sure it lives until it can get assigned to rcw.
                var cleanupSentinel = new RuntimeWrapperCleanup(identity.ThisPtr, new WeakReference(runtimeWrapper));
                return new WeakReference<object>(runtimeWrapper);
            };

            RuntimeWrapperCache.AddOrUpdate(
                identity.ThisPtr,
                rcwFactory,
                (ptr, oldValue, objRef) =>
                {
                    if (!oldValue.TryGetTarget(out keepAliveSentinel))
                    {
                        return rcwFactory(ptr, objRef);
                    }
                    return oldValue;
                },
                identity).TryGetTarget(out object rcw);

            GC.KeepAlive(keepAliveSentinel);
            return rcw;
        }

        public static IObjectReference CreateCCWForObject(object obj)
        {
            if (obj is global::System.Delegate del)
            {
                // TODO: Handle delegate passed as IInspectable
            }
            var wrapper = ComWrapperCache.GetValue(obj, _ => new ComCallableWrapper(obj));
            var objRef = ObjectReference<IUnknownVftbl>.FromAbi(wrapper.IdentityPtr);
            GC.KeepAlive(wrapper); // This GC.KeepAlive ensures that a newly created wrapper is alive until objRef is created and has AddRef'd the CCW.
            return objRef;
        }

        public static IObjectReference CreateCCWForDelegate(IntPtr invoke, global::System.Delegate del)
        {
            var wrapper = DelegateWrapperCache.GetValue(del, _ => new Delegate(invoke, del));
            var objRef = ObjectReference<IDelegateVftbl>.FromAbi(wrapper.ThisPtr);
            GC.KeepAlive(wrapper); // This GC.KeepAlive ensures that a newly created wrapper is alive until objRef is created and has AddRef'd the CCW.
            return objRef;
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

        private class RuntimeWrapperCleanup
        {
            public IntPtr _identityComObject;
            public WeakReference _runtimeWrapper;

            public RuntimeWrapperCleanup(IntPtr identityComObject, object runtimeWrapper)
            {
                _identityComObject = identityComObject;
                _runtimeWrapper = new WeakReference(runtimeWrapper);
            }
            ~RuntimeWrapperCleanup()
            {
                // If runtimeWrapper is still alive, then we need to go back into the finalization queue
                // so we can check again later.
                if (_runtimeWrapper.IsAlive)
                {
                    GC.ReRegisterForFinalize(this);
                }
                else
                {
                    RuntimeWrapperCache.TryRemove(_identityComObject, out var _);
                }
            }
        }
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

    internal class RefCountingWrapperBase
    {
        private volatile IntPtr _strongHandle;
        protected GCHandle WeakHandle { get; }
        private int _refs = 0;

        public RefCountingWrapperBase()
        {
            _strongHandle = IntPtr.Zero;
            WeakHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
        }

        ~RefCountingWrapperBase()
        {
            WeakHandle.Free();
        }

        internal uint AddRef()
        {
            uint refs = (uint)System.Threading.Interlocked.Increment(ref _refs);
            // We now own a reference. Let's try to create a strong handle if we don't already have one.
            if (_strongHandle == IntPtr.Zero)
            {
                GCHandle strongHandle = GCHandle.Alloc(this);
                IntPtr previousStrongHandle = Interlocked.CompareExchange(ref _strongHandle, GCHandle.ToIntPtr(strongHandle), IntPtr.Zero);
                if (previousStrongHandle != IntPtr.Zero)
                {
                    // We lost the race and someone else set the strong handle.
                    // Release our strong handle.
                    strongHandle.Free();
                }
            }
            return refs;
        }

        internal uint Release()
        {
            if (_refs == 0)
            {
                throw new InvalidOperationException("WinRT wrapper has been over-released!");
            }

            var refs = (uint)System.Threading.Interlocked.Decrement(ref _refs);
            if (refs == 0)
            {
                IntPtr currentStrongHandle = _strongHandle;
                // No more references. We need to remove the strong reference to make sure we don't stay alive forever.
                // Only remove the strong handle if someone else doesn't change the strong handle
                // If the strong handle changes, then someone else released and re-acquired the strong handle, meaning someone is holding a reference
                IntPtr oldStrongHandle = Interlocked.CompareExchange(ref _strongHandle, IntPtr.Zero, currentStrongHandle);
                // If _refs != 0, then someone AddRef'd this back from zero
                // so we can't release this handle.
                if (oldStrongHandle == currentStrongHandle)
                {
                    if (_refs == 0)
                    {
                        GCHandle.FromIntPtr(currentStrongHandle).Free();
                    }
                    else
                    {
                        // We took away the strong handle but someone AddRef'd. We need to put the handle back if it's still IntPtr.Zero
                        oldStrongHandle = Interlocked.CompareExchange(ref _strongHandle, currentStrongHandle, IntPtr.Zero);
                        if (oldStrongHandle != IntPtr.Zero)
                        {
                            // Someone allocated another strong handle in the meantime, we can release ours.
                            GCHandle.FromIntPtr(currentStrongHandle).Free();
                        }
                    }
                }
            }
            return refs;
        }
    }

    internal class ComCallableWrapper : RefCountingWrapperBase
    {
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

            InitializeManagedQITable(interfaceTableEntries);

            IdentityPtr = _managedQITable[typeof(IUnknownVftbl).GUID];
        }

        ~ComCallableWrapper()
        {
            foreach (var obj in _managedQITable.Values)
            {
                Marshal.FreeCoTaskMem(obj);
            }
            _managedQITable.Clear();
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
                    ifaceTearOff->_gchandlePtr = GCHandle.ToIntPtr(WeakHandle);
                    _managedQITable.Add(entry.IID, (IntPtr)ifaceTearOff);
                }
            }
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
    }

    partial class Delegate : RefCountingWrapperBase
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

        static IDelegateVftbl _vftblTemplate;
        static Delegate()
        {
            // lay out the vftable
            _vftblTemplate.QueryInterface = Marshal.GetFunctionPointerForDelegate(_QueryInterface);
            _vftblTemplate.AddRef = Marshal.GetFunctionPointerForDelegate(_AddRef);
            _vftblTemplate.Release = Marshal.GetFunctionPointerForDelegate(_Release);
            _vftblTemplate.Invoke = IntPtr.Zero;
        }

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
            _unmanagedObj._gchandlePtr = GCHandle.ToIntPtr(WeakHandle);

            ThisPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(_unmanagedObj));
            Marshal.StructureToPtr(_unmanagedObj, ThisPtr, false);
        }

        ~Delegate()
        {
            Marshal.FreeCoTaskMem(ThisPtr);
        }
    }
#endif
}

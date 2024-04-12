// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using WinRT.Interop;
using static System.Runtime.InteropServices.ComWrappers;

namespace WinRT
{
#if EMBED
    internal
#else
    public
#endif
    static partial class ComWrappersSupport
    {
        // Instance field and property for Singleton pattern: ComWrappers `set` method should be idempotent 
        private static DefaultComWrappers _instance;
        private static DefaultComWrappers DefaultComWrappersInstance
        {
            get 
            {
                if (_instance == null)
                {
                    _instance = new DefaultComWrappers();
                }
                return _instance;
            }
        }

        internal static readonly ConditionalWeakTable<Type, InspectableInfo> InspectableInfoTable = new();
        
        [ThreadStatic]
        internal static Type CreateRCWType;

        private static ComWrappers _comWrappers;
        private static object _comWrappersLock = new object();
        private static ComWrappers ComWrappers
        {
            get
            {
                if (_comWrappers is null)
                {
                    lock (_comWrappersLock)
                    {
                        if (_comWrappers is null)
                        {
                            var comWrappersToSet = DefaultComWrappersInstance;
#if !EMBED 
                            ComWrappers.RegisterForTrackerSupport(comWrappersToSet);
#endif
                            _comWrappers = comWrappersToSet;
                        }
                    }
                }
                return _comWrappers;
            }
            set
            {
                lock (_comWrappersLock)
                {
                    if (value == null && _comWrappers == DefaultComWrappersInstance)
                    {
                        return;
                    }
                    var comWrappersToSet = value ?? DefaultComWrappersInstance; 
#if !EMBED 
                    ComWrappers.RegisterForTrackerSupport(comWrappersToSet);
#endif
                    _comWrappers = comWrappersToSet; 
                }
            }
        }

        internal static unsafe InspectableInfo GetInspectableInfo(IntPtr pThis)
        {
            var _this = FindObject<object>(pThis);
            return InspectableInfoTable.GetValue(_this.GetType(), o => PregenerateNativeTypeInformation(o).inspectableInfo);
        }

        public static T CreateRcwForComObject<T>(IntPtr ptr)
        {
            return CreateRcwForComObject<T>(ptr, true);
        }

        private static T CreateRcwForComObject<T>(IntPtr ptr, bool tryUseCache)
        {
            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            // CreateRCWType is a thread local which is set here to communicate the statically known type
            // when we are called by the ComWrappers API to create the object.  We can't pass this through the
            // ComWrappers API surface, so we are achieving it via a thread local.  We unset it after in case
            // there is other calls to it via other means.
            CreateRCWType = typeof(T);
            
            var flags = tryUseCache ? CreateObjectFlags.TrackerObject : CreateObjectFlags.TrackerObject | CreateObjectFlags.UniqueInstance;
            var rcw = ComWrappers.GetOrCreateObjectForComInstance(ptr, flags);
            CreateRCWType = null;
            // Because .NET will de-duplicate strings and WinRT doesn't,
            // our RCW factory returns a wrapper of our string instance.
            // This ensures that ComWrappers never sees the same managed object for two different
            // native pointers. We unwrap here to ensure that the user-experience is expected
            // and consumers get a string object for a Windows.Foundation.IReference<String>.
            // We need to do the same thing for System.Type because there can be multiple WUX.Interop.TypeName's
            // for a single System.Type.

            return rcw switch
            {
                ABI.System.Nullable nt => (T)nt.Value,
                T castRcw => castRcw,
                _ when tryUseCache => CreateRcwForComObject<T>(ptr, false),
                _ => throw new ArgumentException(string.Format("Unable to create a wrapper object. The WinRT object {0} has type {1} which cannot be assigned to type {2}", ptr, rcw.GetType(), typeof(T)))
            };

        }

        public static bool TryUnwrapObject(object o, out IObjectReference objRef)
        {
            // The unwrapping here needs to be an exact type match in case the user
            // has implemented a WinRT interface or inherited from a WinRT class
            // in a .NET (non-projected) type.

            if (o is Delegate del)
            {
                return TryUnwrapObject(del.Target, out objRef);
            }

            if (o is IWinRTObject winrtObj && winrtObj.HasUnwrappableNativeObject)
            {
                objRef = winrtObj.NativeObject;
                return true;
            }

            objRef = null;
            return false;
        }

        public static void RegisterObjectForInterface(object obj, IntPtr thisPtr, CreateObjectFlags createObjectFlags) =>
            ComWrappers.GetOrRegisterObjectForComInstance(thisPtr, createObjectFlags, obj);

        public static void RegisterObjectForInterface(object obj, IntPtr thisPtr) => 
            TryRegisterObjectForInterface(obj, thisPtr);

        public static object TryRegisterObjectForInterface(object obj, IntPtr thisPtr)
        {
            return ComWrappers.GetOrRegisterObjectForComInstance(thisPtr, CreateObjectFlags.TrackerObject, obj);
        }

        public static IObjectReference CreateCCWForObject(object obj)
        {
            IntPtr ccw = ComWrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.TrackerSupport);
            return ObjectReference<IUnknownVftbl>.Attach(ref ccw, IID.IID_IUnknown);
        }

        internal static IntPtr CreateCCWForObjectForABI(object obj, Guid iid)
        {
            IntPtr ccw = ComWrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.TrackerSupport);
            try
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(ccw, ref iid, out var iidCcw));
                return iidCcw;
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(ccw);
            }
        }

        public static unsafe T FindObject<T>(IntPtr ptr)
            where T : class => ComInterfaceDispatch.GetInstance<T>((ComInterfaceDispatch*)ptr);

        public static IUnknownVftbl IUnknownVftbl => DefaultComWrappers.IUnknownVftbl;
        public static IntPtr IUnknownVftblPtr => DefaultComWrappers.IUnknownVftblPtr;

        public static IntPtr AllocateVtableMemory(Type vtableType, int size) => RuntimeHelpers.AllocateTypeAssociatedMemory(vtableType, size);

        /// <summary>
        /// Initialize the global <see cref="System.Runtime.InteropServices.ComWrappers"/> instance to use for WinRT.
        /// </summary>
        /// <param name="wrappers">The wrappers instance to use, or the default if null.</param>
        /// <remarks>
        /// A custom ComWrappers instance can be supplied to enable programs to fast-track some type resolution
        /// instead of using reflection when the full type closure is known.
        /// </remarks>
        public static void InitializeComWrappers(ComWrappers wrappers = null)
        {
            ComWrappers = wrappers;
        }

        internal static Func<IInspectable, object> GetTypedRcwFactory(Type implementationType) => TypedObjectFactoryCacheForType.GetOrAdd(implementationType, classType => CreateTypedRcwFactory(classType));

        public static bool RegisterTypedRcwFactory(Type implementationType, Func<IInspectable, object> rcwFactory) => TypedObjectFactoryCacheForType.TryAdd(implementationType, rcwFactory);

        private static Func<IInspectable, object> CreateFactoryForImplementationType(string runtimeClassName, Type implementationType)
        {
            if (implementationType.IsGenericType)
            {
                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    throw new NotSupportedException($"Cannot create an RCW factory for implementation type '{implementationType}'.");
                }

#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                Type genericImplType = GetGenericImplType(implementationType);
#pragma warning restore IL3050
                if (genericImplType != null)
                {
                    var createRcw = genericImplType.GetMethod("CreateRcw", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IInspectable) }, null);
                    return (Func<IInspectable, object>)createRcw.CreateDelegate(typeof(Func<IInspectable, object>));
                }
            }

            if (implementationType.IsInterface)
            {
                return obj => obj;
            }

            // We never look for attributes on base types, since each [WinRTImplementationTypeRcwFactory] type acts as
            // a factory type specifically for the annotated implementation type, so it has to be on that derived type.
            var attribute = implementationType.GetCustomAttribute<WinRTImplementationTypeRcwFactoryAttribute>(inherit: false);

            // For update projections, get the derived [WinRTImplementationTypeRcwFactory]
            // attribute instance and use its overridden 'CreateInstance' method as factory.
            if (attribute is not null)
            {
                return attribute.CreateInstance;
            }

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException(
                    $"Cannot create an RCW factory for implementation type '{implementationType}', because it doesn't have " +
                    "a [WinRTImplementationTypeRcwFactory] derived attribute on it. The fallback path for older projections " +
                    "is not trim-safe, and isn't supported in AOT environments. Make sure to reference updated projections.");
            }

            return CreateRcwFallback(implementationType);

            [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "This fallback path is not trim-safe by design (to avoid annotations).")]
            static Func<IInspectable, object> CreateRcwFallback(Type implementationType)
            {
                var constructor = implementationType.GetConstructor(
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                    binder: null,
                    types: new[] { typeof(IObjectReference) },
                    modifiers: null);

                return (IInspectable obj) => constructor.Invoke(new[] { obj.ObjRef });
            }

#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            static Type GetGenericImplType(Type implementationType)
            {
                var genericType = implementationType.GetGenericTypeDefinition();

                Type genericImplType = null;
                if (genericType == typeof(IList<>))
                {
                    genericImplType = typeof(IListImpl<>);
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    genericImplType = typeof(IDictionaryImpl<,>);
                }
                else if (genericType == typeof(IReadOnlyDictionary<,>))
                {
                    genericImplType = typeof(IReadOnlyDictionaryImpl<,>);
                }
                else if (genericType == typeof(IReadOnlyList<>))
                {
                    genericImplType = typeof(IReadOnlyListImpl<>);
                }
                else if (genericType == typeof(IEnumerable<>))
                {
                    genericImplType = typeof(IEnumerableImpl<>);
                }
                else if (genericType == typeof(IEnumerator<>))
                {
                    genericImplType = typeof(IEnumeratorImpl<>);
                }
                else
                {
                    return null;
                }

                return genericImplType.MakeGenericType(implementationType.GetGenericArguments());
            }
        }

        private static readonly ReaderWriterLockSlim ComInterfaceEntriesLookupRwLock = new();
        private static readonly List<Func<Type, ComInterfaceEntry[]>> ComInterfaceEntriesLookup = new();

        public static void RegisterTypeComInterfaceEntriesLookup(Func<Type, ComInterfaceEntry[]> comInterfaceEntriesLookup)
        {
            ComInterfaceEntriesLookupRwLock.EnterWriteLock();
            try
            {
                ComInterfaceEntriesLookup.Add(comInterfaceEntriesLookup);
            }
            finally
            {
                ComInterfaceEntriesLookupRwLock.ExitWriteLock();
            }
        }

        internal static ComInterfaceEntry[] GetComInterfaceEntriesForTypeFromLookupTable(Type type)
        {
            ComInterfaceEntriesLookupRwLock.EnterReadLock();

            try
            {
                foreach (var func in ComInterfaceEntriesLookup)
                {
                    var comInterfaceEntries = func(type);
                    if (comInterfaceEntries != null)
                    {
                        return comInterfaceEntries;
                    }
                }
            }
            finally
            {
                ComInterfaceEntriesLookupRwLock.ExitReadLock();
            }

            return null;
        }

        private readonly static ConcurrentDictionary<Type, ComInterfaceEntry[]> ComInterfaceEntriesForType = new();
        public static void RegisterComInterfaceEntries(Type implementationType, ComInterfaceEntry[] comInterfaceEntries) => ComInterfaceEntriesForType.TryAdd(implementationType, comInterfaceEntries);
    }

#if EMBED
    internal 
#else
    public
#endif     
    class ComWrappersHelper
    {
        public unsafe static void Init(
            bool isAggregation,
            object thisInstance,
            IntPtr newInstance,
            IntPtr inner,
            out IObjectReference objRef)
        {
            objRef = ComWrappersSupport.GetObjectReferenceForInterface(isAggregation ? inner : newInstance);

            IntPtr referenceTracker;
            {
                // Determine if the instance supports IReferenceTracker (e.g. WinUI).
                // Acquiring this interface is useful for:
                //   1) Providing an indication of what value to pass during RCW creation.
                //   2) Informing the Reference Tracker runtime during non-aggregation
                //      scenarios about new references.
                //
                // If aggregation, query the inner since that will have the implementation
                // otherwise the new instance will be used. Since the inner was composed
                // it should answer immediately without going through the outer. Either way
                // the reference count will go to the new instance.
                int hr = Marshal.QueryInterface(objRef.ThisPtr, ref Unsafe.AsRef(in IID.IID_IReferenceTracker), out referenceTracker);
                if (hr != 0)
                {
                    referenceTracker = default;
                }
            }

            {
                // Determine flags needed for native object wrapper (i.e. RCW) creation.
                var createObjectFlags = CreateObjectFlags.None;
                IntPtr instanceToWrap = newInstance;

                // The instance supports IReferenceTracker.
                if (referenceTracker != default(IntPtr))
                {
                    createObjectFlags |= CreateObjectFlags.TrackerObject;
                }

                // Update flags if the native instance is being used in an aggregation scenario.
                if (isAggregation)
                {
                    // Indicate the scenario is aggregation
                    createObjectFlags |= (CreateObjectFlags)4;

                    // The instance supports IReferenceTracker.
                    if (referenceTracker != default(IntPtr))
                    {
                        // IReferenceTracker is not needed in aggregation scenarios.
                        // It is not needed because all QueryInterface() calls on an
                        // object are followed by an immediately release of the returned
                        // pointer - see below for details.
                        Marshal.Release(referenceTracker);

                        // .NET 5 limitation
                        //
                        // For aggregated scenarios involving IReferenceTracker
                        // the API handles object cleanup. In .NET 5 the API
                        // didn't expose an option to handle this so we pass the inner
                        // in order to handle its lifetime.
                        //
                        // The API doesn't handle inner lifetime in any other scenario
                        // in the .NET 5 timeframe.
                        instanceToWrap = inner;
                    }
                }

                // Create a native object wrapper (i.e. RCW).
                //
                // Note this function will call QueryInterface() on the supplied instance,
                // therefore it is important that the enclosing CCW forwards to its inner
                // if aggregation is involved. This is typically accomplished through an
                // implementation of ICustomQueryInterface.
                ComWrappersSupport.RegisterObjectForInterface(thisInstance, instanceToWrap, createObjectFlags);
            }

            // The following sets up the object reference to correctly handle AddRefs and releases
            // based on the scenario.
            if (isAggregation)
            {
                // Aggregation scenarios should avoid calling AddRef() on the
                // newInstance value. This is due to the semantics of COM Aggregation
                // and the fact that calling an AddRef() on the instance will increment
                // the CCW which in turn will ensure it cannot be cleaned up. Calling
                // AddRef() on the instance when passed to unmanaged code is correct
                // since unmanaged code is required to call Release() at some point.

                // A pointer to the inner that should be queried for
                // additional interfaces. Immediately after a QueryInterface()
                // a Release() should be called on the returned pointer but the
                // pointer can be retained and used.  This is determined by the
                // IsAggregated and PreventReleaseOnDispose properties on IObjectReference.
                objRef.IsAggregated = true;
                // In WinUI scenario don't release inner
                objRef.PreventReleaseOnDispose = referenceTracker != default(IntPtr);
            }
            else
            {
                if (referenceTracker != default(IntPtr))
                {
                    // WinUI scenario
                    // This instance should be used to tell the
                    // Reference Tracker runtime whenever an AddRef()/Release()
                    // is performed on newInstance.
                    objRef.ReferenceTrackerPtr = referenceTracker;

                    // This instance is already AddRefFromTrackerSource by the CLR,
                    // so it would also ReleaseFromTrackerSource on destruction.
                    objRef.PreventReleaseFromTrackerSourceOnDispose = true;

                    Marshal.Release(referenceTracker);
                }

                Marshal.Release(newInstance);
            }
        }

        public unsafe static void Init(IObjectReference objRef, bool addRefFromTrackerSource = true)
        {
            if (objRef.ReferenceTrackerPtr == IntPtr.Zero)
            {
                int hr = Marshal.QueryInterface(objRef.ThisPtr, ref Unsafe.AsRef(in IID.IID_IReferenceTracker), out var referenceTracker);
                if (hr == 0)
                {
                    // WinUI scenario
                    // This instance should be used to tell the
                    // Reference Tracker runtime whenever an AddRef()/Release()
                    // is performed on newInstance.
                    objRef.ReferenceTrackerPtr = referenceTracker;

                    if (addRefFromTrackerSource)
                    {
                        objRef.AddRefFromTrackerSource(); // ObjRef instance
                    }
                    else
                    {
                        objRef.PreventReleaseFromTrackerSourceOnDispose = true;
                    }

                    Marshal.Release(referenceTracker);
                }
            }
        }
    }

#if EMBED
    internal 
#else
    public 
#endif     
    class DefaultComWrappers : ComWrappers
    {
        private static readonly ConditionalWeakTable<Type, VtableEntries> TypeVtableEntryTable = new();
        public static unsafe IUnknownVftbl IUnknownVftbl => Unsafe.AsRef<IUnknownVftbl>(IUnknownVftblPtr.ToPointer());

        internal static IntPtr IUnknownVftblPtr { get; }

        static unsafe DefaultComWrappers()
        {
            GetIUnknownImpl(out var qi, out var addRef, out var release);

            IUnknownVftblPtr = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IUnknownVftbl), sizeof(IUnknownVftbl));
            (*(IUnknownVftbl*)IUnknownVftblPtr) = new IUnknownVftbl
            {
                QueryInterface = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)qi,
                AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)addRef,
                Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)release,
            };
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            var vtableEntries = TypeVtableEntryTable.GetValue(obj.GetType(), static (type) => 
            {
                if (IsRuntimeImplementedRCW(type))
                {
                    // If the object is a runtime-implemented RCW, let the runtime create a CCW.
                    return new VtableEntries();
                }

                return new VtableEntries(ComWrappersSupport.GetInterfaceTableEntries(type), type);
            });

            count = vtableEntries.Count;
            if (count != 0 && !flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown))
            {
                // The vtable list unconditionally has the last entry as IUnknown, but it should
                // only be included if the flag is set.  We achieve that by excluding the last entry
                // from the count if the flag isn't set.
                count -= 1;
            }

            return vtableEntries.Data;
        }

        private static unsafe bool IsRuntimeImplementedRCW(Type objType)
        {
            bool isRcw = objType.IsCOMObject;
            if (objType.IsGenericType)
            {
                foreach (var arg in objType.GetGenericArguments())
                {
                    if (arg.IsCOMObject)
                    {
                        isRcw = true;
                        break;
                    }
                }
            }
            return isRcw;
        }

        private static object CreateObject(IntPtr externalComObject)
        {
            Guid inspectableIID = IID.IID_IInspectable;
            Guid weakReferenceIID = ABI.WinRT.Interop.IWeakReference.IID;
            IntPtr ptr = IntPtr.Zero;

            try
            {
                if (ComWrappersSupport.CreateRCWType != null && ComWrappersSupport.CreateRCWType.IsDelegate())
                {
                    return ComWrappersSupport.GetOrCreateDelegateFactory(ComWrappersSupport.CreateRCWType)(externalComObject);
                }
                else if (Marshal.QueryInterface(externalComObject, ref inspectableIID, out ptr) == 0)
                {
                    var inspectableObjRef = ComWrappersSupport.GetObjectReferenceForInterface<IInspectable.Vftbl>(ptr);
                    ComWrappersHelper.Init(inspectableObjRef);

                    IInspectable inspectable = new IInspectable(inspectableObjRef);

                    if (ComWrappersSupport.CreateRCWType != null
                        && ComWrappersSupport.CreateRCWType.IsSealed)
                    {
                        return ComWrappersSupport.GetTypedRcwFactory(ComWrappersSupport.CreateRCWType)(inspectable);
                    }

                    Type runtimeClassType = ComWrappersSupport.GetRuntimeClassForTypeCreation(inspectable, ComWrappersSupport.CreateRCWType);
                    if (runtimeClassType == null)
                    {
                        // If the external IInspectable has not implemented GetRuntimeClassName,
                        // we use the Inspectable wrapper directly.
                        return inspectable;
                    }

                    return ComWrappersSupport.GetTypedRcwFactory(runtimeClassType)(inspectable);
                }
                else if (Marshal.QueryInterface(externalComObject, ref weakReferenceIID, out ptr) == 0)
                {
                    // IWeakReference is IUnknown-based, so implementations of it may not (and likely won't) implement
                    // IInspectable. As a result, we need to check for them explicitly.
                    var iunknownObjRef = ComWrappersSupport.GetObjectReferenceForInterface<IUnknownVftbl>(ptr, weakReferenceIID, false);
                    ComWrappersHelper.Init(iunknownObjRef);

                    return new SingleInterfaceOptimizedObject(typeof(IWeakReference), iunknownObjRef, false);
                }
                else
                {
                    // If the external COM object isn't IInspectable or IWeakReference, we can't handle it.
                    // If we're registered globally, we want to let the runtime fall back for IUnknown and IDispatch support.
                    // Return null so the runtime can fall back gracefully in IUnknown and IDispatch scenarios.
                    return null;
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.Release(ptr);
                }
            }
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            var obj = CreateObject(externalComObject);
            if (obj is IWinRTObject winrtObj && winrtObj.HasUnwrappableNativeObject && winrtObj.NativeObject != null)
            {
                // Handle the scenario where the CLR has already done an AddRefFromTrackerSource on the instance
                // stored by the RCW type.  We handle it by releasing the AddRef we did and not doing an release
                // on destruction as the CLR would do it.
                winrtObj.NativeObject.ReleaseFromTrackerSource();
                winrtObj.NativeObject.PreventReleaseFromTrackerSourceOnDispose = true;
            }

            return obj;
        }

        protected override void ReleaseObjects(IEnumerable objects)
        {
            foreach (var obj in objects)
            {
                if (ComWrappersSupport.TryUnwrapObject(obj, out var objRef))
                {
                    objRef.Dispose();
                }
                else
                {
                    throw new InvalidOperationException("Cannot release objects that are not runtime wrappers of native WinRT objects.");
                }
            }
        }

        unsafe sealed class VtableEntries
        {
            public ComInterfaceEntry* Data { get; }
            public int Count { get; }

            public VtableEntries()
            {
                Data = null;
                Count = 0;
            }

            public VtableEntries(List<ComInterfaceEntry> entries, Type type)
            {
                Data = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(type, sizeof(ComInterfaceEntry) * entries.Count);
                for (int i = 0; i < entries.Count; i++)
                {
                    Data[i] = entries[i];
                }
                Count = entries.Count;
            }
        }
    }
}

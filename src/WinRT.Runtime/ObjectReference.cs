// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
#if EMBED
    internal
#else
    public
#endif
    abstract class IObjectReference : IDisposable
    {
        // Flags for the '_disposedFlags' field, see notes in the Dispose() method below
        private const int NOT_DISPOSED = 0;
        private const int DISPOSE_PENDING = 1;
        private const int DISPOSE_COMPLETED = 2;

        private readonly IntPtr _thisPtr;
        private IntPtr _referenceTrackerPtr;
        private int _disposedFlags;

        public IntPtr ThisPtr
        {
            get
            {
                ThrowIfDisposed();
                return GetThisPtrForCurrentContext();
            }
        }

        public bool IsFreeThreaded => GetContextToken() == IntPtr.Zero;

        public bool IsInCurrentContext
        {
            get
            {
                var contextToken = GetContextToken();
                return contextToken == IntPtr.Zero || contextToken == Context.GetContextToken();
            }
        }

        private protected IntPtr ThisPtrFromOriginalContext
        {
            get
            {
                ThrowIfDisposed();
                return _thisPtr;
            }
        }

#if DEBUG
        private unsafe uint RefCount
        {
            get
            {
                Marshal.AddRef(ThisPtr);
                return (uint)Marshal.Release(ThisPtr);
            }
        }

        private bool BreakOnDispose { get; set; }
#endif

        internal bool IsAggregated { get; set; }

        internal bool PreventReleaseOnDispose { get; set; }

        internal bool PreventReleaseFromTrackerSourceOnDispose { get; set; }

        internal unsafe IntPtr ReferenceTrackerPtr
        {
            get
            {
                return _referenceTrackerPtr;
            }

            set
            {
                _referenceTrackerPtr = value;
                if (_referenceTrackerPtr != IntPtr.Zero)
                {
                    Marshal.AddRef(_referenceTrackerPtr);
                    AddRefFromTrackerSource();
                }
            }
        }

        internal unsafe IReferenceTrackerVftbl ReferenceTracker
        {
            get
            {
                ThrowIfDisposed();
                return **(IReferenceTrackerVftbl**)ReferenceTrackerPtr;
            }
        }

        protected unsafe IUnknownVftbl VftblIUnknown
        {
            get
            {
                ThrowIfDisposed();
                return **(IUnknownVftbl**)ThisPtr;
            }
        }

        private protected unsafe IUnknownVftbl VftblIUnknownFromOriginalContext
        {
            get
            {
                ThrowIfDisposed();
                return **(IUnknownVftbl**)ThisPtrFromOriginalContext;
            }
        }

        protected IObjectReference(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(thisPtr));
            }
            _thisPtr = thisPtr;

            // We are holding onto a native object or one of its interfaces.
            // This causes for there to be native memory being held onto by
            // this that the .NET GC isn't aware of.  So we use memory pressure
            // to make the .NET GC aware of it.  In theory all the interface QIs
            // can be holding onto the same native object.  Here we are taking the simplified
            // approach of having each IObjectReference represent some basic native memory
            // pressure rather than tracking all the IObjectReferences that are connected
            // to the same object and only releasing the memory pressure once all of them
            // have been finalized.
            GC.AddMemoryPressure(ComWrappersSupport.GC_PRESSURE_BASE);
        }

        ~IObjectReference()
        {
            Dispose();
        }

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public ObjectReference<T> As<T>() => As<T>(GuidGenerator.GetIID(typeof(T)));

        public unsafe ObjectReference<T> As<T>(Guid iid)
        {
            Marshal.ThrowExceptionForHR(TryAs<T>(iid, out var objRef));
            return objRef;
        }

        public unsafe TInterface AsInterface<TInterface>()
        {
            if (typeof(TInterface).IsDefined(typeof(ComImportAttribute)))
            {
                Guid iid = typeof(TInterface).GUID;
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(ThisPtr, ref iid, out IntPtr comPtr));
                try
                {
                    return (TInterface)Marshal.GetObjectForIUnknown(comPtr);
                }
                finally
                {
                    _ = Marshal.Release(comPtr);
                }
            }

            if (!FeatureSwitches.EnableIDynamicInterfaceCastableSupport)
            {
                throw new NotSupportedException(
                    "Using 'AsInterface<TInterface>' to cast an RCW to an interface type not present in " +
                    "metadata relies on 'IDynamicInterfaceCastable' support, which is not currently available. " +
                    "Make sure the 'EnableIDynamicInterfaceCastableSupport' property is not set to 'false'.");
            }

#if !NET
            return (TInterface)typeof(TInterface).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { this });
#else
            return (TInterface)(object)new WinRT.IInspectable(this);
#endif
        }

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public int TryAs<T>(out ObjectReference<T> objRef) => TryAs(GuidGenerator.GetIID(typeof(T)), out objRef);

        public unsafe int TryAs<T>(Guid iid, out ObjectReference<T> objRef)
        {
            // Check the marker interface for ObjectReferenceWithContext<T>. If that is the case, we inline
            // the special logic for such objects. This avoids having to use a generic virtual method here,
            // which would just explode the binary size on NativeAOT due to combinatorial generics.
            if (this is IObjectReferenceWithContext)
            {
                return ObjectReferenceWithContext<T>.TryAs(this, iid, out objRef);
            }

            // Normal path for IObjectReference or any IObjectReference<T>
            return ObjectReference<T>.TryAs(this, iid, out objRef);
        }

        public virtual unsafe ObjectReference<IUnknownVftbl> AsKnownPtr(IntPtr ptr)
        {
            AddRef(true);
            var objRef = ObjectReference<IUnknownVftbl>.Attach(ref ptr, IID.IID_IUnknown);
            objRef.IsAggregated = IsAggregated;
            objRef.PreventReleaseOnDispose = IsAggregated;
            objRef.ReferenceTrackerPtr = ReferenceTrackerPtr;
            return objRef;
        }

        // Used only as part of the GetInterface implementation where the
        // result is an reference passed across the ABI and doesn't need to
        // be tracked as an internal reference.  This is separate to handle
        // tear off aggregate scenario where releasing an reference can end up
        // deleting the tear off interface.
        public virtual unsafe int TryAs(Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            ThrowIfDisposed();
            IntPtr thatPtr = IntPtr.Zero;
            int hr = Marshal.QueryInterface(ThisPtr, ref iid, out thatPtr);
            if (hr >= 0)
            {
                ppv = thatPtr;
            }
            return hr;
        }

        public unsafe IObjectReference As(Guid iid) => As<IUnknownVftbl>(iid);

#if NET
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public T AsType<T>()
        {
            ThrowIfDisposed();

#if NET
            // Same logic as in 'ComWrappersSupport.CreateFactoryForImplementationType', see notes there
            var attribute = typeof(T).GetCustomAttribute<WinRTImplementationTypeRcwFactoryAttribute>(inherit: false);

            if (attribute is not null)
            {
                return (T)attribute.CreateInstance(new IInspectable(this));
            }

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException(
                    $"Cannot create an RCW instance for type '{typeof(T)}', because it doesn't have a " +
                    "[WinRTImplementationTypeRcwFactory] derived attribute on it. The fallback path for older projections " +
                    "is not trim-safe, and isn't supported in AOT environments. Make sure to reference updated projections.");
            }

            if (TryCreateRcwFallback(this, out object rcwInstance))
            {
                return (T)rcwInstance;
            }

            [UnconditionalSuppressMessage("Trimming", "IL2090", Justification = "This fallback path is not trim-safe by design (to avoid annotations).")]
            static bool TryCreateRcwFallback(IObjectReference objectReference, out object rcwInstance)
            {
                var constructor = typeof(T).GetConstructor(
                    bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                    binder: null,
                    types: new[] { typeof(IObjectReference) },
                    modifiers: null);

                if (constructor is not null)
                {
                    rcwInstance = constructor.Invoke(new[] { objectReference });

                    return true;
                }

                rcwInstance = null;

                return false;
            }
#else
            var ctor = typeof(T).GetConstructor(new[] { typeof(IObjectReference) });
            if (ctor is not null)
            {
                return (T)ctor.Invoke(new[] { this });
            }
#endif
            throw new InvalidOperationException($"Target type '{typeof(T)}' is not a projected type.");
        }

        public IntPtr GetRef()
        {
            ThrowIfDisposed();
            AddRef(false);
            return ThisPtr;
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if <see cref="Dispose"/> has already been called on the current instance.
        /// </summary>
        /// <remarks>
        /// Note that calling this method does not protect callers against concurrent threads calling <see cref="Dispose"/> on the
        /// same instance, as that behavior is explicitly undefined. Similarly, callers using this to then access the underlying
        /// pointers should also make sure to keep the current instance alive until they're done using the pointer (unless they're
        /// also incrementing it via <c>AddRef</c> in some way), or the GC could concurrently collect the instance and cause the
        /// same problem (ie. the underlying pointer being in use becoming invalid right after retrieving it from the object).
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the current instance is disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposedFlags) == DISPOSE_COMPLETED)
            {
                ThrowObjectDisposedException();
            }

            static void ThrowObjectDisposedException()
            {
                throw new ObjectDisposedException("ObjectReference");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // We swap the disposed flag and only dispose the first time. This is safe with respect to
            // different threads concurrently trying to dispose the same object, as only the first one
            // will actually dispose it, and the others will just do nothing.
            //
            // It is not safe when combined with ThrowIfDisposed(), as the following scenario is possible:
            //   - Thread A calls ProjectedType.Foo()
            //   - Thread A calls ThisPtr, the dispose check passes and it gets the IntPtr value (not incremented)
            //   - Thread B calls Dispose(), which releases the object
            //   - Thread A now uses that IntPtr to invoke some function pointer
            //   - Thread A goes ka-boom 💥
            //
            // However, this is by design, as the ObjectReference owns the 'ThisPtr' property, and disposing it while it
            // is still in use can lead to all kinds of things going wrong. This is conceptually the same as calling
            // SafeHandle.DangerousGetHandle(). Furthermore, the same exact behavior was already possible with an actual
            // lock object guarding all the logic within the Dispose() method. The only difference is that simply using
            // a flag this way avoids one object allocation per ObjectReference instance, and also allows making the size
            // of the whole object smaller by sizeof(object), when taking into account padding.
            //
            // Additionally, note that the '_disposedFlags' field has 3 different states:
            //   - NOT_DISPOSED: the initial state, when the object is alive
            //   - DISPOSE_PENDING: indicates that a thread is currently executing the Dispose() method and got past the
            //     first check, and is in the process of releasing the native resources. This state is checked by the
            //     ThrowIfDisposed() method above, and still treated as if the object can be used normally. This is
            //     necessary, because the dispose logic still has to access the 'ThisPtr' property and others in order
            //     to perform the various Release() calls on the native objects being used. If the state was immediately
            //     set to disposed, that method would just start throwing immediately, and this logic would not work.
            //   - DISPOSE_COMPLETED: set when all the Dispose() logic has been completed and the object should not be
            //     used at all anymore. When this is set, the ThrowIfDisposed() method will start throwing exceptions.
            if (Interlocked.CompareExchange(ref _disposedFlags, DISPOSE_PENDING, NOT_DISPOSED) == NOT_DISPOSED)
            {
#if DEBUG
                if (BreakOnDispose && System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif

                if (!PreventReleaseOnDispose)
                {
                    Release();
                }

                DisposeTrackerSource();
                GC.RemoveMemoryPressure(ComWrappersSupport.GC_PRESSURE_BASE);

                Volatile.Write(ref _disposedFlags, DISPOSE_COMPLETED);
            }
        }

        protected virtual unsafe void AddRef(bool refFromTrackerSource)
        {
            Marshal.AddRef(ThisPtr);
            if (refFromTrackerSource)
            {
                AddRefFromTrackerSource();
            }
        }

        protected virtual unsafe void AddRef()
        {
            AddRef(true);
        }

        protected virtual unsafe void Release()
        {
            ReleaseFromTrackerSource();
            Marshal.Release(ThisPtr);
        }

        private protected unsafe void ReleaseWithoutContext()
        {
            ReleaseFromTrackerSource();
            Marshal.Release(ThisPtrFromOriginalContext);
        }

        internal unsafe bool IsReferenceToManagedObject
        {
            get
            {
                return VftblIUnknown.Equals(IUnknownVftbl.AbiToProjectionVftbl);
            }
        }

        internal unsafe void AddRefFromTrackerSource()
        {
            if (ReferenceTrackerPtr != IntPtr.Zero)
            {
                ReferenceTracker.AddRefFromTrackerSource(ReferenceTrackerPtr);
            }
        }

        internal unsafe void ReleaseFromTrackerSource()
        {
            if (ReferenceTrackerPtr != IntPtr.Zero)
            {
                ReferenceTracker.ReleaseFromTrackerSource(ReferenceTrackerPtr);
            }
        }

        private unsafe void DisposeTrackerSource()
        {
            if (ReferenceTrackerPtr != IntPtr.Zero)
            {
                if (!PreventReleaseFromTrackerSourceOnDispose)
                {
                    ReferenceTracker.ReleaseFromTrackerSource(ReferenceTrackerPtr);
                }
                Marshal.Release(ReferenceTrackerPtr);
            }
        }

        private protected virtual IntPtr GetThisPtrForCurrentContext()
        {
            return ThisPtrFromOriginalContext;
        }

        private protected virtual IntPtr GetContextToken()
        {
            return IntPtr.Zero;
        }

        public ObjectReferenceValue AsValue()
        {
            // Sharing ptr with objref.
            return new ObjectReferenceValue(ThisPtr, IntPtr.Zero, true, this);
        }

        public unsafe ObjectReferenceValue AsValue(Guid iid)
        {
            IntPtr thatPtr = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(ThisPtr, ref iid, out thatPtr));
            if (IsAggregated)
            {
                Marshal.Release(thatPtr);
            }
            AddRefFromTrackerSource();

            return new ObjectReferenceValue(thatPtr, ReferenceTrackerPtr, IsAggregated, this);
        }
    }

#if EMBED
    internal
#else
    public
#endif
    class ObjectReference<T> : IObjectReference
    {
        private readonly T _vftbl;
        public T Vftbl
        {
            get
            {
                ThrowIfDisposed();
                return GetVftblForCurrentContext();
            }
        }

        private protected ObjectReference(IntPtr thisPtr, T vftblT) :
            base(thisPtr)
        {
            _vftbl = vftblT;
        }

        private protected ObjectReference(IntPtr thisPtr) :
            this(thisPtr, GetVtable(thisPtr))
        {
        }

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public static ObjectReference<T> Attach(ref IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }

            if (ComWrappersSupport.IsFreeThreaded(thisPtr))
            {
                var obj = new ObjectReference<T>(thisPtr);
                thisPtr = IntPtr.Zero;
                return obj;
            }
            else
            {
                var obj = new ObjectReferenceWithContext<T>(
                    thisPtr,
                    Context.GetContextCallback(),
                    Context.GetContextToken());
                thisPtr = IntPtr.Zero;
                return obj;
            }
        }

        public static ObjectReference<T> Attach(ref IntPtr thisPtr, Guid iid)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }

            if (ComWrappersSupport.IsFreeThreaded(thisPtr))
            {
                var obj = new ObjectReference<T>(thisPtr);
                thisPtr = IntPtr.Zero;
                return obj;
            }
            else
            {
                var obj = new ObjectReferenceWithContext<T>(
                    thisPtr,
                    Context.GetContextCallback(),
                    Context.GetContextToken(),
                    iid);
                thisPtr = IntPtr.Zero;
                return obj;
            }
        }

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public static unsafe ObjectReference<T> FromAbi(IntPtr thisPtr, T vftblT)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }

            Marshal.AddRef(thisPtr);
            if (ComWrappersSupport.IsFreeThreaded(thisPtr))
            {
                var obj = new ObjectReference<T>(thisPtr, vftblT);
                return obj;
            }
            else
            {
                var obj = new ObjectReferenceWithContext<T>(
                    thisPtr,
                    vftblT,
                    Context.GetContextCallback(),
                    Context.GetContextToken());
                return obj;
            }
        }

        public static unsafe ObjectReference<T> FromAbi(IntPtr thisPtr, T vftblT, Guid iid)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }

            Marshal.AddRef(thisPtr);
            if (ComWrappersSupport.IsFreeThreaded(thisPtr))
            {
                var obj = new ObjectReference<T>(thisPtr, vftblT);
                return obj;
            }
            else
            {
                var obj = new ObjectReferenceWithContext<T>(
                    thisPtr,
                    vftblT,
                    Context.GetContextCallback(),
                    Context.GetContextToken(),
                    iid);
                return obj;
            }
        }

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public static ObjectReference<T> FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = GetVtable(thisPtr);
            return FromAbi(thisPtr, vftblT);
        }

        public static ObjectReference<T> FromAbi(IntPtr thisPtr, Guid iid)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = GetVtable(thisPtr);
            return FromAbi(thisPtr, vftblT, iid);
        }

        private static unsafe T GetVtable(IntPtr thisPtr)
        {
            // With our vtable types, the generic vtables will have System.Delegate fields
            // and the non-generic types will have only void* fields. On .NET 6+, we can use
            // RuntimeHelpers.IsReferenceorContainsReferences to disambiguate between generic
            // and non-generic vtables since it's a JIT-time constant. Projections for .NET 6+
            // never use such vtables, so this path will just always throw. This also allows
            // dropping the trimming annotations preserving all non public constructors.
            // Since it is a JIT time constant, this function will be branchless on .NET 6+.
            // On .NET Standard 2.0, the IsReferenceOrContainsReferences method does not exist,
            // so we instead fall back to typeof(T).IsGenericType, which is not a JIT-time constant.
#if NET
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                throw new NotSupportedException("Managed vtable types (ie. containing any reference types) are not supported.");
            }
#else
            if (typeof(T).IsGenericType)
            {
                return (T)typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new[] { typeof(IntPtr) }, null).Invoke(new object[] { thisPtr });
            }
#endif

            // Blittable vtables can just be read directly from the input pointer
            return Unsafe.Read<T>(*(void***)thisPtr);
        }

        private protected virtual T GetVftblForCurrentContext()
        {
            return _vftbl;
        }

        internal static int TryAs(IObjectReference sourceRef, Guid iid, out ObjectReference<T> objRef)
        {
            objRef = null;

            int hr = Marshal.QueryInterface(sourceRef.ThisPtr, ref iid, out IntPtr thatPtr);

            if (hr >= 0)
            {
                if (sourceRef.IsAggregated)
                {
                    Marshal.Release(thatPtr);
                }

                sourceRef.AddRefFromTrackerSource();

                objRef = Attach(ref thatPtr, iid);
                objRef.IsAggregated = sourceRef.IsAggregated;
                objRef.PreventReleaseOnDispose = sourceRef.IsAggregated;
                objRef.ReferenceTrackerPtr = sourceRef.ReferenceTrackerPtr;
            }

            return hr;
        }
    }

    internal sealed class ObjectReferenceWithContext<T> : ObjectReference<T>, IObjectReferenceWithContext
    {
        private readonly IntPtr _contextCallbackPtr;
        private readonly IntPtr _contextToken;

        private volatile ConcurrentDictionary<IntPtr, IObjectReference> __cachedContext;
        private ConcurrentDictionary<IntPtr, IObjectReference> CachedContext => __cachedContext ?? Make_CachedContext();
        private ConcurrentDictionary<IntPtr, IObjectReference> Make_CachedContext()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __cachedContext, new(), null);
            return __cachedContext;
        }

        // Agile reference can be null, so whether it is set is tracked separately.
        private volatile bool _isAgileReferenceSet;
        private volatile AgileReference __agileReference;
        private AgileReference AgileReference => _isAgileReferenceSet ? __agileReference : Make_AgileReference();
        private unsafe AgileReference Make_AgileReference()
        {
            Context.CallInContext(
                _contextCallbackPtr,
                _contextToken,
#if NET && CsWinRT_LANG_11_FEATURES
                &InitAgileReference,
#else
                InitAgileReference,
#endif
                null,
                this);

            // Set after CallInContext callback given callback can fail to occur.
            _isAgileReferenceSet = true;

            return __agileReference;

            static void InitAgileReference(object state)
            {
                ObjectReferenceWithContext<T> @this = Unsafe.As<ObjectReferenceWithContext<T>>(state);

                global::System.Threading.Interlocked.CompareExchange(ref @this.__agileReference, new AgileReference(@this), null);
            }
        }

        private readonly Guid _iid;

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        internal ObjectReferenceWithContext(IntPtr thisPtr, IntPtr contextCallbackPtr, IntPtr contextToken)
            : base(thisPtr)
        {
            _contextCallbackPtr = contextCallbackPtr;
            _contextToken = contextToken;
        }

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This constructor is setting the '_iid' field directly.")]
#endif
        internal ObjectReferenceWithContext(IntPtr thisPtr, IntPtr contextCallbackPtr, IntPtr contextToken, Guid iid)
            : this(thisPtr, contextCallbackPtr, contextToken)
        {
            if (iid == default)
            {
                ObjectReferenceWithContextHelper.ThrowArgumentExceptionForEmptyIid();
            }

            _iid = iid;
        }

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        internal ObjectReferenceWithContext(IntPtr thisPtr, T vftblT, IntPtr contextCallbackPtr, IntPtr contextToken)
            : base(thisPtr, vftblT)
        {
            _contextCallbackPtr = contextCallbackPtr;
            _contextToken = contextToken;
        }

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This constructor is setting the '_iid' field directly.")]
#endif
        internal ObjectReferenceWithContext(IntPtr thisPtr, T vftblT, IntPtr contextCallbackPtr, IntPtr contextToken, Guid iid)
            : this(thisPtr, vftblT, contextCallbackPtr, contextToken)
        {
            if (iid == default)
            {
                ObjectReferenceWithContextHelper.ThrowArgumentExceptionForEmptyIid();
            }

            _iid = iid;
        }

        private protected override IntPtr GetThisPtrForCurrentContext()
        {
            ObjectReference<T> cachedObjRef = GetCurrentContext();
            if (cachedObjRef == null)
            {
                return base.GetThisPtrForCurrentContext();
            }

            return cachedObjRef.ThisPtr;
        }

        private protected override IntPtr GetContextToken()
        {
            return this._contextToken;
        }

        private protected override T GetVftblForCurrentContext()
        {
            ObjectReference<T> cachedObjRef = GetCurrentContext();
            if (cachedObjRef == null)
            {
                return base.GetVftblForCurrentContext();
            }

            return cachedObjRef.Vftbl;
        }

        // Gets the object reference with respect to the current context.
        // If we are already on the same context as when this object reference was
        // created or failed to switch context, null is returned as the current base
        // object reference should be used.
        private ObjectReference<T> GetCurrentContext()
        {
            IntPtr currentContext = Context.GetContextToken();
            if (_contextCallbackPtr == IntPtr.Zero || currentContext == _contextToken)
            {
                return null;
            }

            // We use a non-generic map of just <IntPtr, IObjectReference> values, to avoid all generic instantiations
            // of ConcurrentDictionary<,> and transitively dependent types for every vtable type T, since it's not
            // something we actually need. Because the cache is private and we're the only ones using it, we can
            // just store the per-context agile references as IObjectReference values, and then cast them on return.
#if NET
            IObjectReference objectReference = CachedContext.GetOrAdd(currentContext, ContextCallbackHolder.Value, this);
#else
            IObjectReference objectReference = CachedContext.GetOrAdd(currentContext, ptr => ContextCallbackHolder.Value(ptr, this));
#endif

            return Unsafe.As<ObjectReference<T>>(objectReference);
        }

        private static class ContextCallbackHolder
        {
            // We have a single lambda expression in this type, so we can manually rewrite it to a 'static readonly'
            // field. This avoids the extra logic to lazily initialized it (it's already lazily initialized because
            // it's in a 'beforefieldinit' type which is only used when the lambda is actually needed), and also it
            // allows storing the entire delegate in the Frozen Object Heap (FOH) on modern runtimes.
            public static readonly Func<IntPtr, IObjectReference, IObjectReference> Value = CreateForCurrentContext;

#if NET
            [UnconditionalSuppressMessage("Trimming", "IL2087", Justification = "The '_iid' field is only empty when using annotated APIs not trim-safe.")]
#endif
            private static IObjectReference CreateForCurrentContext(IntPtr _, IObjectReference state)
            {
                ObjectReferenceWithContext<T> @this = Unsafe.As<ObjectReferenceWithContext<T>>(state);

                var agileReference = @this.AgileReference;

                // We may fail to switch context and thereby not get an agile reference.
                // In these cases, fallback to using the current context.
                if (agileReference == null)
                {
                    return null;
                }

                try
                {
#if NET
                    // On NAOT, we can always assume the IID will not be empty, as the only way to reach that path is by
                    // going through a trim-unsafe constructor, which is explicitly not supported in this configuration.
                    if (!RuntimeFeature.IsDynamicCodeCompiled)
                    {
                        return agileReference.Get<T>(@this._iid);
                    }
#endif

                    if (@this._iid == Guid.Empty)
                    {
                        return agileReference.Get<T>(GuidGenerator.GetIID(typeof(T)));
                    }
                    else
                    {
                        return agileReference.Get<T>(@this._iid);
                    }
                }
                catch (Exception)
                {
                    // Fallback to using the current context in case of error.
                    return null;
                }
            }
        }

        protected override unsafe void Release()
        {
            // Don't initialize cached context by calling through property if it is already null.
            if (__cachedContext != null)
            {
                CachedContext.Clear();
            }

            Context.CallInContext(
                _contextCallbackPtr,
                _contextToken,
#if NET && CsWinRT_LANG_11_FEATURES
                &Release,
                &ReleaseWithoutContext,
#else
                Release,
                ReleaseWithoutContext,
#endif
                this);

            Context.DisposeContextCallback(_contextCallbackPtr);

            static void Release(object state)
            {
                ObjectReferenceWithContext<T> @this = Unsafe.As<ObjectReferenceWithContext<T>>(state);

                @this.ReleaseFromBase();
            }

            static void ReleaseWithoutContext(object state)
            {
                ObjectReferenceWithContext<T> @this = Unsafe.As<ObjectReferenceWithContext<T>>(state);

                @this.ReleaseWithoutContext();
            }
        }

        // Helper stub to invoke 'base.Release()' on a given 'ObjectReferenceWithContext<T>' input parameter.
        // We can't just do 'param.base.Release()' (or something like that), so the only way to specifically
        // invoke the base implementation of an overridden method on that object is to go through a helper
        // instance method invoked on it that just calls the base implementation of the method we want.
        private void ReleaseFromBase()
        {
            base.Release();
        }

        public override ObjectReference<IUnknownVftbl> AsKnownPtr(IntPtr ptr)
        {
            AddRef(true);
            var objRef = new ObjectReferenceWithContext<IUnknownVftbl>(ptr, Context.GetContextCallback(), Context.GetContextToken(), IID.IID_IUnknown)
            {
                IsAggregated = IsAggregated,
                PreventReleaseOnDispose = IsAggregated,
                ReferenceTrackerPtr = ReferenceTrackerPtr
            };
            return objRef;
        }

        internal static new int TryAs(IObjectReference sourceRef, Guid iid, out ObjectReference<T> objRef)
        {
            objRef = null;

            int hr = Marshal.QueryInterface(sourceRef.ThisPtr, ref iid, out IntPtr thatPtr);

            if (hr >= 0)
            {
                if (sourceRef.IsAggregated)
                {
                    Marshal.Release(thatPtr);
                }

                sourceRef.AddRefFromTrackerSource();

                objRef = new ObjectReferenceWithContext<T>(thatPtr, Context.GetContextCallback(), Context.GetContextToken(), iid)
                {
                    IsAggregated = sourceRef.IsAggregated,
                    PreventReleaseOnDispose = sourceRef.IsAggregated,
                    ReferenceTrackerPtr = sourceRef.ReferenceTrackerPtr
                };
            }

            return hr;
        }
    }

    internal static class ObjectReferenceWithContextHelper
    {
        public static void ThrowArgumentExceptionForEmptyIid()
        {
            throw new ArgumentException("The input argument 'iid' cannot be empty and must be set to a valid IID.");
        }
    }

    internal interface IObjectReferenceWithContext
    {
    }

#if EMBED
    internal
#else
    public
#endif
    readonly struct ObjectReferenceValue
    {
        internal readonly IntPtr ptr;
        internal readonly IntPtr referenceTracker;
        internal readonly bool preventReleaseOnDispose;
        // Used to keep the original IObjectReference alive as we share the same
        // referenceTracker instance and in some cases use the same ptr as the
        // IObjectReference without an addref (i.e preventReleaseOnDispose).
        internal readonly IObjectReference objRef;

        internal ObjectReferenceValue(IntPtr ptr) : this()
        {
            this.ptr = ptr;
        }

        internal ObjectReferenceValue(IntPtr ptr, IntPtr referenceTracker, bool preventReleaseOnDispose, IObjectReference objRef)
        {
            this.ptr = ptr;
            this.referenceTracker = referenceTracker;
            this.preventReleaseOnDispose = preventReleaseOnDispose;
            this.objRef = objRef;

        }

        public static ObjectReferenceValue Attach(ref IntPtr thisPtr)
        {
            var obj = new ObjectReferenceValue(thisPtr);
            thisPtr = IntPtr.Zero;
            return obj;
        }

        public readonly IntPtr GetAbi() => ptr;

        public unsafe readonly IntPtr Detach()
        {
            // If the ptr is not owned by this instance, do an AddRef.
            if (preventReleaseOnDispose && ptr != IntPtr.Zero)
            {
                Marshal.AddRef(ptr);
            }

            // Release tracker source reference as it is no longer a managed ref maintained by RCW.
            if (referenceTracker != IntPtr.Zero)
            {
                (**(IReferenceTrackerVftbl**)referenceTracker).ReleaseFromTrackerSource(referenceTracker);
            }

            return ptr;
        }

        public unsafe readonly void Dispose()
        {
            if (referenceTracker != IntPtr.Zero)
            {
                (**(IReferenceTrackerVftbl**)referenceTracker).ReleaseFromTrackerSource(referenceTracker);
            }

            if (!preventReleaseOnDispose && ptr != IntPtr.Zero)
            {
                Marshal.Release(ptr);
            }
        }
    }
}

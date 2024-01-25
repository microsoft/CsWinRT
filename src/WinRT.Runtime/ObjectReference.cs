﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        protected bool disposed;
        private readonly IntPtr _thisPtr;
        private object _disposedLock = new object();
        private IntPtr _referenceTrackerPtr;

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
        }

        ~IObjectReference()
        {
            Dispose(false);
        }

        public ObjectReference<T> As<
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        T>() => As<T>(GuidGenerator.GetIID(typeof(T)));

        public unsafe ObjectReference<T> As<
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        T>(Guid iid)
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

#if !NET
            return (TInterface)typeof(TInterface).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { this });
#else
            return (TInterface)(object)new WinRT.IInspectable(this);
#endif
        }

        public int TryAs<
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            T>(out ObjectReference<T> objRef) => TryAs(GuidGenerator.GetIID(typeof(T)), out objRef);

        public unsafe int TryAs<
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
            T>(Guid iid, out ObjectReference<T> objRef)
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
            var objRef = ObjectReference<IUnknownVftbl>.Attach(ref ptr);
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

        public T AsType<T>()
        {
            ThrowIfDisposed();
            var ctor = typeof(T).GetConstructor(new[] { typeof(IObjectReference) });
            if (ctor != null)
            {
                return (T)ctor.Invoke(new[] { this });
            }
            throw new InvalidOperationException("Target type is not a projected interface.");
        }

        public IntPtr GetRef()
        {
            ThrowIfDisposed();
            AddRef(false);
            return ThisPtr;
        }

        protected void ThrowIfDisposed()
        {
            if (disposed)
            {
                lock (_disposedLock)
                {
                    if (disposed) throw new ObjectDisposedException("ObjectReference");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_disposedLock)
            {
                if (disposed)
                {
                    return;
                }
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
                disposed = true;
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
    class ObjectReference<
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        T> : IObjectReference
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
            T vftblT;
            // With our vtable types, the generic vtables will have System.Delegate fields
            // and the non-generic types will have only void* fields.
            // On .NET 5, we can use RuntimeHelpers.IsReferenceorContainsReferences
            // to disambiguate between generic and non-generic vtables since it's a JIT-time constant.
            // Since it is a JIT time constant, this function will be branchless on .NET 5.
            // On .NET Standard 2.0, the IsReferenceOrContainsReferences method does not exist,
            // so we instead fall back to typeof(T).IsGenericType, which sadly is not a JIT-time constant.
#if !NET
            if (typeof(T).IsGenericType)
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
            {
                vftblT = (T)typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new[] { typeof(IntPtr) }, null).Invoke(new object[] { thisPtr });
            }
            else
            {
                vftblT = Unsafe.Read<T>(*(void***)thisPtr);
            }
            return vftblT;
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

                objRef = ObjectReference<T>.Attach(ref thatPtr);
                objRef.IsAggregated = sourceRef.IsAggregated;
                objRef.PreventReleaseOnDispose = sourceRef.IsAggregated;
                objRef.ReferenceTrackerPtr = sourceRef.ReferenceTrackerPtr;
            }

            return hr;
        }
    }

    internal sealed class ObjectReferenceWithContext<
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        T> : ObjectReference<T>, IObjectReferenceWithContext
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
        private AgileReference Make_AgileReference()
        {
            Context.CallInContext(_contextCallbackPtr, _contextToken, InitAgileReference, null);

            // Set after CallInContext callback given callback can fail to occur.
            _isAgileReferenceSet = true;
            return __agileReference;

            void InitAgileReference()
            {
                global::System.Threading.Interlocked.CompareExchange(ref __agileReference, new AgileReference(this), null);
            }
        }

        private readonly Guid _iid;

        internal ObjectReferenceWithContext(IntPtr thisPtr, IntPtr contextCallbackPtr, IntPtr contextToken)
            : base(thisPtr)
        {
            _contextCallbackPtr = contextCallbackPtr;
            _contextToken = contextToken;
        }

        internal ObjectReferenceWithContext(IntPtr thisPtr, IntPtr contextCallbackPtr, IntPtr contextToken, Guid iid)
            : this(thisPtr, contextCallbackPtr, contextToken)
        {
            _iid = iid;
        }

        internal ObjectReferenceWithContext(IntPtr thisPtr, T vftblT, IntPtr contextCallbackPtr, IntPtr contextToken)
            : base(thisPtr, vftblT)
        {
            _contextCallbackPtr = contextCallbackPtr;
            _contextToken = contextToken;
        }

        internal ObjectReferenceWithContext(IntPtr thisPtr, T vftblT, IntPtr contextCallbackPtr, IntPtr contextToken, Guid iid)
            : this(thisPtr, vftblT, contextCallbackPtr, contextToken)
        {
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
            IObjectReference objectReference = CachedContext.GetOrAdd(currentContext, CreateForCurrentContext);

            return Unsafe.As<ObjectReference<T>>(objectReference);

            IObjectReference CreateForCurrentContext(IntPtr _)
            {
                var agileReference = AgileReference;
                // We may fail to switch context and thereby not get an agile reference.
                // In these cases, fallback to using the current context.
                if (agileReference == null)
                {
                    return null;
                }

                try
                {
                    if (_iid == Guid.Empty)
                    {
                        return agileReference.Get<T>(GuidGenerator.GetIID(typeof(T)));
                    }
                    else
                    {
                        return agileReference.Get<T>(_iid);
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

            Context.CallInContext(_contextCallbackPtr, _contextToken, base.Release, ReleaseWithoutContext);
            Context.DisposeContextCallback(_contextCallbackPtr);
        }

        public override ObjectReference<IUnknownVftbl> AsKnownPtr(IntPtr ptr)
        {
            AddRef(true);
            var objRef = new ObjectReferenceWithContext<IUnknownVftbl>(ptr, Context.GetContextCallback(), Context.GetContextToken())
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

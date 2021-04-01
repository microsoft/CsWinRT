using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    public abstract class IObjectReference : IDisposable
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
                return _thisPtr;
            }
        }

#if DEBUG
        private unsafe uint RefCount
        {
            get
            {
                VftblIUnknown.AddRef(ThisPtr);
                return VftblIUnknown.Release(ThisPtr);
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
                    ReferenceTracker.IUnknownVftbl.AddRef(_referenceTrackerPtr);
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

        protected  unsafe IUnknownVftbl VftblIUnknown
        {
            get
            {
                ThrowIfDisposed();
                return **(IUnknownVftbl**)ThisPtr;
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

        public ObjectReference<T> As<T>() => As<T>(GuidGenerator.GetIID(typeof(T)));
        public unsafe ObjectReference<T> As<T>(Guid iid)
        {
            ThrowIfDisposed();
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out IntPtr thatPtr));
            if (IsAggregated)
            {
                Marshal.Release(thatPtr);
            }
            AddRefFromTrackerSource();

            var objRef = ObjectReference<T>.Attach(ref thatPtr);
            objRef.IsAggregated = IsAggregated;
            objRef.PreventReleaseOnDispose = IsAggregated;
            objRef.ReferenceTrackerPtr = ReferenceTrackerPtr;
            return objRef;
        }

        public unsafe TInterface AsInterface<TInterface>()
        {
            if (typeof(TInterface).GetCustomAttribute(typeof(System.Runtime.InteropServices.ComImportAttribute)) is object)
            {
                Guid iid = typeof(TInterface).GUID;
                Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out IntPtr comPtr));
                try
                {
                    return (TInterface)Marshal.GetObjectForIUnknown(comPtr);
                }
                finally
                {
                    var vftblPtr = Unsafe.AsRef<WinRT.VftblPtr>(comPtr.ToPointer());
                    var vftblIUnknown = Marshal.PtrToStructure<WinRT.Interop.IUnknownVftbl>(vftblPtr.Vftbl);
                    vftblIUnknown.Release(comPtr);
                }
            }

#if NETSTANDARD2_0
            return (TInterface)typeof(TInterface).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { this });
#else
            return (TInterface)(object)new WinRT.IInspectable(this);
#endif
        }

        public int TryAs<T>(out ObjectReference<T> objRef) => TryAs(GuidGenerator.GetIID(typeof(T)), out objRef);

        public virtual unsafe int TryAs<T>(Guid iid, out ObjectReference<T> objRef)
        {
            objRef = null;
            ThrowIfDisposed();
            int hr = VftblIUnknown.QueryInterface(ThisPtr, ref iid, out IntPtr thatPtr);
            if (hr >= 0)
            {
                if (IsAggregated)
                {
                    Marshal.Release(thatPtr);
                }
                AddRefFromTrackerSource();

                objRef = ObjectReference<T>.Attach(ref thatPtr);
                objRef.IsAggregated = IsAggregated;
                objRef.PreventReleaseOnDispose = IsAggregated;
                objRef.ReferenceTrackerPtr = ReferenceTrackerPtr;
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

        internal bool Resurrect()
        {
            lock (_disposedLock)
            {
                if (!disposed)
                {
                    return false;
                }
                disposed = false;
                AddRef();
                GC.ReRegisterForFinalize(this);
                return true;
            }
        }

        protected virtual unsafe void AddRef(bool refFromTrackerSource)
        {
            VftblIUnknown.AddRef(ThisPtr);
            if(refFromTrackerSource)
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
            VftblIUnknown.Release(ThisPtr);
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
                ReferenceTracker.IUnknownVftbl.Release(ReferenceTrackerPtr);
            }
        }
    }

    public class ObjectReference<T> : IObjectReference
    {
        private readonly T _vftbl;
        public T Vftbl
        {
            get
            {
                ThrowIfDisposed();
                return _vftbl;
            }
        }

        public static ObjectReference<T> Attach(ref IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new ObjectReference<T>(thisPtr);
            thisPtr = IntPtr.Zero;
            return obj;
        }

        ObjectReference(IntPtr thisPtr, T vftblT) :
            base(thisPtr)
        {
            _vftbl = vftblT;
        }

        private protected ObjectReference(IntPtr thisPtr) :
            this(thisPtr, GetVtable(thisPtr))
        {
        }

        public static unsafe ObjectReference<T> FromAbi(IntPtr thisPtr, T vftblT)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new ObjectReference<T>(thisPtr, vftblT);
            obj.VftblIUnknown.AddRef(obj.ThisPtr);
            return obj;
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

        private static unsafe T GetVtable(IntPtr thisPtr)
        {
            var vftblPtr = Unsafe.AsRef<VftblPtr>(thisPtr.ToPointer());
            T vftblT;
            // With our vtable types, the generic vtables will have System.Delegate fields
            // and the non-generic types will have only void* fields.
            // On .NET 5, we can use RuntimeHelpers.IsReferenceorContainsReferences
            // to disambiguate between generic and non-generic vtables since it's a JIT-time constant.
            // Since it is a JIT time constant, this function will be branchless on .NET 5.
            // On .NET Standard 2.0, the IsReferenceOrContainsReferences method does not exist,
            // so we instead fall back to typeof(T).IsGenericType, which sadly is not a JIT-time constant.
#if NETSTANDARD2_0
            if (typeof(T).IsGenericType)
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
            {
                vftblT = (T)typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new[] { typeof(IntPtr) }, null).Invoke(new object[] { thisPtr });
            }
            else
            {
                vftblT = Unsafe.AsRef<T>(vftblPtr.Vftbl.ToPointer());
            }
            return vftblT;
        }
    }

    internal class ObjectReferenceWithContext<T> : ObjectReference<T>
    {
        private static readonly Guid IID_ICallbackWithNoReentrancyToApplicationSTA = Guid.Parse("0A299774-3E4E-FC42-1D9D-72CEE105CA57");
        private readonly IntPtr _contextCallbackPtr;

        internal ObjectReferenceWithContext(IntPtr thisPtr, IntPtr contextCallbackPtr)
            :base(thisPtr)
        {
            _contextCallbackPtr = contextCallbackPtr;
        }

        protected override unsafe void Release()
        {
            ComCallData data = default;
            IntPtr contextCallbackPtr = _contextCallbackPtr;

            var contextCallback = new ABI.WinRT.Interop.IContextCallback(ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.Attach(ref contextCallbackPtr));

            contextCallback.ContextCallback(_ =>
            {
                base.Release();
                return 0;
            }, &data, IID_ICallbackWithNoReentrancyToApplicationSTA, 5);
        }

        public override unsafe int TryAs<U>(Guid iid, out ObjectReference<U> objRef)
        {
            objRef = null;
            int hr = VftblIUnknown.QueryInterface(ThisPtr, ref iid, out IntPtr thatPtr);
            if (hr >= 0)
            {
                if (IsAggregated)
                {
                    Marshal.Release(thatPtr);
                }
                AddRefFromTrackerSource();

                using (var contextCallbackReference = ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.FromAbi(_contextCallbackPtr))
                {
                    objRef = new ObjectReferenceWithContext<U>(thatPtr, contextCallbackReference.GetRef())
                    {
                        IsAggregated = IsAggregated,
                        PreventReleaseOnDispose = IsAggregated,
                        ReferenceTrackerPtr = ReferenceTrackerPtr
                    };
                }
            }
            return hr;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
#endif

        protected unsafe IUnknownVftbl VftblIUnknown
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
            Marshal.ThrowExceptionForHR(QueryInterface(ref iid, out IntPtr thatPtr));
            return ObjectReference<T>.Attach(ref thatPtr);
        }

        public unsafe TInterface AsInterface<TInterface>()
        {
            if (typeof(TInterface).GetCustomAttribute(typeof(System.Runtime.InteropServices.ComImportAttribute)) is object)
            {
                Guid iid = typeof(TInterface).GUID;
                Marshal.ThrowExceptionForHR(QueryInterface(ref iid, out IntPtr comPtr));
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
            int hr = QueryInterface(ref iid, out IntPtr thatPtr);
            if (hr >= 0)
            {
                objRef = ObjectReference<T>.Attach(ref thatPtr);
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
            AddRef();
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
                Release();
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

        protected virtual unsafe int QueryInterface(ref Guid iid, out IntPtr thatPtr)
        {
            return VftblIUnknown.QueryInterface(ThisPtr, ref iid, out thatPtr);
        }

        protected virtual unsafe void AddRef()
        {
            VftblIUnknown.AddRef(ThisPtr);
        }

        protected virtual unsafe void Release()
        {
            VftblIUnknown.Release(ThisPtr);
        }

        internal unsafe bool IsReferenceToManagedObject
        {
            get
            {
                return VftblIUnknown.Equals(IUnknownVftbl.AbiToProjectionVftbl);
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
            obj.AddRef();
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

        protected static unsafe T GetVtable(IntPtr thisPtr)
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

        internal ObjectReferenceWithContext(IntPtr intPtr, IntPtr contextCallbackPtr)
            : base(intPtr)
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
            int hr = QueryInterface(ref iid, out IntPtr thatPtr);
            if (hr >= 0)
            {
                using (var contextCallbackReference = ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.FromAbi(_contextCallbackPtr))
                {
                    objRef = new ObjectReferenceWithContext<U>(thatPtr, contextCallbackReference.GetRef());
                }
            }
            return hr;
        }
    }
    
    internal class WeakCount
    {
        internal int Increment() => 
            System.Threading.Interlocked.Increment(ref _count);

        internal int Decrement()
        {
            var count = System.Threading.Interlocked.Decrement(ref _count);
            if (count < 0)
            {
                throw new InvalidOperationException("WeakObjectReference has been over-released!");
            }
            return count;
        }

        private int _count;
    }

    internal class WeakObjectReference<T> : ObjectReference<T>
    {
        public WeakCount Count { get; private set; }

        internal WeakObjectReference(IntPtr ptr) : base(ptr)
        {
            Count = new WeakCount();
            WeakAddRef();
        }

        internal WeakObjectReference(WeakCount count, IntPtr ptr) : base(ptr)
        {
            Count = count;
            WeakAddRef();
        }

        public static ObjectReference<T> Attach(ref IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new WeakObjectReference<T>(thisPtr);
            thisPtr = IntPtr.Zero;
            return obj;
        }

        public static WeakObjectReference<T> FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new WeakObjectReference<T>(thisPtr);
            return obj;
        }

        private protected void StrongAddRef() => base.AddRef();

        private protected void StrongRelease() => base.Release();

        private protected void WeakAddRef()
        {
            Count.Increment();
        }

        private protected void WeakRelease()
        {
            var count = Count.Decrement();
            if (count < 0)
            {
                throw new InvalidOperationException("Weak object reference has been over-released!");
            }
            if (count == 0)
            {
                StrongRelease();
            }
        }

        protected override unsafe int QueryInterface(ref Guid iid, out IntPtr thatPtr)
        {
            int hr = base.QueryInterface(ref iid, out thatPtr);
            //if (hr >= 0)
            //{
            //    WeakAddRef();
            //    StrongRelease();
            //}
            return hr;
        }

        protected override unsafe void AddRef() => WeakAddRef();

        protected override unsafe void Release() => WeakRelease();

        public override unsafe int TryAs<T>(Guid iid, out ObjectReference<T> objRef)
        {
            objRef = null;
            ThrowIfDisposed();
            int hr = QueryInterface(ref iid, out IntPtr thatPtr);
            if (hr >= 0)
            {
//                objRef = WeakObjectReference<T>.Attach(ref thatPtr);
                objRef = ObjectReference<T>.Attach(ref thatPtr);
            }
            return hr;
        }
    }

    internal class WeakObjectReferenceWithContext<T> : WeakObjectReference<T>
    {
        private static readonly Guid IID_ICallbackWithNoReentrancyToApplicationSTA = Guid.Parse("0A299774-3E4E-FC42-1D9D-72CEE105CA57");
        private readonly IntPtr _contextCallbackPtr;

        internal WeakObjectReferenceWithContext(WeakCount count, IntPtr thisPtr, IntPtr contextCallbackPtr)
            : base(count, thisPtr)
        {
            _contextCallbackPtr = contextCallbackPtr;
        }

        protected override unsafe void Release()
        {
            disposed = true;
            if (Count.Decrement() != 0)
            {
                return;
            }

            ComCallData data = default;
            IntPtr contextCallbackPtr = _contextCallbackPtr;

            var contextCallback = new ABI.WinRT.Interop.IContextCallback(ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.Attach(ref contextCallbackPtr));

            contextCallback.ContextCallback(_ =>
            {
                StrongRelease();
                return 0;
            }, &data, IID_ICallbackWithNoReentrancyToApplicationSTA, 5);
        }

        //public override unsafe int TryAs<U>(Guid iid, out ObjectReference<U> objRef)
        //{
        //    objRef = null;
        //    int hr = QueryInterface(ref iid, out IntPtr thatPtr);
        //    if (hr >= 0)
        //    {
        //        using (var contextCallbackReference = ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.FromAbi(_contextCallbackPtr))
        //        {
        //            objRef = new WeakObjectReferenceWithContext<U>(Count, thatPtr, contextCallbackReference.GetRef());
        //        }
        //    }
        //    return hr;
        //}
    }
}
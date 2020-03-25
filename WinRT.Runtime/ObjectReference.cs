﻿using System;
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
        public IntPtr ThisPtr
        {
            get
            {
                ThrowIfDisposed();
                return _thisPtr;
            }
        }

        protected IUnknownVftbl VftblIUnknown
        {
            get
            {
                ThrowIfDisposed();
                return VftblIUnknownUnsafe;
            }
        }

        protected virtual IUnknownVftbl VftblIUnknownUnsafe { get; }

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
        public virtual unsafe ObjectReference<T> As<T>(Guid iid)
        {
            ThrowIfDisposed();
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out IntPtr thatPtr));
            return ObjectReference<T>.Attach(ref thatPtr);
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
            if (disposed) throw new ObjectDisposedException("ObjectReference");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            Release();
            disposed = true;
        }

        protected virtual void AddRef()
        {
            VftblIUnknown.AddRef(ThisPtr);
        }

        protected virtual void Release()
        {
            var releaseDelegate = VftblIUnknown.Release;

            if (releaseDelegate is null)
            {
                unsafe
                {
                    // If we're in the finalizer, then releaseDelegate might already be null.
                    // In this case, we need to re-fetch the delegate from the vtable.
                    // TODO: Fix this once we have function pointers.
                    releaseDelegate = Marshal.PtrToStructure<IUnknownVftbl>(Unsafe.AsRef<VftblPtr>(ThisPtr.ToPointer()).Vftbl).Release;
                }
            }

            releaseDelegate(ThisPtr);
        }

        internal unsafe bool IsReferenceToManagedObject
        {
            get
            {
                using var unknownObjRef = As<IUnknownVftbl>();
                return ((VftblPtr*)unknownObjRef.ThisPtr.ToPointer())->Vftbl == IUnknownVftbl.AbiToProjectionVftblPtr;
            }
        }
    }

    public class ObjectReference<T> : IObjectReference
    {
        protected override IUnknownVftbl VftblIUnknownUnsafe => _vftblIUnknown;
        readonly IUnknownVftbl _vftblIUnknown;
        public readonly T Vftbl;

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

        ObjectReference(IntPtr thisPtr, IUnknownVftbl vftblIUnknown, T vftblT) :
            base(thisPtr)
        {
            _vftblIUnknown = vftblIUnknown;
            Vftbl = vftblT;
        }

        private protected ObjectReference(IntPtr thisPtr) :
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
        private static unsafe (IUnknownVftbl vftblIUnknown, T vftblT) GetVtables(IntPtr thisPtr)
        {
            var vftblPtr = Unsafe.AsRef<VftblPtr>(thisPtr.ToPointer());
            var vftblIUnknown = Marshal.PtrToStructure<IUnknownVftbl>(vftblPtr.Vftbl);
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

        public override ObjectReference<U> As<U>(Guid iid)
        {
            ThrowIfDisposed();
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out IntPtr thatPtr));
            using (var contextCallbackReference = ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.FromAbi(_contextCallbackPtr))
            {
                return new ObjectReferenceWithContext<U>(thatPtr, contextCallbackReference.GetRef()); 
            }
        }
    }
}

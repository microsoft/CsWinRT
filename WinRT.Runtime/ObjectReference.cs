using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{

    internal struct VftblPtr
    {
        public IntPtr Vftbl;
    }

    public abstract class IObjectReference : IDisposable
    {
        protected bool disposed;
        public readonly IntPtr ThisPtr;
        protected virtual Interop.IUnknownVftbl VftblIUnknown { get; }

        protected IObjectReference(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(thisPtr));
            }
            ThisPtr = thisPtr;
        }

        ~IObjectReference()
        {
            Dispose(false);
        }

        public ObjectReference<T> As<T>() => As<T>(GuidGenerator.GetIID(typeof(T)));
        public virtual unsafe ObjectReference<T> As<T>(Guid iid)
        {
            ThrowIfDisposed();
            IntPtr thatPtr;
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out thatPtr));
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
            VftblIUnknown.Release(ThisPtr);
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
        private static (IUnknownVftbl vftblIUnknown, T vftblT) GetVtables(IntPtr thisPtr)
        {
            var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
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
        private readonly IContextCallback _context;

        internal ObjectReferenceWithContext(IntPtr thisPtr, IContextCallback context)
            :base(thisPtr)
        {
            _context = context;
        }

        protected override unsafe void Release()
        {
            ComCallData data = default;

            _context.ContextCallback(_ =>
            {
                base.Release();
                return 0;
            }, &data, IID_ICallbackWithNoReentrancyToApplicationSTA, 5);
        }

        public override ObjectReference<U> As<U>(Guid iid)
        {
            ThrowIfDisposed();
            IntPtr thatPtr;
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out thatPtr));
            return new ObjectReferenceWithContext<U>(thatPtr, _context);
        }
    }
}

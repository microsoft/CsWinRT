using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using WinRT;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT.Interop
{
    interface IAgileReference
    {
        uint AddRef();
        IObjectReference Resolve(Guid iid);
        uint Release();
    }
}

namespace ABI.WinRT.Interop
{
    using global::WinRT.Interop;

    class IAgileReference : global::WinRT.Interop.IAgileReference
    {
        public struct Vftbl
        {
            public delegate int _Resolve(IntPtr thisPtr, ref Guid iid, out IntPtr ppvObject);
            public IUnknownVftbl IUnknownVftbl;
            public _Resolve Resolve_1;
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IAgileReference(IObjectReference obj) => (obj != null) ? new IAgileReference(obj) : null;
        public static implicit operator IAgileReference(ObjectReference<Vftbl> obj) => (obj != null) ? new IAgileReference(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IAgileReference(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IAgileReference(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public IObjectReference Resolve(Guid iid)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.Resolve_1(ThisPtr, ref iid, out IntPtr obj));
            return ObjectReference<IUnknownVftbl>.Attach(ref obj);
        }

        public uint AddRef()
        {
            return _obj.Vftbl.IUnknownVftbl.AddRef(ThisPtr);
        }

        public uint Release()
        {
            return _obj.Vftbl.IUnknownVftbl.Release(ThisPtr);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.Microsoft.UI.Xaml.Data
{
    [Guid("63D0C952-396B-54F4-AF8C-BA8724A427BF")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IPropertyChangedEventArgsVftbl
    {
        internal IInspectable.Vftbl IInspectableVftbl;
        private void* _get_PropertyName_0;
        public delegate* stdcall<IntPtr, IntPtr*, int> get_PropertyName_0 => (delegate* stdcall<IntPtr, IntPtr*, int>)_get_PropertyName_0;
    }


    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("7C0C27A8-0B41-5070-B160-FC9AE960A36C")]
    internal unsafe class WinRTPropertyChangedEventArgsRuntimeClassFactory
    {
        [Guid("7C0C27A8-0B41-5070-B160-FC9AE960A36C")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _CreateInstance_0;
            public delegate* stdcall<IntPtr, IntPtr, IntPtr, IntPtr*, IntPtr*, int> CreateInstance_0 => (delegate* stdcall<IntPtr, IntPtr, IntPtr, IntPtr*, IntPtr*, int>)_CreateInstance_0;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator WinRTPropertyChangedEventArgsRuntimeClassFactory(IObjectReference obj) => (obj != null) ? new WinRTPropertyChangedEventArgsRuntimeClassFactory(obj) : null;
        public static implicit operator WinRTPropertyChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj) => (obj != null) ? new WinRTPropertyChangedEventArgsRuntimeClassFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public WinRTPropertyChangedEventArgsRuntimeClassFactory(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public WinRTPropertyChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe IObjectReference CreateInstance(string name, object baseInterface, out IObjectReference innerInterface)
        {
            MarshalString __name = default;
            IObjectReference __baseInterface = default;
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                __name = MarshalString.CreateMarshaler(name);
                __baseInterface = MarshalInspectable.CreateMarshaler(baseInterface);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstance_0(ThisPtr, MarshalString.GetAbi(__name), MarshalInspectable.GetAbi(__baseInterface), &__innerInterface, &__retval));
                innerInterface = ObjectReference<IUnknownVftbl>.FromAbi(__innerInterface);
                return ObjectReference<IUnknownVftbl>.Attach(ref __retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__name);
                MarshalInspectable.DisposeMarshaler(__baseInterface);
                MarshalInspectable.DisposeAbi(__innerInterface);
                MarshalInspectable.DisposeAbi(__retval);
            }
        }
    }
}

namespace ABI.System.ComponentModel
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PropertyChangedEventArgs
    {
        private static WeakLazy<ActivationFactory> _propertyChangedArgsFactory = new WeakLazy<ActivationFactory>();

        private class ActivationFactory : BaseActivationFactory
        {
            public ActivationFactory() : base("Microsoft.UI.Xaml.Data", "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs")
            {
            }
        }

        public static IObjectReference CreateMarshaler(global::System.ComponentModel.PropertyChangedEventArgs value)
        {
            if (value is null)
            {
                return null;
            }

            ABI.Microsoft.UI.Xaml.Data.WinRTPropertyChangedEventArgsRuntimeClassFactory factory = _propertyChangedArgsFactory.Value._As<ABI.Microsoft.UI.Xaml.Data.WinRTPropertyChangedEventArgsRuntimeClassFactory.Vftbl>();
            return factory.CreateInstance(value.PropertyName, null, out _);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static global::System.ComponentModel.PropertyChangedEventArgs FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            using var args = ObjectReference<ABI.Microsoft.UI.Xaml.Data.IPropertyChangedEventArgsVftbl>.FromAbi(ptr);
            IntPtr propertyName = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(args.Vftbl.get_PropertyName_0(args.ThisPtr, &propertyName));
                return new global::System.ComponentModel.PropertyChangedEventArgs(MarshalString.FromAbi(propertyName));
            }
            finally
            {
                MarshalString.DisposeAbi(propertyName);
            }
        }

        public static unsafe void CopyManaged(global::System.ComponentModel.PropertyChangedEventArgs o, IntPtr dest)
        {
            using var objRef = CreateMarshaler(o);
            *(IntPtr*)dest.ToPointer() = objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static IntPtr FromManaged(global::System.ComponentModel.PropertyChangedEventArgs value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref abi); }

        public static string GetGuidSignature()
        {
            return "rc(Microsoft.UI.Xaml.Data.NotifyPropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})";
        }
    }
}

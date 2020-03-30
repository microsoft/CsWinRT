using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.Windows.UI.Xaml.Data
{
    [Guid("4F33A9A0-5CF4-47A4-B16F-D7FAAF17457E")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct IPropertyChangedEventArgsVftbl
    {
        internal IInspectable.Vftbl IInspectableVftbl;
        public _get_PropertyAsString get_PropertyName_0;
    }


    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("6DCC9C03-E0C7-4EEE-8EA9-37E3406EEB1C")]
    internal class WinRTPropertyChangedEventArgsRuntimeClassFactory
    {
        [Guid("6DCC9C03-E0C7-4EEE-8EA9-37E3406EEB1C")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            public unsafe delegate int _CreateInstance_0(IntPtr thisPtr, IntPtr name, IntPtr baseInterface, out IntPtr innerInterface, out IntPtr value);
            internal IInspectable.Vftbl IInspectableVftbl;
            public _CreateInstance_0 CreateInstance_0;
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

        public unsafe IObjectReference CreateInstance(string name, object baseInterface, out object innerInterface)
        {
            MarshalString __name = default;
            IObjectReference __baseInterface = default;
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                __name = MarshalString.CreateMarshaler(name);
                __baseInterface = MarshalInspectable.CreateMarshaler(baseInterface);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstance_0(ThisPtr, MarshalString.GetAbi(__name), MarshalInspectable.GetAbi(__baseInterface), out __innerInterface, out __retval));
                innerInterface = MarshalInspectable.FromAbi(__innerInterface);
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
    public struct PropertyChangedEventArgs
    {
        private static WeakLazy<ActivationFactory> _propertyChangedArgsFactory = new WeakLazy<ActivationFactory>();

        private class ActivationFactory
        {
            public BaseActivationFactory Factory { get; }
            public ActivationFactory()
            {
                try
                {
                    Factory = new BaseActivationFactory("Microsoft.UI.Xaml.Data", "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs");
                }
                catch (global::System.Exception)
                {
                    Factory = new BaseActivationFactory("Windows.UI.Xaml.Data", "Windows.UI.Xaml.Data.PropertyChangedEventArgs");
                }
            }
        }

        public static IObjectReference CreateMarshaler(global::System.ComponentModel.PropertyChangedEventArgs value)
        {
            if (value is null)
            {
                return null;
            }

            ABI.Windows.UI.Xaml.Data.WinRTPropertyChangedEventArgsRuntimeClassFactory factory = _propertyChangedArgsFactory.Value.Factory._As<ABI.Windows.UI.Xaml.Data.WinRTPropertyChangedEventArgsRuntimeClassFactory.Vftbl>();
            return factory.CreateInstance(value.PropertyName, null, out _);
        }

        public static IntPtr GetAbi(IObjectReference m) => m.ThisPtr;

        public static global::System.ComponentModel.PropertyChangedEventArgs FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            using var uri = ObjectReference<ABI.Windows.UI.Xaml.Data.IPropertyChangedEventArgsVftbl>.FromAbi(ptr);
            IntPtr propertyName = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(uri.Vftbl.get_PropertyName_0(uri.ThisPtr, out propertyName));
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

        public static void DisposeMarshaler(IObjectReference m) { m.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref abi); }

        public static string[] GetGuidSignatures()
        {
            return GuidSignatures;
        }

        private static readonly string[] GuidSignatures = new[]
        {
            "rc(Windows.UI.Xaml.Data.NotifyPropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})",
            "rc(Microsoft.UI.Xaml.Data.NotifyPropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})",
        };
    }
}

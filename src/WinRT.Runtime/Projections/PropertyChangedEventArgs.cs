// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> get_PropertyName_0 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_get_PropertyName_0;
    }

#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
#endif
    [Guid("7C0C27A8-0B41-5070-B160-FC9AE960A36C")]
    internal sealed unsafe class WinRTPropertyChangedEventArgsRuntimeClassFactory
    {
        [Guid("7C0C27A8-0B41-5070-B160-FC9AE960A36C")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _CreateInstance_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr*, IntPtr*, int> CreateInstance_0 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr*, IntPtr*, int>)_CreateInstance_0;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_PropertyChangedEventArgsRuntimeClassFactory);

        public static implicit operator WinRTPropertyChangedEventArgsRuntimeClassFactory(IObjectReference obj) => (obj != null) ? new WinRTPropertyChangedEventArgsRuntimeClassFactory(obj) : null;
        public static implicit operator WinRTPropertyChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj) => (obj != null) ? new WinRTPropertyChangedEventArgsRuntimeClassFactory(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public WinRTPropertyChangedEventArgsRuntimeClassFactory(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public WinRTPropertyChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe IObjectReference CreateInstance(string name, object baseInterface, out IObjectReference innerInterface)
        {
            IObjectReference __baseInterface = default;
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                MarshalString.Pinnable __name = new(name);
                fixed (void* ___name = __name)
                {
                    __baseInterface = MarshalInspectable<object>.CreateMarshaler(baseInterface);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstance_0(ThisPtr, MarshalString.GetAbi(ref __name), MarshalInspectable<object>.GetAbi(__baseInterface), &__innerInterface, &__retval));
                    innerInterface = ObjectReference<IUnknownVftbl>.FromAbi(__innerInterface, IID.IID_IUnknown);
                    return ObjectReference<IUnknownVftbl>.Attach(ref __retval, IID.IID_IUnknown);
                }
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__baseInterface);
                MarshalInspectable<object>.DisposeAbi(__innerInterface);
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        public unsafe ObjectReferenceValue CreateInstance(string name)
        {
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                MarshalString.Pinnable __name = new(name);
                fixed (void* ___name = __name)
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstance_0(ThisPtr, MarshalString.GetAbi(ref __name), IntPtr.Zero, &__innerInterface, &__retval));
                    return new ObjectReferenceValue(__retval);
                }
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__innerInterface);
            }
        }

    }
}

namespace ABI.System.ComponentModel
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    unsafe struct PropertyChangedEventArgs
    {
        private static ABI.Microsoft.UI.Xaml.Data.WinRTPropertyChangedEventArgsRuntimeClassFactory Instance = ActivationFactory.Get("Microsoft.UI.Xaml.Data.PropertyChangedEventArgs");

        public static IObjectReference CreateMarshaler(global::System.ComponentModel.PropertyChangedEventArgs value)
        {
            if (value is null)
            {
                return null;
            }

            return Instance.CreateInstance(value.PropertyName, null, out _);
        }

        public static ObjectReferenceValue CreateMarshaler2(global::System.ComponentModel.PropertyChangedEventArgs value)
        {
            if (value is null)
            {
                return new ObjectReferenceValue();
            }

            return Instance.CreateInstance(value.PropertyName);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static global::System.ComponentModel.PropertyChangedEventArgs FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            IntPtr propertyName = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR((**(ABI.Microsoft.UI.Xaml.Data.IPropertyChangedEventArgsVftbl**)ptr).get_PropertyName_0(ptr, &propertyName));
                return new global::System.ComponentModel.PropertyChangedEventArgs(MarshalString.FromAbi(propertyName));
            }
            finally
            {
                MarshalString.DisposeAbi(propertyName);
            }
        }

        public static unsafe void CopyManaged(global::System.ComponentModel.PropertyChangedEventArgs o, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(o).Detach();
        }

        public static IntPtr FromManaged(global::System.ComponentModel.PropertyChangedEventArgs value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler2(value).Detach();
        }

        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { MarshalInspectable<object>.DisposeAbi(abi); }

        public static unsafe MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.MarshalerArray CreateMarshalerArray(global::System.ComponentModel.PropertyChangedEventArgs[] array) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.CreateMarshalerArray2(array, CreateMarshaler2);
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.GetAbiArray(box);
        public static unsafe global::System.ComponentModel.PropertyChangedEventArgs[] FromAbiArray(object box) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.FromAbiArray(box, FromAbi);
        public static void CopyAbiArray(global::System.ComponentModel.PropertyChangedEventArgs[] array, object box) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.CopyAbiArray(array, box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::System.ComponentModel.PropertyChangedEventArgs[] array) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.FromManagedArray(array, FromManaged);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.MarshalerArray array) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventArgs>.DisposeMarshalerArray(array);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);

        public static string GetGuidSignature()
        {
            return "rc(Microsoft.UI.Xaml.Data.NotifyPropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})";
        }
    }
}

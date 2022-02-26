﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.Microsoft.UI.Xaml.Data
{
    [Guid("D026DD64-5F26-5F15-A86A-0DEC8A431796")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IDataErrorsChangedEventArgsVftbl
    {
        internal IInspectable.Vftbl IInspectableVftbl;
        
        private void* _get_PropertyName_0;
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> get_PropertyName_0 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_get_PropertyName_0;

        private void* _put_PropertyName_1;
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> put_PropertyName_1 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_put_PropertyName_1;
    }


    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("62D0BD1E-B85F-5FCC-842A-7CB0DDA37FE5")]
    internal unsafe sealed class WinRTDataErrorsChangedEventArgsRuntimeClassFactory
    {
        [Guid("62D0BD1E-B85F-5FCC-842A-7CB0DDA37FE5")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _CreateInstance_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int> CreateInstance_0 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>)_CreateInstance_0;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator WinRTDataErrorsChangedEventArgsRuntimeClassFactory(IObjectReference obj) => (obj != null) ? new WinRTDataErrorsChangedEventArgsRuntimeClassFactory(obj) : null;
        public static implicit operator WinRTDataErrorsChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj) => (obj != null) ? new WinRTDataErrorsChangedEventArgsRuntimeClassFactory(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public WinRTDataErrorsChangedEventArgsRuntimeClassFactory(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public WinRTDataErrorsChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe IObjectReference CreateInstance(string name)
        {
            IntPtr __retval = default;
            try
            {
                MarshalString.Pinnable __name = new(name);
                fixed (void* ___name = __name)
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstance_0(ThisPtr, MarshalString.GetAbi(ref __name), &__retval));
                    return ObjectReference<IUnknownVftbl>.Attach(ref __retval);
                }
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        public unsafe ObjectReferenceValue CreateInstanceForMarshaling(string name)
        {
            IntPtr __retval = default;
            MarshalString.Pinnable __name = new(name);
            fixed (void* ___name = __name)
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstance_0(ThisPtr, MarshalString.GetAbi(ref __name), &__retval));
                return new ObjectReferenceValue(__retval);
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
    unsafe struct DataErrorsChangedEventArgs
    {
        private sealed class ActivationFactory : BaseActivationFactory
        {
            public ActivationFactory() : base("Microsoft.UI.Xaml.Data", "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs")
            {
            }

            internal static ABI.Microsoft.UI.Xaml.Data.WinRTDataErrorsChangedEventArgsRuntimeClassFactory Instance =
                new ActivationFactory()._As<ABI.Microsoft.UI.Xaml.Data.WinRTDataErrorsChangedEventArgsRuntimeClassFactory.Vftbl>();
        }

        public static IObjectReference CreateMarshaler(global::System.ComponentModel.DataErrorsChangedEventArgs value)
        {
            if (value is null)
            {
                return null;
            }

            return ActivationFactory.Instance.CreateInstance(value.PropertyName);
        }

        public static ObjectReferenceValue CreateMarshaler2(global::System.ComponentModel.DataErrorsChangedEventArgs value)
        {
            if (value is null)
            {
                return new ObjectReferenceValue();
            }

            return ActivationFactory.Instance.CreateInstanceForMarshaling(value.PropertyName);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static global::System.ComponentModel.DataErrorsChangedEventArgs FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            IntPtr propertyName = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR((**(ABI.Microsoft.UI.Xaml.Data.IDataErrorsChangedEventArgsVftbl**)ptr).get_PropertyName_0(ptr, &propertyName));
                return new global::System.ComponentModel.DataErrorsChangedEventArgs(MarshalString.FromAbi(propertyName));
            }
            finally
            {
                MarshalString.DisposeAbi(propertyName);
            }
        }

        public static unsafe void CopyManaged(global::System.ComponentModel.DataErrorsChangedEventArgs o, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(o).Detach();
        }

        public static IntPtr FromManaged(global::System.ComponentModel.DataErrorsChangedEventArgs value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler2(value).Detach();
        }

        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { MarshalInspectable<object>.DisposeAbi(abi); }

        public static string GetGuidSignature()
        {
            return "rc(Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs;{d026dd64-5f26-5f15-a86a-0dec8a431796})";
        }
    }
}

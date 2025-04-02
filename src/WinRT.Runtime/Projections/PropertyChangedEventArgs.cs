// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.Microsoft.UI.Xaml.Data
{
    internal sealed unsafe class PropertyChangedEventArgsRuntimeClassFactory
    {
        private readonly IObjectReference _obj;

        public PropertyChangedEventArgsRuntimeClassFactory()
        {
#if NET
            _obj = FeatureSwitches.UseWindowsUIXamlProjections
                ? ActivationFactory.Get("Windows.UI.Xaml.Data.PropertyChangedEventArgs", IID.IID_WUX_PropertyChangedEventArgsRuntimeClassFactory)
                : ActivationFactory.Get("Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", IID.IID_MUX_PropertyChangedEventArgsRuntimeClassFactory);
#else
            _obj = ActivationFactory.Get<IUnknownVftbl>("Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", IID.IID_MUX_PropertyChangedEventArgsRuntimeClassFactory);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IObjectReference CreateInstance(string name, object baseInterface, out IObjectReference innerInterface)
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
                    IntPtr thisPtr = _obj.ThisPtr;
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(
                        ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr*, IntPtr*, int>)(*(void***)thisPtr)[6])(
                            thisPtr,
                            MarshalString.GetAbi(ref __name),
                            MarshalInspectable<object>.GetAbi(__baseInterface),
                            &__innerInterface,
                            &__retval));
                    global::System.GC.KeepAlive(_obj);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ObjectReferenceValue CreateInstance(string name)
        {
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                MarshalString.Pinnable __name = new(name);
                fixed (void* ___name = __name)
                {
                    IntPtr thisPtr = _obj.ThisPtr;
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(
                        ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr*, IntPtr*, int>)(*(void***)thisPtr)[6])(
                            thisPtr,
                            MarshalString.GetAbi(ref __name),
                            IntPtr.Zero,
                            &__innerInterface,
                            &__retval));
                    global::System.GC.KeepAlive(_obj);
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
        private static readonly ABI.Microsoft.UI.Xaml.Data.PropertyChangedEventArgsRuntimeClassFactory Instance = new();

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
                // We can use the Microsoft.UI.Xaml.Data.IPropertyChangedEventArgsVftbl here in both WUX and MUX because the vtables are laid out the same and we know
                // that we have either a MUX or WUX IPropertyChangedEventArgs pointer in ptr.
                ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)ptr)[6])(ptr, &propertyName));
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
            if (FeatureSwitches.UseWindowsUIXamlProjections)
            {
                return "rc(Windows.UI.Xaml.Data.PropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})";
            }

            return "rc(Microsoft.UI.Xaml.Data.PropertyChangedEventArgs;{63d0c952-396b-54f4-af8c-ba8724a427bf})";
        }
    }
}

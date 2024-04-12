// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Windows.Foundation
{
    // Provide a stub definition of IReferenceArray so we have
    // a "public" type for the type mapping definition.
    // IReferenceArray cannot appear in signatures, so it doesn't need to actually be public.
    [Guid("61C17707-2D65-11E0-9AE8-D48564015472")]
    [WindowsRuntimeHelperType(typeof(global::ABI.Windows.Foundation.IReferenceArray<>))]
    internal interface IReferenceArray<T>
    {
        T[] Value { get; }
    }
}

namespace ABI.Windows.Foundation
{
    internal static class IReferenceArrayIIDs
    {
        internal static readonly Guid IReferenceArrayOfInt32_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xA5, 0x80, 0xD0, 0xA6, 0x87, 0xB0, 0xC2, 0x5B, 0x9A, 0x9F, 0x5C, 0xD6, 0x87, 0xB4, 0xD1, 0xF7 }));
        internal static readonly Guid IReferenceArrayOfString_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x8E, 0x68, 0x85, 0x03, 0xC7, 0xE3, 0x5E, 0x5C, 0xA3, 0x89, 0x55, 0x24, 0xED, 0xE3, 0x49, 0xF1 }));
        internal static readonly Guid IReferenceArrayOfByte_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x83, 0x26, 0xF2, 0x2A, 0x34, 0x37, 0xD0, 0x56, 0xA6, 0x0E, 0x68, 0x8C, 0xC8, 0x5D, 0x16, 0x19 }));
        internal static readonly Guid IReferenceArrayOfInt16_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xD7, 0x8F, 0x2F, 0x91, 0xC0, 0xAD, 0x60, 0x5D, 0xA8, 0x96, 0x7E, 0xD7, 0x60, 0x89, 0xCC, 0x5B }));
        internal static readonly Guid IReferenceArrayOfUInt16_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xDD, 0xA2, 0x24, 0x66, 0xF7, 0x83, 0x9C, 0x51, 0x9D, 0x55, 0xBB, 0x1F, 0x65, 0x60, 0x45, 0x6B }));
        internal static readonly Guid IReferenceArrayOfUInt32_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x68, 0x4B, 0x37, 0x97, 0x87, 0xEB, 0xCC, 0x56, 0xB1, 0x8E, 0x27, 0xEF, 0x0F, 0x9C, 0xFC, 0x0C }));
        internal static readonly Guid IReferenceArrayOfInt64_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x71, 0x32, 0x33, 0x6E, 0x2A, 0x2E, 0x55, 0x59, 0x87, 0x90, 0x83, 0x6C, 0x76, 0xEE, 0x53, 0xB6 }));
        internal static readonly Guid IReferenceArrayOfUInt64_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x34, 0x04, 0xB6, 0x38, 0x7C, 0xD6, 0x3E, 0x52, 0x9D, 0x0E, 0x24, 0xD6, 0x43, 0x41, 0x10, 0x73 }));
        internal static readonly Guid IReferenceArrayOfSingle_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x83, 0xEA, 0xB1, 0x6A, 0x41, 0xCB, 0x99, 0x5F, 0x92, 0xCC, 0x23, 0xBD, 0x43, 0x36, 0xA1, 0xFB }));
        internal static readonly Guid IReferenceArrayOfDouble_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x53, 0xF2, 0x01, 0xD3, 0xA3, 0xE0, 0x2B, 0x5D, 0x9A, 0x41, 0xA4, 0xD6, 0x2B, 0xEC, 0x46, 0x23 }));
        internal static readonly Guid IReferenceArrayOfChar_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xAB, 0x5A, 0x09, 0xA4, 0x7D, 0xEB, 0x82, 0x57, 0x8F, 0xAD, 0x16, 0x09, 0xDE, 0xA2, 0x49, 0xAD }));
        internal static readonly Guid IReferenceArrayOfBoolean_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x66, 0x26, 0xE7, 0xE8, 0xCC, 0x48, 0x3F, 0x59, 0xBA, 0x85, 0x26, 0x63, 0x49, 0x69, 0x56, 0xE3 }));
        internal static readonly Guid IReferenceArrayOfGuid_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x38, 0x98, 0xCF, 0xEE, 0xC2, 0xC1, 0x4A, 0x5B, 0x97, 0x6F, 0xCE, 0xC2, 0x61, 0xAE, 0x1D, 0x55 }));
        internal static readonly Guid IReferenceArrayOfDateTimeOffset_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x94, 0x95, 0x8E, 0x1B, 0x8E, 0x58, 0x07, 0x5A, 0x9E, 0x65, 0x07, 0x31, 0xA4, 0xC9, 0xA2, 0xDB }));
        internal static readonly Guid IReferenceArrayOfTimeSpan_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x7D, 0x19, 0x73, 0xAD, 0xFA, 0x2C, 0xA6, 0x57, 0x89, 0x93, 0x9F, 0xAC, 0x40, 0xFE, 0xB7, 0x91 }));
        internal static readonly Guid IReferenceArrayOfObject_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x4F, 0xA8, 0xD7, 0x9C, 0x80, 0x0C, 0xC5, 0x59, 0xB4, 0x4E, 0x97, 0x78, 0x41, 0xBB, 0x43, 0xD9 }));
        internal static readonly Guid IReferenceArrayOfType_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xA7, 0x57, 0x84, 0xDA, 0xEB, 0xC2, 0xA1, 0x5D, 0x80, 0xBE, 0x71, 0x32, 0xA2, 0xE1, 0xBF, 0xA4 }));
        internal static readonly Guid IReferenceArrayOfMatrix3x2_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xff, 0xd9, 0x25, 0xa5, 0x9b, 0xc0, 0x1a, 0x50, 0xa7, 0x85, 0x4d, 0x1e, 0xd9, 0xe1, 0x02, 0xb8 }));
        internal static readonly Guid IReferenceArrayOfMatrix4x4_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x15, 0x5a, 0x0d, 0xfc, 0x9d, 0x8f, 0x8f, 0x5e, 0x88, 0x28, 0xae, 0xf2, 0xc2, 0xe2, 0x5b, 0xad }));
        internal static readonly Guid IReferenceArrayOfPlane_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x7d, 0x7f, 0xcf, 0xf9, 0x59, 0x54, 0x98, 0x5f, 0x91, 0xb9, 0xf2, 0x63, 0x2a, 0x9e, 0xc2, 0x98 }));
        internal static readonly Guid IReferenceArrayOfQuaternion_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xbe, 0x76, 0xba, 0xe9, 0x31, 0x2c, 0x1d, 0x5e, 0x98, 0xa4, 0xeb, 0xdb, 0x62, 0x5a, 0xee, 0x93 }));
        internal static readonly Guid IReferenceArrayOfVector2_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x78, 0x21, 0xdf, 0x29, 0xdb, 0xff, 0x3e, 0x56, 0x88, 0xdb, 0x38, 0x69, 0xa0, 0x07, 0x30, 0x5e }));
        internal static readonly Guid IReferenceArrayOfVector3_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xfa, 0x35, 0x1a, 0xaa, 0x4e, 0x0b, 0x48, 0x52, 0xbd, 0x79, 0xff, 0xd4, 0x7c, 0xfe, 0x40, 0x27 }));
        internal static readonly Guid IReferenceArrayOfVector4_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x50, 0x72, 0x75, 0x68, 0x49, 0x58, 0x72, 0x57, 0x90, 0xe3, 0xaa, 0xdb, 0x4c, 0x97, 0x0b, 0xff }));
        internal static readonly Guid IReferenceArrayOfException_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xcc, 0xe4, 0x1a, 0x40, 0xb9, 0x4a, 0x8f, 0x5a, 0xb9, 0x93, 0xe3, 0x27, 0x90, 0x0c, 0x36, 0x4d }));
    }

    internal static class BoxedArrayIReferenceArrayImpl<T>
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;
        private static readonly IReferenceArray_Delegates.get_Value_0 DelegateCache;

        static unsafe BoxedArrayIReferenceArrayImpl()
        {
            DelegateCache = new IReferenceArray_Delegates.get_Value_0(Do_Abi_get_Value_0);

            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(BoxedArrayIReferenceArrayImpl<T>), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr));
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((IntPtr*)AbiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(DelegateCache);
        }

        private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, int* ____return_value__Size, IntPtr* __return_value__)
        {
            T[] ____return_value__ = default;

            *__return_value__ = default;
            *____return_value__Size = default;

            try
            {
                ____return_value__ = (T[])global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                (*____return_value__Size, *__return_value__) = Marshaler<T>.FromManagedArray(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    [Guid("61C17707-2D65-11E0-9AE8-D48564015472")]
    internal sealed class IReferenceArray<T> : global::Windows.Foundation.IReferenceArray<T>
    {
        public static IObjectReference CreateMarshaler(object value)
        {
            return value is null ? null : ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(value, PIID);
        }

        public static ObjectReferenceValue CreateMarshaler2(object value) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(value, PIID);

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static object FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            var wrapper = new IReferenceArray<T>(ObjectReference<IUnknownVftbl>.FromAbi(ptr));
            return wrapper.Value;
        }

        public static unsafe object GetValue(IInspectable inspectable)
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref PIID, out referenceArrayPtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return Marshaler<T>.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                Marshaler<T>.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }

        public static unsafe void CopyManaged(object o, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(o).Detach();
        }

        public static IntPtr FromManaged(object value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler2(value).Detach();
        }

        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { MarshalInspectable<object>.DisposeAbi(abi); }

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IReferenceArray<T>));

        public static Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(IReferenceArray<T>));

        private readonly ObjectReference<IUnknownVftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;

        public IReferenceArray(ObjectReference<IUnknownVftbl> obj)
        {
            _obj = obj;
        }

        public unsafe T[] Value
        {
            get
            {
                int __retval_length = default;
                IntPtr __retval_data = default;
                try
                {
                    ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval_length, &__retval_data));
                    return Marshaler<T>.FromAbiArray((__retval_length, __retval_data));
                }
                finally
                {
                    Marshaler<T>.DisposeAbiArray((__retval_length, __retval_data));
                }
            }
        }
    }

    internal static class IReferenceArray_Delegates
    {
        public unsafe delegate int get_Value_0(IntPtr thisPtr, int* ____return_value__Size, IntPtr* __return_value__);
    }
}
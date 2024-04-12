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
        internal static readonly Guid IReferenceArrayOfInt32_IID = new(0xA6D080A5, 0xB087, 0x5BC2, 0x9A, 0x9F, 0x5C, 0xD6, 0x87, 0xB4, 0xD1, 0xF7);
        internal static readonly Guid IReferenceArrayOfString_IID = new(0x0385688E, 0xE3C7, 0x5C5E, 0xA3, 0x89, 0x55, 0x24, 0xED, 0xE3, 0x49, 0xF1);
        internal static readonly Guid IReferenceArrayOfByte_IID = new(0x2AF22683, 0x3734, 0x56D0, 0xA6, 0x0E, 0x68, 0x8C, 0xC8, 0x5D, 0x16, 0x19);
        internal static readonly Guid IReferenceArrayOfInt16_IID = new(0x912F8FD7, 0xADC0, 0x5D60, 0xA8, 0x96, 0x7E, 0xD7, 0x60, 0x89, 0xCC, 0x5B);
        internal static readonly Guid IReferenceArrayOfUInt16_IID = new(0x6624A2DD, 0x83F7, 0x519C, 0x9D, 0x55, 0xBB, 0x1F, 0x65, 0x60, 0x45, 0x6B);
        internal static readonly Guid IReferenceArrayOfUInt32_IID = new(0x97374B68, 0xEB87, 0x56CC, 0xB1, 0x8E, 0x27, 0xEF, 0x0F, 0x9C, 0xFC, 0x0C);
        internal static readonly Guid IReferenceArrayOfInt64_IID = new(0x6E333271, 0x2E2A, 0x5955, 0x87, 0x90, 0x83, 0x6C, 0x76, 0xEE, 0x53, 0xB6);
        internal static readonly Guid IReferenceArrayOfUInt64_IID = new(0x38B60434, 0xD67C, 0x523E, 0x9D, 0x0E, 0x24, 0xD6, 0x43, 0x41, 0x10, 0x73);
        internal static readonly Guid IReferenceArrayOfSingle_IID = new(0x6AB1EA83, 0xCB41, 0x5F99, 0x92, 0xCC, 0x23, 0xBD, 0x43, 0x36, 0xA1, 0xFB);
        internal static readonly Guid IReferenceArrayOfDouble_IID = new(0xD301F253, 0xE0A3, 0x5D2B, 0x9A, 0x41, 0xA4, 0xD6, 0x2B, 0xEC, 0x46, 0x23);
        internal static readonly Guid IReferenceArrayOfChar_IID = new(0xA4095AAB, 0xEB7D, 0x5782, 0x8F, 0xAD, 0x16, 0x09, 0xDE, 0xA2, 0x49, 0xAD);
        internal static readonly Guid IReferenceArrayOfBoolean_IID = new(0xE8E72666, 0x48CC, 0x593F, 0xBA, 0x85, 0x26, 0x63, 0x49, 0x69, 0x56, 0xE3);
        internal static readonly Guid IReferenceArrayOfGuid_IID = new(0xEECF9838, 0xC1C2, 0x5B4A, 0x97, 0x6F, 0xCE, 0xC2, 0x61, 0xAE, 0x1D, 0x55);
        internal static readonly Guid IReferenceArrayOfDateTimeOffset_IID = new(0x1B8E9594, 0x588E, 0x5A07, 0x9E, 0x65, 0x07, 0x31, 0xA4, 0xC9, 0xA2, 0xDB);
        internal static readonly Guid IReferenceArrayOfTimeSpan_IID = new(0xAD73197D, 0x2CFA, 0x57A6, 0x89, 0x93, 0x9F, 0xAC, 0x40, 0xFE, 0xB7, 0x91);
        internal static readonly Guid IReferenceArrayOfObject_IID = new(0x9CD7A84F, 0x0C80, 0x59C5, 0xB4, 0x4E, 0x97, 0x78, 0x41, 0xBB, 0x43, 0xD9);
        internal static readonly Guid IReferenceArrayOfType_IID = new(0xDA8457A7, 0xC2EB, 0x5DA1, 0x80, 0xBE, 0x71, 0x32, 0xA2, 0xE1, 0xBF, 0xA4);
        internal static readonly Guid IReferenceArrayOfMatrix3x2_IID = new(0xa525d9ff, 0xc09b, 0x501a, 0xa7, 0x85, 0x4d, 0x1e, 0xd9, 0xe1, 0x02, 0xb8);
        internal static readonly Guid IReferenceArrayOfMatrix4x4_IID = new(0xfc0d5a15, 0x8f9d, 0x5e8f, 0x88, 0x28, 0xae, 0xf2, 0xc2, 0xe2, 0x5b, 0xad);
        internal static readonly Guid IReferenceArrayOfPlane_IID = new(0xf9cf7f7d, 0x5459, 0x5f98, 0x91, 0xb9, 0xf2, 0x63, 0x2a, 0x9e, 0xc2, 0x98);
        internal static readonly Guid IReferenceArrayOfQuaternion_IID = new(0xe9ba76be, 0x2c31, 0x5e1d, 0x98, 0xa4, 0xeb, 0xdb, 0x62, 0x5a, 0xee, 0x93);
        internal static readonly Guid IReferenceArrayOfVector2_IID = new(0x29df2178, 0xffdb, 0x563e, 0x88, 0xdb, 0x38, 0x69, 0xa0, 0x07, 0x30, 0x5e);
        internal static readonly Guid IReferenceArrayOfVector3_IID = new(0xaa1a35fa, 0x0b4e, 0x5248, 0xbd, 0x79, 0xff, 0xd4, 0x7c, 0xfe, 0x40, 0x27);
        internal static readonly Guid IReferenceArrayOfVector4_IID = new(0x68757250, 0x5849, 0x5772, 0x90, 0xe3, 0xaa, 0xdb, 0x4c, 0x97, 0x0b, 0xff);
        internal static readonly Guid IReferenceArrayOfException_IID = new(0x401ae4cc, 0x4ab9, 0x5a8f, 0xb9, 0x93, 0xe3, 0x27, 0x90, 0x0c, 0x36, 0x4d);
    }

    internal static class BoxedArrayIReferenceArrayImpl<T>
    {
        private static readonly IReferenceArray<T>.Vftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;
        static unsafe BoxedArrayIReferenceArrayImpl()
        {
            AbiToProjectionVftable = new IReferenceArray<T>.Vftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                get_Value_0 = Do_Abi_get_Value_0
            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(BoxedArrayIReferenceArrayImpl<T>), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
            Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
            nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Value_0);

            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, out int ____return_value__Size, out IntPtr __return_value__)
        {
            T[] ____return_value__ = default;

            __return_value__ = default;
            ____return_value__Size = default;

            try
            {
                ____return_value__ = (T[])global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                (____return_value__Size, __return_value__) = Marshaler<T>.FromManagedArray(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("61C17707-2D65-11E0-9AE8-D48564015472")]
    internal sealed class IReferenceArray<T> : global::Windows.Foundation.IReferenceArray<T>
    {
        public static IObjectReference CreateMarshaler(object value)
        {
            if (value is null)
            {
                return null;
            }
            return ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(value, PIID);
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
            var vftblT = new Vftbl(ptr);
            var wrapper = new IReferenceArray<T>(ObjectReference<Vftbl>.FromAbi(ptr, vftblT));
            return wrapper.Value;
        }

        public static object GetValue(IInspectable inspectable)
        {
            var array = new IReferenceArray<T>(inspectable.ObjRef);
            return array.Value;
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

        [Guid("61C17707-2D65-11E0-9AE8-D48564015472")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public IReferenceArray_Delegates.get_Value_0 get_Value_0;
            public static Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(IReferenceArray<T>));

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = *(void***)thisPtr;
                var vftbl = (IntPtr*)vftblPtr;
                IInspectableVftbl = *(IInspectable.Vftbl*)vftblPtr;
                get_Value_0 = Marshal.GetDelegateForFunctionPointer<IReferenceArray_Delegates.get_Value_0>(vftbl[6]);
            }
        }

        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IReferenceArray<T>(IObjectReference obj) => (obj != null) ? new IReferenceArray<T>(obj) : null;
        public static implicit operator IReferenceArray<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IReferenceArray<T>(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IReferenceArray(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IReferenceArray(ObjectReference<Vftbl> obj)
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
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Value_0(ThisPtr, out __retval_length, out __retval_data));
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
        public unsafe delegate int get_Value_0(IntPtr thisPtr, out int ____return_value__Size, out IntPtr __return_value__);
    }
}


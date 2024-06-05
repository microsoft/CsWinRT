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


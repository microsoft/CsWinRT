using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.Windows.Foundation
{
    using System;

    internal static class BoxedValueIReferenceImpl<T>
    {
        private static Nullable<T>.Vftbl AbiToProjectionVftable;
        public static IntPtr AbiToProjectionVftablePtr;

        static unsafe BoxedValueIReferenceImpl()
        {
            AbiToProjectionVftable = new Nullable<T>.Vftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                get_Value_0 = global::System.Delegate.CreateDelegate(Nullable<T>.Vftbl.get_Value_0_Type, typeof(BoxedValueIReferenceImpl<T>).GetMethod("Do_Abi_get_Value_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType))
            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(BoxedValueIReferenceImpl<T>), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
            Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
            nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Value_0);

            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        private static unsafe int Do_Abi_get_Value_0<TAbi>(void* thisPtr, out TAbi __return_value__)
        {
            T ____return_value__ = default;

            __return_value__ = default;

            try
            {
                ____return_value__ = (T)global::WinRT.ComWrappersSupport.FindObject<object>(new IntPtr(thisPtr));
                __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }
}

namespace ABI.System
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("61C17706-2D65-11E0-9AE8-D48564015472")]
    public class Nullable<T>
    {
        public static IObjectReference CreateMarshaler(object value)
        {
            if (value is null)
            {
                return null;
            }
            return ComWrappersSupport.CreateCCWForObject(value).As(PIID);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static object FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(ptr);
            var wrapper = new Nullable<T>(ObjectReference<Vftbl>.FromAbi(ptr, vftblT));
            return wrapper.Value;
        }

        public static unsafe void CopyManaged(object o, IntPtr dest)
        {
            using var objRef = CreateMarshaler(o);
            *(IntPtr*)dest.ToPointer() = objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static IntPtr FromManaged(object value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref abi); }

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(Nullable<T>));

        [Guid("61C17706-2D65-11E0-9AE8-D48564015472")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate get_Value_0;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(Nullable<T>));
            public static readonly global::System.Type get_Value_0_Type = Expression.GetDelegateType(new global::System.Type[] { typeof(void*), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                get_Value_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], get_Value_0_Type);
            }
        }

        public static Guid PIID = Vftbl.PIID;

        public static implicit operator Nullable<T>(IObjectReference obj) => (obj != null) ? new Nullable<T>(obj) : null;
        public static implicit operator Nullable<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new Nullable<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public Nullable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public Nullable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe T Value
        {
            get
            {
                var __params = new object[] { ThisPtr, null };
                try
                {
                    _obj.Vftbl.get_Value_0.DynamicInvokeAbi(__params);
                    return Marshaler<T>.FromAbi(__params[1]);
                }
                finally
                {
                    Marshaler<T>.DisposeAbi(__params[1]);
                }
            }
        }
    }
}

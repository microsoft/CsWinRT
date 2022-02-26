// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.Windows.Foundation
{
    using System;

    internal static class BoxedValueIReferenceImpl<T>
    {
        private static global::ABI.System.Nullable<T>.Vftbl AbiToProjectionVftable;
        public static IntPtr AbiToProjectionVftablePtr;

        static unsafe BoxedValueIReferenceImpl()
        {
            AbiToProjectionVftable = new global::ABI.System.Nullable<T>.Vftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                get_Value_0 = global::System.Delegate.CreateDelegate(global::ABI.System.Nullable<T>.Vftbl.get_Value_0_Type, typeof(BoxedValueIReferenceImpl<T>).GetMethod("Do_Abi_get_Value_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType))
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
#if EMBED
    internal
#else
    public
#endif
    class Nullable<T>
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
            var vftblT = new Vftbl(ptr);
            var wrapper = new Nullable<T>(ObjectReference<Vftbl>.FromAbi(ptr, vftblT));
            return wrapper.Value;
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
        public Nullable(IObjectReference obj) : this(obj.As<Vftbl>(Vftbl.PIID)) { }
        public Nullable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        internal static unsafe T GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            var __params = new object[] { IntPtr.Zero, null };
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref PIID, out nullablePtr));
                __params[0] = nullablePtr;
                Marshal.GetDelegateForFunctionPointer((*(IntPtr**)nullablePtr)[6], Vftbl.get_Value_0_Type).DynamicInvokeAbi(__params);
                return Marshaler<T>.FromAbi(__params[1]);
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__params[1]);
                Marshal.Release(nullablePtr);
            }
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

    // Used to handle boxing of strings and types where the C# compiler will de-duplicate them
    // causing for the same instance to be reused with multiple different box instances.
    // This is also used for delegates which are objects themselves in C# and are associated with
    // their own ptr and thereby can not be associated with the ptr for the box / nullable.
    internal sealed class Nullable
    {
        public Nullable(object boxedObject)
        {
            Value = boxedObject;
        }

        public object Value { get; }
    }

    [Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4")]
    internal static class Nullable_int
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i4)";
        internal static Guid IID = new(0x548cefbd, 0xbc8a, 0x5fa0, 0x8d, 0xf2, 0x95, 0x74, 0x40, 0xfc, 0x8b, 0xf4);

        [Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, int*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, int*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, int* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, int*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, int* __return_value__)
            {
                int ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (int)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static int GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                int __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("fd416dfb-2a07-52eb-aae3-dfce14116c05")]
    internal static class Nullable_string
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};string)";
        internal static Guid IID = new(0xfd416dfb, 0x2a07, 0x52eb, 0xaa, 0xe3, 0xdf, 0xce, 0x14, 0x11, 0x6c, 0x05);

        [Guid("fd416dfb-2a07-52eb-aae3-dfce14116c05")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, IntPtr* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, IntPtr* __return_value__)
            {
                string ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (string)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = MarshalString.FromManaged(____return_value__);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(MarshalString.FromAbi(__retval));
            }
            finally
            {
                MarshalString.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("e5198cc8-2873-55f5-b0a1-84ff9e4aad62")]
    internal static class Nullable_byte
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u1)";
        internal static Guid IID = new(0xe5198cc8, 0x2873, 0x55f5, 0xb0, 0xa1, 0x84, 0xff, 0x9e, 0x4a, 0xad, 0x62);

        [Guid("e5198cc8-2873-55f5-b0a1-84ff9e4aad62")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, byte*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, byte*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, byte* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, byte*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, byte* __return_value__)
            {
                byte ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (byte)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static byte GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                byte __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("95500129-fbf6-5afc-89df-70642d741990")]
    internal static class Nullable_sbyte
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i1)";
        internal static Guid IID = new(0x95500129, 0xfbf6, 0x5afc, 0x89, 0xdf, 0x70, 0x64, 0x2d, 0x74, 0x19, 0x90);

        [Guid("95500129-fbf6-5afc-89df-70642d741990")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, sbyte*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, sbyte*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, sbyte* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, sbyte*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, sbyte* __return_value__)
            {
                sbyte ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (sbyte)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static sbyte GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                sbyte __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, sbyte*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("6ec9e41b-6709-5647-9918-a1270110fc4e")]
    internal static class Nullable_short
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i2)";
        internal static Guid IID = new(0x6ec9e41b, 0x6709, 0x5647, 0x99, 0x18, 0xa1, 0x27, 0x01, 0x10, 0xfc, 0x4e);

        [Guid("6ec9e41b-6709-5647-9918-a1270110fc4e")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, short*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, short*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, short* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, short*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, short* __return_value__)
            {
                short ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (short)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static short GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                short __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, short*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("5ab7d2c3-6b62-5e71-a4b6-2d49c4f238fd")]
    internal static class Nullable_ushort
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u2)";
        internal static Guid IID = new(0x5ab7d2c3, 0x6b62, 0x5e71, 0xa4, 0xb6, 0x2d, 0x49, 0xc4, 0xf2, 0x38, 0xfd);

        [Guid("5ab7d2c3-6b62-5e71-a4b6-2d49c4f238fd")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, ushort*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, ushort*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, ushort* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, ushort*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, ushort* __return_value__)
            {
                ushort ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (ushort)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static ushort GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                ushort __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, ushort*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("513ef3af-e784-5325-a91e-97c2b8111cf3")]
    internal static class Nullable_uint
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u4)";
        internal static Guid IID = new(0x513ef3af, 0xe784, 0x5325, 0xa9, 0x1e, 0x97, 0xc2, 0xb8, 0x11, 0x1c, 0xf3);

        [Guid("513ef3af-e784-5325-a91e-97c2b8111cf3")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, uint*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, uint* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, uint*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, uint* __return_value__)
            {
                uint ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (uint)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static uint GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                uint __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("4dda9e24-e69f-5c6a-a0a6-93427365af2a")]
    internal static class Nullable_long
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i8)";
        internal static Guid IID = new(0x4dda9e24, 0xe69f, 0x5c6a, 0xa0, 0xa6, 0x93, 0x42, 0x73, 0x65, 0xaf, 0x2a);

        [Guid("4dda9e24-e69f-5c6a-a0a6-93427365af2a")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, long*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, long*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, long* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, long*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, long* __return_value__)
            {
                long ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (long)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static long GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                long __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, long*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("6755e376-53bb-568b-a11d-17239868309e")]
    internal static class Nullable_ulong
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u8)";
        internal static Guid IID = new(0x6755e376, 0x53bb, 0x568b, 0xa1, 0x1d, 0x17, 0x23, 0x98, 0x68, 0x30, 0x9e);

        [Guid("6755e376-53bb-568b-a11d-17239868309e")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, ulong*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, ulong*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, ulong* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, ulong*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, ulong* __return_value__)
            {
                ulong ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (ulong)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static ulong GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                ulong __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, ulong*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("719cc2ba-3e76-5def-9f1a-38d85a145ea8")]
    internal static class Nullable_float
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};f4)";
        internal static Guid IID = new(0x719cc2ba, 0x3e76, 0x5def, 0x9f, 0x1a, 0x38, 0xd8, 0x5a, 0x14, 0x5e, 0xa8);

        [Guid("719cc2ba-3e76-5def-9f1a-38d85a145ea8")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, float*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, float*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, float* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, float*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, float* __return_value__)
            {
                float ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (float)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static float GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                float __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, float*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("2f2d6c29-5473-5f3e-92e7-96572bb990e2")]
    internal static class Nullable_double
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};f8)";
        internal static Guid IID = new(0x2f2d6c29, 0x5473, 0x5f3e, 0x92, 0xe7, 0x96, 0x57, 0x2b, 0xb9, 0x90, 0xe2);

        [Guid("2f2d6c29-5473-5f3e-92e7-96572bb990e2")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, double*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, double*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, double* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, double*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, double* __return_value__)
            {
                double ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (double)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static double GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                double __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, double*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("fb393ef3-bbac-5bd5-9144-84f23576f415")]
    internal static class Nullable_char
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};c2)";
        internal static Guid IID = new(0xfb393ef3, 0xbbac, 0x5bd5, 0x91, 0x44, 0x84, 0xf2, 0x35, 0x76, 0xf4, 0x15);

        [Guid("fb393ef3-bbac-5bd5-9144-84f23576f415")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, char*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, char*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, char* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, char*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, char* __return_value__)
            {
                char ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (char)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static char GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                char __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, char*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("3c00fd60-2950-5939-a21a-2d12c5a01b8a")]
    internal static class Nullable_bool
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};b1)";
        internal static Guid IID = new(0x3c00fd60, 0x2950, 0x5939, 0xa2, 0x1a, 0x2d, 0x12, 0xc5, 0xa0, 0x1b, 0x8a);

        [Guid("3c00fd60-2950-5939-a21a-2d12c5a01b8a")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, bool*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, bool*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, bool* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, bool*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, bool* __return_value__)
            {
                bool ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (bool)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static bool GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                bool __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, bool*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("7d50f649-632c-51f9-849a-ee49428933ea")]
    internal static class Nullable_guid
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};g16)";
        internal static Guid IID = new(0x7d50f649, 0x632c, 0x51f9, 0x84, 0x9a, 0xee, 0x49, 0x42, 0x89, 0x33, 0xea);

        [Guid("7d50f649-632c-51f9-849a-ee49428933ea")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, Guid*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, Guid* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, Guid*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, Guid* __return_value__)
            {
                Guid ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (Guid)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static Guid GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                Guid __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Guid*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("5541d8a7-497c-5aa4-86fc-7713adbf2a2c")]
    internal static class Nullable_DateTimeOffset
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.DateTime;i8))";
        internal static Guid IID = new(0x5541d8a7, 0x497c, 0x5aa4, 0x86, 0xfc, 0x77, 0x13, 0xad, 0xbf, 0x2a, 0x2c);

        [Guid("5541d8a7-497c-5aa4-86fc-7713adbf2a2c")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, DateTimeOffset*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, DateTimeOffset*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, DateTimeOffset* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, DateTimeOffset*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, DateTimeOffset* __return_value__)
            {
                global::System.DateTimeOffset ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (global::System.DateTimeOffset)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = DateTimeOffset.FromManaged(____return_value__);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static global::System.DateTimeOffset GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            DateTimeOffset __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, DateTimeOffset*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return DateTimeOffset.FromAbi(__retval);
            }
            finally
            {
                DateTimeOffset.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("604d0c4c-91de-5c2a-935f-362f13eaf800")]
    internal static class Nullable_TimeSpan
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.TimeSpan;i8))";
        internal static Guid IID = new(0x604d0c4c, 0x91de, 0x5c2a, 0x93, 0x5f, 0x36, 0x2f, 0x13, 0xea, 0xf8, 0x00);

        [Guid("604d0c4c-91de-5c2a-935f-362f13eaf800")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, TimeSpan*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, TimeSpan*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, TimeSpan* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, TimeSpan*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, TimeSpan* __return_value__)
            {
                global::System.TimeSpan ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (global::System.TimeSpan)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = TimeSpan.FromManaged(____return_value__);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static global::System.TimeSpan GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            TimeSpan __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, TimeSpan*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return TimeSpan.FromAbi(__retval);
            }
            finally
            {
                TimeSpan.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("06dccc90-a058-5c88-87b7-6f3360a2fc16")]
    internal static class Nullable_Object
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};cinterface(IInspectable))";
        internal static Guid IID = new(0x06dccc90, 0xa058, 0x5c88, 0x87, 0xb7, 0x6f, 0x33, 0x60, 0xa2, 0xfc, 0x16);

        [Guid("06dccc90-a058-5c88-87b7-6f3360a2fc16")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, IntPtr* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, IntPtr* __return_value__)
            {
                object ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = MarshalInspectable<object>.FromManaged(____return_value__);
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

    [Guid("3830ad99-d8da-53f3-989b-fc92ad222778")]
    internal static class Nullable_Type
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4)))";
        internal static Guid IID = new(0x3830ad99, 0xd8da, 0x53f3, 0x98, 0x9b, 0xfc, 0x92, 0xad, 0x22, 0x27, 0x78);

        [Guid("3830ad99-d8da-53f3-989b-fc92ad222778")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Type*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, Type*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, Type* value);
            private static readonly GetValueDelegate delegateCache;
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _Get_Value_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0).ToPointer()
#else
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, Type*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, Type* __return_value__)
            {
                global::System.Type ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (global::System.Type)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = Type.FromManaged(____return_value__);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        unsafe internal static Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            Type __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Type*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(Type.FromAbi(__retval));
            }
            finally
            {
                Type.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("61C17706-2D65-11E0-9AE8-D48564015472")]
    internal static class Nullable_Delegate<T> where T : global::System.Delegate
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(Nullable<T>));

        [Guid("61C17706-2D65-11E0-9AE8-D48564015472")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private Nullable_Delegates.GetValueDelegate _Get_Value_0;

            public static Guid PIID = GuidGenerator.CreateIID(typeof(Nullable<T>));

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    _Get_Value_0 = Do_Abi_get_Value_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable._Get_Value_0);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, IntPtr* __return_value__)
            {
                T ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<T>(thisPtr);
                    *__return_value__ = (IntPtr)Marshaler<T>.FromManaged(____return_value__);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        public static Guid PIID = Vftbl.PIID;

        unsafe internal static Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref PIID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(Marshaler<T>.FromAbi(__retval));
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    internal static class Nullable_Delegates
    {
        public unsafe delegate int GetValueDelegate(IntPtr thisPtr, IntPtr* value);
    }
}

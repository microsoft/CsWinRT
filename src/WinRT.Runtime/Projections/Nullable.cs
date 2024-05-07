// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
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

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "ABI types used with MakeGenericType are not reflected on.")]
#endif
        static unsafe BoxedValueIReferenceImpl()
        {
            AbiToProjectionVftable = new global::ABI.System.Nullable<T>.Vftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                get_Value_0 = GetValueDelegateForAbi(out IntPtr nativePtr)
            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(BoxedValueIReferenceImpl<T>), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
            *(global::WinRT.IInspectable.Vftbl*)nativeVftbl = AbiToProjectionVftable.IInspectableVftbl;
            nativeVftbl[6] = nativePtr;

            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        // This method is only for all blittable types. Note: this method  could be an overload of
        // the generic one below, but using a different one to simplify the reflection lookup in the
        // fallback case below (so we can use GetMethod and find just the single match we need).
        private static unsafe int Do_Abi_get_Value_0_Blittable(void* thisPtr, void* result)
        {
            if (result is null)
            {
                // Immediately return E_POINTER if the target is null
                return unchecked((int)0x80004003);
            }

            try
            {
                T unboxedValue = (T)global::WinRT.ComWrappersSupport.FindObject<object>(new IntPtr(thisPtr));

                Unsafe.WriteUnaligned(result, unboxedValue);

                return 0;
            }
            catch (global::System.Exception __exception__)
            {
                Unsafe.WriteUnaligned<T>(result, default);

                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
        }

        private static unsafe int Do_Abi_get_Value_0_DateTimeOffset(void* thisPtr, DateTimeOffset* result)
        {
            if (result is null)
            {
                return unchecked((int)0x80004003);
            }

            try
            {
                T unboxedValue = (T)global::WinRT.ComWrappersSupport.FindObject<object>(new IntPtr(thisPtr));

                Unsafe.WriteUnaligned(result, DateTimeOffset.FromManaged(Unsafe.As<T, global::System.DateTimeOffset>(ref unboxedValue)));

                return 0;
            }
            catch (global::System.Exception __exception__)
            {
                Unsafe.WriteUnaligned<T>(result, default);

                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
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

        internal static unsafe Delegate GetValueDelegateForAbi(out IntPtr nativePtr)
        {
            if (typeof(T) == typeof(int) ||
                typeof(T) == typeof(byte) ||
                typeof(T) == typeof(bool) ||
                typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(char) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(long) ||
                typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(float) ||
                typeof(T) == typeof(double) ||
                typeof(T) == typeof(Guid) ||
                typeof(T) == typeof(global::System.TimeSpan) ||
                typeof(T) == typeof(global::Windows.Foundation.Point) ||
                typeof(T) == typeof(global::Windows.Foundation.Rect) ||
                typeof(T) == typeof(global::Windows.Foundation.Size) ||
                typeof(T) == typeof(global::System.Numerics.Matrix3x2) ||
                typeof(T) == typeof(global::System.Numerics.Matrix4x4) ||
                typeof(T) == typeof(global::System.Numerics.Plane) ||
                typeof(T) == typeof(global::System.Numerics.Quaternion) ||
                typeof(T) == typeof(global::System.Numerics.Vector2) ||
                typeof(T) == typeof(global::System.Numerics.Vector3) ||
                typeof(T) == typeof(global::System.Numerics.Vector4) ||
                (typeof(T).IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(int)) ||
                (typeof(T).IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(uint)))
            {
                Nullable_Delegates.GetValueDelegateAbi stub = new(Do_Abi_get_Value_0_Blittable);

                nativePtr = Marshal.GetFunctionPointerForDelegate(stub);

                return stub;
            }

            if (typeof(T) == typeof(global::System.DateTimeOffset))
            {
                Nullable_Delegates.GetValueDelegateAbiDateTimeOffset stub = new(Do_Abi_get_Value_0_DateTimeOffset);

                nativePtr = Marshal.GetFunctionPointerForDelegate(stub);

                return stub;
            }

#if NET
            // Only when not on NAOT, use LINQ expressions to get the delegate type.
            // This is not safe on AOT, so in that case we just can't get a delegate.
            if (RuntimeFeature.IsDynamicCodeCompiled)
#endif
            {
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                Nullable<T>.Vftbl.get_Value_0_Type ??= Projections.GetAbiDelegateType(typeof(void*), Marshaler<T>.AbiType.MakeByRefType(), typeof(int));

                Delegate stub = Delegate.CreateDelegate(
                    Nullable<T>.Vftbl.get_Value_0_Type,
                    typeof(BoxedValueIReferenceImpl<T>).GetMethod(nameof(Do_Abi_get_Value_0), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(Marshaler<T>.AbiType));

                nativePtr = Marshal.GetFunctionPointerForDelegate(stub);

                return stub;
#pragma warning restore IL3050
            }

            throw new NotSupportedException($"Failed to get the marshalling delegate for BoxedValueIReferenceImpl`1 with type '{typeof(T)}'.");
        }
    }

    internal static class BoxedValueIReferenceImpl<T, TAbi> where TAbi : unmanaged
    {
        public static IntPtr AbiToProjectionVftablePtr;
        private readonly static Nullable_Delegates.GetValueDelegateAbi GetValue;

        static unsafe BoxedValueIReferenceImpl()
        {
            if (typeof(T) == typeof(TAbi))
            {
                GetValue = Do_Abi_get_Value_0_Blittable;
            }
            else
            {
                GetValue = Do_Abi_get_Value_0;
            }

            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(BoxedValueIReferenceImpl<T, TAbi>), sizeof(IInspectable.Vftbl) + sizeof(IntPtr));
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((IntPtr*)AbiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(GetValue);
        }

        private static unsafe int Do_Abi_get_Value_0(void* thisPtr, void* __return_value__)
        {
            *(TAbi*)__return_value__ = default;

            try
            {
                T ____return_value__ = (T)global::WinRT.ComWrappersSupport.FindObject<object>(new IntPtr(thisPtr));
                *(TAbi*)__return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_get_Value_0_Blittable(void* thisPtr, void* __return_value__)
        {
            *(TAbi*)__return_value__ = default;

            try
            {
                *(TAbi*)__return_value__ = (TAbi)global::WinRT.ComWrappersSupport.FindObject<object>(new IntPtr(thisPtr));
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
#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
#endif
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
            var wrapper = new Nullable<T>(ObjectReference<Vftbl>.FromAbi(ptr, vftblT, PIID));
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

        public static string GetGuidSignature() => CreateGuidSignature();

        private static string CreateGuidSignature()
        {
            if (typeof(T) == typeof(int)) return Nullable_int.GetGuidSignature();
            if (typeof(T) == typeof(byte)) return Nullable_byte.GetGuidSignature();
            if (typeof(T) == typeof(bool)) return Nullable_bool.GetGuidSignature();
            if (typeof(T) == typeof(sbyte)) return Nullable_sbyte.GetGuidSignature();
            if (typeof(T) == typeof(short)) return Nullable_short.GetGuidSignature();
            if (typeof(T) == typeof(ushort)) return Nullable_ushort.GetGuidSignature();
            if (typeof(T) == typeof(char)) return Nullable_char.GetGuidSignature();
            if (typeof(T) == typeof(uint)) return Nullable_uint.GetGuidSignature();
            if (typeof(T) == typeof(long)) return Nullable_long.GetGuidSignature();
            if (typeof(T) == typeof(ulong)) return Nullable_ulong.GetGuidSignature();
            if (typeof(T) == typeof(float)) return Nullable_float.GetGuidSignature();
            if (typeof(T) == typeof(double)) return Nullable_double.GetGuidSignature();
            if (typeof(T) == typeof(Guid)) return Nullable_guid.GetGuidSignature();
            if (typeof(T) == typeof(global::System.Type)) return Nullable_Type.GetGuidSignature();
            if (typeof(T) == typeof(global::System.TimeSpan)) return Nullable_TimeSpan.GetGuidSignature();
            if (typeof(T) == typeof(global::System.DateTimeOffset)) return Nullable_DateTimeOffset.GetGuidSignature();
            if (typeof(T) == typeof(global::Windows.Foundation.Point)) return IReferenceSignatures.Point;
            if (typeof(T) == typeof(global::Windows.Foundation.Size)) return IReferenceSignatures.Size;
            if (typeof(T) == typeof(global::Windows.Foundation.Rect)) return IReferenceSignatures.Rect;
            if (typeof(T) == typeof(global::System.Numerics.Matrix3x2)) return IReferenceSignatures.Matrix3x2;
            if (typeof(T) == typeof(global::System.Numerics.Matrix4x4)) return IReferenceSignatures.Matrix4x4;
            if (typeof(T) == typeof(global::System.Numerics.Plane)) return IReferenceSignatures.Plane;
            if (typeof(T) == typeof(global::System.Numerics.Quaternion)) return IReferenceSignatures.Quaternion;
            if (typeof(T) == typeof(global::System.Numerics.Vector2)) return IReferenceSignatures.Vector2;
            if (typeof(T) == typeof(global::System.Numerics.Vector3)) return IReferenceSignatures.Vector3;
            if (typeof(T) == typeof(global::System.Numerics.Vector4)) return IReferenceSignatures.Vector4;

            return GuidGenerator.GetSignature(typeof(Nullable<T>));
        }

        [Guid("61C17706-2D65-11E0-9AE8-D48564015472")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate get_Value_0;
            public static readonly Guid PIID = Nullable<T>.PIID;
            public static global::System.Type get_Value_0_Type;

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = *(void***)thisPtr;
                var vftbl = (IntPtr*)vftblPtr;
                IInspectableVftbl = *(IInspectable.Vftbl*)vftblPtr;
                get_Value_0 = GetValueDelegateForFunctionPointer(vftbl[6]);
            }

            internal static Delegate GetValueDelegateForFunctionPointer(IntPtr ptr)
            {
                if (typeof(T) == typeof(int) ||
                    typeof(T) == typeof(byte) ||
                    typeof(T) == typeof(bool) ||
                    typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(ushort) ||
                    typeof(T) == typeof(char) ||
                    typeof(T) == typeof(uint) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(ulong) ||
                    typeof(T) == typeof(float) ||
                    typeof(T) == typeof(double) ||
                    typeof(T) == typeof(Guid) ||
                    typeof(T) == typeof(global::System.TimeSpan) ||
                    typeof(T) == typeof(global::Windows.Foundation.Point) ||
                    typeof(T) == typeof(global::Windows.Foundation.Rect) ||
                    typeof(T) == typeof(global::Windows.Foundation.Size) ||
                    typeof(T) == typeof(global::System.Numerics.Matrix3x2) ||
                    typeof(T) == typeof(global::System.Numerics.Matrix4x4) ||
                    typeof(T) == typeof(global::System.Numerics.Plane) ||
                    typeof(T) == typeof(global::System.Numerics.Quaternion) ||
                    typeof(T) == typeof(global::System.Numerics.Vector2) ||
                    typeof(T) == typeof(global::System.Numerics.Vector3) ||
                    typeof(T) == typeof(global::System.Numerics.Vector4) ||
                    (typeof(T).IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(int)) ||
                    (typeof(T).IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(uint)))
                {
                    return Marshal.GetDelegateForFunctionPointer<Nullable_Delegates.GetValueDelegateAbi>(ptr);
                }

                if (typeof(T) == typeof(DateTimeOffset))
                {
                    return Marshal.GetDelegateForFunctionPointer<Nullable_Delegates.GetValueDelegateAbiDateTimeOffset>(ptr);
                }

#if NET
                // Only when not on NAOT, use LINQ expressions to get the delegate type.
                // This is not safe on AOT, so in that case we just can't get a delegate.
                if (RuntimeFeature.IsDynamicCodeCompiled)
#endif
                {
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                    get_Value_0_Type ??= Projections.GetAbiDelegateType(typeof(void*), Marshaler<T>.AbiType.MakeByRefType(), typeof(int));

                    return Marshal.GetDelegateForFunctionPointer(ptr, get_Value_0_Type);
#pragma warning restore IL3050
                }

                throw new NotSupportedException($"Failed to get the marshalling delegate for Nullable`1 with type '{typeof(T)}'.");
            }
        }

        public static readonly Guid PIID = NullableType.GetIID<T>();

#if !NET
        public static implicit operator Nullable<T>(IObjectReference obj) => (obj != null) ? new Nullable<T>(obj) : null;
        public static implicit operator Nullable<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new Nullable<T>(obj) : null;
#endif
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;

        public Nullable(IObjectReference obj) : this(obj.As<Vftbl>(Vftbl.PIID)) { }
        public Nullable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public static unsafe T GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in PIID), out nullablePtr));

                Delegate marshallingDelegate = Vftbl.GetValueDelegateForFunctionPointer((*(IntPtr**)nullablePtr)[6]);

                // No need to use GC.KeepAlive here because 'nullablePtr' is already keeping
                // the instance alive through the call. It's only needed in 'Value' below,
                // because 'ThisPtr' is being used without anyone keeping 'this' alive too.
                return GetValueFromAbi(nullablePtr, marshallingDelegate);
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }

        public T Value
        {
            get
            {
                T result = GetValueFromAbi(ThisPtr, _obj.Vftbl.get_Value_0);

                GC.KeepAlive(this);

                return result;
            }
        }

        /// <summary>
        /// Shared marshalling stub to get the underlying nullable value from a given <c>IReference`1</c> native instance.
        /// </summary>
        /// <param name="thisPtr">The <c>IReference`1</c> native instance to get the value from.</param>
        /// <param name="marshallingDelegate">The marshalling delegate to get the value from the ABI.</param>
        /// <returns>The marshalled <typeparamref name="T"/> value retrieved from <paramref name="thisPtr"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown if no marshalling code for <typeparamref name="T"/> is available.</exception>
        private static unsafe T GetValueFromAbi(IntPtr thisPtr, Delegate marshallingDelegate)
        {
            if (marshallingDelegate.GetType() == typeof(Nullable_Delegates.GetValueDelegateAbi))
            {
                T result;

#pragma warning disable CS8500 // We know that T is unmanaged
                Marshal.ThrowExceptionForHR(((Nullable_Delegates.GetValueDelegateAbi)marshallingDelegate)((void*)thisPtr, &result));
#pragma warning restore CS8500

                return result;
            }

            if (marshallingDelegate.GetType() == typeof(Nullable_Delegates.GetValueDelegateAbiDateTimeOffset))
            {
                DateTimeOffset result;

                Marshal.ThrowExceptionForHR(((Nullable_Delegates.GetValueDelegateAbiDateTimeOffset)marshallingDelegate)((void*)thisPtr, &result));

                global::System.DateTimeOffset managed = DateTimeOffset.FromAbi(result);

                return Unsafe.As<global::System.DateTimeOffset, T>(ref managed);
            }

#if NET
            if (RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                var __params = new object[] { thisPtr, null };
                try
                {
                    marshallingDelegate.DynamicInvokeAbi(__params);
                    return Marshaler<T>.FromAbi(__params[1]);
                }
                finally
                {
                    Marshaler<T>.DisposeAbi(__params[1]);
                }
            }

            throw new NotSupportedException($"Cannot retrieve the value for the current Nullable`1 instance with type '{typeof(T)}'.");
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
        internal static readonly Guid IID = new(0x548cefbd, 0xbc8a, 0x5fa0, 0x8d, 0xf2, 0x95, 0x74, 0x40, 0xfc, 0x8b, 0xf4);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("fd416dfb-2a07-52eb-aae3-dfce14116c05")]
    internal static class Nullable_string
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};string)";
        internal static readonly Guid IID = new(0xfd416dfb, 0x2a07, 0x52eb, 0xaa, 0xe3, 0xdf, 0xce, 0x14, 0x11, 0x6c, 0x05);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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

        public static unsafe Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
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
        internal static readonly Guid IID = new(0xe5198cc8, 0x2873, 0x55f5, 0xb0, 0xa1, 0x84, 0xff, 0x9e, 0x4a, 0xad, 0x62);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("95500129-fbf6-5afc-89df-70642d741990")]
    internal static class Nullable_sbyte
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i1)";
        internal static readonly Guid IID = new(0x95500129, 0xfbf6, 0x5afc, 0x89, 0xdf, 0x70, 0x64, 0x2d, 0x74, 0x19, 0x90);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("6ec9e41b-6709-5647-9918-a1270110fc4e")]
    internal static class Nullable_short
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i2)";
        internal static readonly Guid IID = new(0x6ec9e41b, 0x6709, 0x5647, 0x99, 0x18, 0xa1, 0x27, 0x01, 0x10, 0xfc, 0x4e);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("5ab7d2c3-6b62-5e71-a4b6-2d49c4f238fd")]
    internal static class Nullable_ushort
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u2)";
        internal static readonly Guid IID = new(0x5ab7d2c3, 0x6b62, 0x5e71, 0xa4, 0xb6, 0x2d, 0x49, 0xc4, 0xf2, 0x38, 0xfd);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("513ef3af-e784-5325-a91e-97c2b8111cf3")]
    internal static class Nullable_uint
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u4)";
        internal static readonly Guid IID = new(0x513ef3af, 0xe784, 0x5325, 0xa9, 0x1e, 0x97, 0xc2, 0xb8, 0x11, 0x1c, 0xf3);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("4dda9e24-e69f-5c6a-a0a6-93427365af2a")]
    internal static class Nullable_long
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i8)";
        internal static readonly Guid IID = new(0x4dda9e24, 0xe69f, 0x5c6a, 0xa0, 0xa6, 0x93, 0x42, 0x73, 0x65, 0xaf, 0x2a);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("6755e376-53bb-568b-a11d-17239868309e")]
    internal static class Nullable_ulong
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u8)";
        internal static readonly Guid IID = new(0x6755e376, 0x53bb, 0x568b, 0xa1, 0x1d, 0x17, 0x23, 0x98, 0x68, 0x30, 0x9e);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("719cc2ba-3e76-5def-9f1a-38d85a145ea8")]
    internal static class Nullable_float
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};f4)";
        internal static readonly Guid IID = new(0x719cc2ba, 0x3e76, 0x5def, 0x9f, 0x1a, 0x38, 0xd8, 0x5a, 0x14, 0x5e, 0xa8);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("2f2d6c29-5473-5f3e-92e7-96572bb990e2")]
    internal static class Nullable_double
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};f8)";
        internal static readonly Guid IID = new(0x2f2d6c29, 0x5473, 0x5f3e, 0x92, 0xe7, 0x96, 0x57, 0x2b, 0xb9, 0x90, 0xe2);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("fb393ef3-bbac-5bd5-9144-84f23576f415")]
    internal static class Nullable_char
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};c2)";
        internal static readonly Guid IID = new(0xfb393ef3, 0xbbac, 0x5bd5, 0x91, 0x44, 0x84, 0xf2, 0x35, 0x76, 0xf4, 0x15);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("3c00fd60-2950-5939-a21a-2d12c5a01b8a")]
    internal static class Nullable_bool
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};b1)";
        internal static readonly Guid IID = new(0x3c00fd60, 0x2950, 0x5939, 0xa2, 0x1a, 0x2d, 0x12, 0xc5, 0xa0, 0x1b, 0x8a);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("7d50f649-632c-51f9-849a-ee49428933ea")]
    internal static class Nullable_guid
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};g16)";
        internal static readonly Guid IID = new(0x7d50f649, 0x632c, 0x51f9, 0x84, 0x9a, 0xee, 0x49, 0x42, 0x89, 0x33, 0xea);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
    }

    [Guid("5541d8a7-497c-5aa4-86fc-7713adbf2a2c")]
    internal static class Nullable_DateTimeOffset
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.DateTime;i8))";
        internal static readonly Guid IID = new(0x5541d8a7, 0x497c, 0x5aa4, 0x86, 0xfc, 0x77, 0x13, 0xad, 0xbf, 0x2a, 0x2c);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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

        public static unsafe object GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            DateTimeOffset __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
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
        internal static readonly Guid IID = new(0x604d0c4c, 0x91de, 0x5c2a, 0x93, 0x5f, 0x36, 0x2f, 0x13, 0xea, 0xf8, 0x00);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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

        public static unsafe object GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            TimeSpan __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
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
        internal static readonly Guid IID = new(0x06dccc90, 0xa058, 0x5c88, 0x87, 0xb7, 0x6f, 0x33, 0x60, 0xa2, 0xfc, 0x16);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
        internal static readonly Guid IID = new(0x3830ad99, 0xd8da, 0x53f3, 0x98, 0x9b, 0xfc, 0x92, 0xad, 0x22, 0x27, 0x78);

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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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

        public static unsafe Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            Type __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
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

    [Guid("6ff27a1e-4b6a-59b7-b2c3-d1f2ee474593")]
    internal static class Nullable_Exception
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.HResult;i4))";
        internal static Guid IID = new(0x6ff27a1e, 0x4b6a, 0x59b7, 0xb2, 0xc3, 0xd1, 0xf2, 0xee, 0x47, 0x45, 0x93);

        [Guid("6ff27a1e-4b6a-59b7-b2c3-d1f2ee474593")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Get_Value_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Exception*, int> Get_Value_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, Exception*, int>)_Get_Value_0; set => _Get_Value_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, Exception* value);
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
                    _Get_Value_0 = (delegate* unmanaged<IntPtr, Exception*, int>)&Do_Abi_get_Value_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, Exception* __return_value__)
            {
                global::System.Exception ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = (global::System.Exception)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                    *__return_value__ = Exception.FromManaged(____return_value__);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        public static unsafe Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            Exception __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref IID, out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Exception*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(Exception.FromAbi(__retval));
            }
            finally
            {
                Exception.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("25230F05-B49C-57EE-8961-5373D98E1AB1")]
    internal static class Nullable_EventHandler
    {
        internal static readonly Guid IID = new(0x25230F05, 0xB49C, 0x57EE, 0x89, 0x61, 0x53, 0x73, 0xD9, 0x8E, 0x1A, 0xB1);

        public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
        private readonly static Nullable_Delegates.GetValueDelegate _Get_Value_0;
#endif

        unsafe static Nullable_EventHandler()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Nullable_EventHandler), sizeof(IInspectable.Vftbl) + sizeof(IntPtr));
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
#if !NET
            ((IntPtr*)AbiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(_Get_Value_0 = Do_Abi_get_Value_0);
#else
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_get_Value_0;
#endif
        }

#if NET
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, IntPtr* __return_value__)
        {
            global::System.EventHandler ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::System.EventHandler>(thisPtr);
                *__return_value__ = EventHandler.FromManaged(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        public static unsafe Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(EventHandler.FromAbi(__retval));
            }
            finally
            {
                EventHandler.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    [Guid("61C17706-2D65-11E0-9AE8-D48564015472")]
    internal static class Nullable_Delegate<T> where T : global::System.Delegate
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(Nullable<T>));

        public static readonly IntPtr AbiToProjectionVftablePtr;

        private readonly static Nullable_Delegates.GetValueDelegate _Get_Value_0;

        unsafe static Nullable_Delegate()
        {
            _Get_Value_0 = Do_Abi_get_Value_0;

            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Nullable_Delegate<T>), sizeof(IInspectable.Vftbl) + sizeof(IntPtr));
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((IntPtr*)AbiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(_Get_Value_0);
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

        public static Guid PIID = Nullable<T>.PIID;

        public static unsafe Nullable GetValue(IInspectable inspectable)
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

    internal static class Nullable_IntEnum
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private unsafe delegate int GetValueDelegate(IntPtr thisPtr, int* value);
            private static readonly GetValueDelegate delegateCache;
#endif

        unsafe static Nullable_IntEnum()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Nullable_IntEnum), sizeof(IInspectable.Vftbl) + sizeof(IntPtr));
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
#if !NET
            ((IntPtr*)AbiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0);
#else
            ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_get_Value_0;
#endif
        }

#if NET
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, int* __return_value__)
        {
            *__return_value__ = default;

            try
            {
                *__return_value__ = (int)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        internal static Guid GetIID(global::System.Type enumType)
        {
            return GuidGenerator.CreateIIDForGenericType("pinterface({61c17706-2d65-11e0-9ae8-d48564015472};enum(" + enumType.FullName + ";i4))");
        }

        internal static unsafe object GetValue(global::System.Type enumType, IInspectable inspectable)
        {
            var IID = GetIID(enumType);
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                int __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return Enum.ToObject(enumType, __retval);
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    internal static class Nullable_FlagsEnum
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
        private unsafe delegate int GetValueDelegate(IntPtr thisPtr, uint* value);
        private static readonly GetValueDelegate delegateCache;
#endif

        unsafe static Nullable_FlagsEnum()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Nullable_FlagsEnum), sizeof(IInspectable.Vftbl) + sizeof(IntPtr));
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
#if !NET
            ((IntPtr*)AbiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_get_Value_0);
#else
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_get_Value_0;
#endif
        }

#if NET
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_get_Value_0(IntPtr thisPtr, uint* __return_value__)
        {
            *__return_value__ = default;

            try
            {
                *__return_value__ = (uint)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        internal static Guid GetIID(global::System.Type enumType)
        {
            return GuidGenerator.CreateIIDForGenericType("pinterface({61c17706-2d65-11e0-9ae8-d48564015472};enum(" + enumType.FullName + ";u4))");
        }

        internal static unsafe object GetValue(global::System.Type enumType, IInspectable inspectable)
        {
            var IID = GetIID(enumType);
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                uint __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return Enum.ToObject(enumType, __retval);
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    internal static class Nullable_Delegates
    {
        public unsafe delegate int GetValueDelegate(IntPtr thisPtr, IntPtr* value);
        public unsafe delegate int GetValueDelegateAbi(void* thisPtr, void* value);
        public unsafe delegate int GetValueDelegateAbiDateTimeOffset(void* ptr, DateTimeOffset* result);
    }

    internal static class NullableBlittable<T> where T: unmanaged
    {
        private readonly static Guid IID = NullableType.GetIID<T>();

        public static unsafe object GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            try
            {
                T __retval = default;
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, void*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return __retval;
            }
            finally
            {
                Marshal.Release(nullablePtr);
            }
        }
    }

    internal static class NullableType
    {
        internal static Guid GetIID<T>()
        {
            if (typeof(T) == typeof(int)) return Nullable_int.IID;
            if (typeof(T) == typeof(byte)) return Nullable_byte.IID;
            if (typeof(T) == typeof(bool)) return Nullable_bool.IID;
            if (typeof(T) == typeof(sbyte)) return Nullable_sbyte.IID;
            if (typeof(T) == typeof(short)) return Nullable_short.IID;
            if (typeof(T) == typeof(ushort)) return Nullable_ushort.IID;
            if (typeof(T) == typeof(char)) return Nullable_char.IID;
            if (typeof(T) == typeof(uint)) return Nullable_uint.IID;
            if (typeof(T) == typeof(long)) return Nullable_long.IID;
            if (typeof(T) == typeof(ulong)) return Nullable_ulong.IID;
            if (typeof(T) == typeof(float)) return Nullable_float.IID;
            if (typeof(T) == typeof(double)) return Nullable_double.IID;
            if (typeof(T) == typeof(Guid)) return Nullable_guid.IID;
            if (typeof(T) == typeof(global::System.Type)) return Nullable_Type.IID;
            if (typeof(T) == typeof(global::System.TimeSpan)) return Nullable_TimeSpan.IID;
            if (typeof(T) == typeof(global::System.DateTimeOffset)) return Nullable_DateTimeOffset.IID;
            if (typeof(T) == typeof(global::Windows.Foundation.Point)) return IReferenceIIDs.IReferenceOfPoint_IID;
            if (typeof(T) == typeof(global::Windows.Foundation.Size)) return IReferenceIIDs.IReferenceOfSize_IID;
            if (typeof(T) == typeof(global::Windows.Foundation.Rect)) return IReferenceIIDs.IReferenceOfRect_IID;
            if (typeof(T) == typeof(global::System.Numerics.Matrix3x2)) return IReferenceIIDs.IReferenceMatrix3x2_IID;
            if (typeof(T) == typeof(global::System.Numerics.Matrix4x4)) return IReferenceIIDs.IReferenceMatrix4x4_IID;
            if (typeof(T) == typeof(global::System.Numerics.Plane)) return IReferenceIIDs.IReferencePlane_IID;
            if (typeof(T) == typeof(global::System.Numerics.Quaternion)) return IReferenceIIDs.IReferenceQuaternion_IID;
            if (typeof(T) == typeof(global::System.Numerics.Vector2)) return IReferenceIIDs.IReferenceVector2_IID;
            if (typeof(T) == typeof(global::System.Numerics.Vector3)) return IReferenceIIDs.IReferenceVector3_IID;
            if (typeof(T) == typeof(global::System.Numerics.Vector4)) return IReferenceIIDs.IReferenceVector4_IID;

            return GuidGenerator.CreateIIDUnsafe(typeof(Nullable<T>));
        }

        public static Func<IInspectable, object> GetValueFactory(global::System.Type type)
        {
            return ComWrappersSupport.CreateReferenceCachingFactory(GetValueFactoryInternal(type));
        }

        private static Func<IInspectable, object> GetValueFactoryInternal(global::System.Type type)
        {
            if (type == typeof(string)) return Nullable_string.GetValue;
            if (type == typeof(int)) return NullableBlittable<int>.GetValue;
            if (type == typeof(byte)) return NullableBlittable<byte>.GetValue;
            if (type == typeof(bool)) return NullableBlittable<bool>.GetValue;
            if (type == typeof(sbyte)) return NullableBlittable<sbyte>.GetValue;
            if (type == typeof(short)) return NullableBlittable<short>.GetValue;
            if (type == typeof(ushort)) return NullableBlittable<ushort>.GetValue;
            if (type == typeof(char)) return NullableBlittable<char>.GetValue;
            if (type == typeof(uint)) return NullableBlittable<uint>.GetValue;
            if (type == typeof(long)) return NullableBlittable<long>.GetValue;
            if (type == typeof(ulong)) return NullableBlittable<ulong>.GetValue;
            if (type == typeof(float)) return NullableBlittable<float>.GetValue;
            if (type == typeof(double)) return NullableBlittable<double>.GetValue;
            if (type == typeof(Guid)) return NullableBlittable<Guid>.GetValue;
            if (type == typeof(global::System.Type)) return Nullable_Type.GetValue;
            if (type == typeof(global::System.TimeSpan)) return Nullable_TimeSpan.GetValue;
            if (type == typeof(global::System.Exception)) return Nullable_Exception.GetValue;
            if (type == typeof(global::System.DateTimeOffset)) return Nullable_DateTimeOffset.GetValue;
            if (type == typeof(global::Windows.Foundation.Point)) return NullableBlittable<global::Windows.Foundation.Point>.GetValue;
            if (type == typeof(global::Windows.Foundation.Size)) return NullableBlittable<global::Windows.Foundation.Size>.GetValue;
            if (type == typeof(global::Windows.Foundation.Rect)) return NullableBlittable<global::Windows.Foundation.Rect>.GetValue;
            if (type == typeof(global::System.Numerics.Matrix3x2)) return NullableBlittable<global::System.Numerics.Matrix3x2>.GetValue;
            if (type == typeof(global::System.Numerics.Matrix4x4)) return NullableBlittable<global::System.Numerics.Matrix4x4>.GetValue;
            if (type == typeof(global::System.Numerics.Plane)) return NullableBlittable<global::System.Numerics.Plane>.GetValue;
            if (type == typeof(global::System.Numerics.Quaternion)) return NullableBlittable<global::System.Numerics.Quaternion>.GetValue;
            if (type == typeof(global::System.Numerics.Vector2)) return NullableBlittable<global::System.Numerics.Vector2>.GetValue;
            if (type == typeof(global::System.Numerics.Vector3)) return NullableBlittable<global::System.Numerics.Vector3>.GetValue;
            if (type == typeof(global::System.Numerics.Vector4)) return NullableBlittable<global::System.Numerics.Vector4>.GetValue;
            if (type == typeof(global::System.EventHandler)) return Nullable_EventHandler.GetValue;
            if (type.IsEnum && Enum.GetUnderlyingType(type) == typeof(int)) return (inspectable) => Nullable_IntEnum.GetValue(type, inspectable);
            if (type.IsEnum && Enum.GetUnderlyingType(type) == typeof(uint)) return (inspectable) => Nullable_FlagsEnum.GetValue(type, inspectable);

#if NET
            var winrtExposedClassAttribute = type.GetCustomAttribute<WinRTExposedTypeAttribute>(false);
            if (winrtExposedClassAttribute == null)
            {
                var authoringMetadaType = type.GetAuthoringMetadataType();
                if (authoringMetadaType != null)
                {
                    winrtExposedClassAttribute = authoringMetadaType.GetCustomAttribute<WinRTExposedTypeAttribute>(false);
                }
            }

            if (winrtExposedClassAttribute != null && winrtExposedClassAttribute.WinRTExposedTypeDetails != null)
            {
                if (Activator.CreateInstance(winrtExposedClassAttribute.WinRTExposedTypeDetails) is IWinRTNullableTypeDetails nullableTypeDetails)
                {
                    return nullableTypeDetails.GetNullableValue;
                }
            }

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException($"Failed to get the value from nullable with type '{type}'.");
            }
#endif

            // Fallback for .NET standard and pre-existing projections.
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            if (type.IsDelegate())
            {
                return ComWrappersSupport.CreateAbiNullableTFactory(typeof(Nullable_Delegate<>).MakeGenericType(type));
            }
            else
            {
                return ComWrappersSupport.CreateNullableTFactory(typeof(global::System.Nullable<>).MakeGenericType(type));
            }
#pragma warning restore IL3050
        }
    }

    internal static class IReferenceSignatures
    {
        public const string Point = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Point;f4;f4))";
        public const string Size = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Size;f4;f4))";
        public const string Rect = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Rect;f4;f4;f4;f4))";
        public const string Matrix3x2 = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Numerics.Matrix3x2;f4;f4;f4;f4;f4;f4))";
        public const string Matrix4x4 = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Numerics.Matrix4x4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4))";
        public const string Plane = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Numerics.Plane;struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4);f4))";
        public const string Quaternion = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Numerics.Quaternion;f4;f4;f4;f4))";
        public const string Vector2 = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Numerics.Vector2;f4;f4))";
        public const string Vector3 = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4))";
        public const string Vector4 = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.Numerics.Vector4;f4;f4;f4;f4))";
    }

    internal static class IReferenceIIDs
    {
#if NET
        internal static readonly Guid IReferenceOfPoint_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x22, 0x4C, 0xF1, 0x84, 0x0A, 0xA0, 0x72, 0x52, 0x8D, 0x3D, 0x82, 0x11, 0x2E, 0x66, 0xDF, 0x00 }));
        internal static readonly Guid IReferenceOfSize_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x86, 0x30, 0x72, 0x61, 0x53, 0x8E, 0x76, 0x52, 0x9F, 0x36, 0x2A, 0x4B, 0xB9, 0x3E, 0x2B, 0x75 }));
        internal static readonly Guid IReferenceOfRect_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x11, 0x3F, 0x42, 0x80, 0x4F, 0x05, 0xAC, 0x5E, 0xAF, 0xD3, 0x63, 0xB6, 0xCE, 0x15, 0xE7, 0x7B }));
        internal static readonly Guid IReferenceMatrix3x2_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xfd, 0x8c, 0x35, 0x76, 0xbd, 0x2c, 0x5b, 0x52, 0xa4, 0x9e, 0x90, 0xee, 0x18, 0x24, 0x7b, 0x71 }));
        internal static readonly Guid IReferenceMatrix4x4_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xdc, 0xff, 0xcb, 0xda, 0xef, 0x68, 0xd0, 0x5f, 0xb6, 0x57, 0x78, 0x2d, 0x0a, 0xc9, 0x80, 0x7e }));
        internal static readonly Guid IReferencePlane_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xa1, 0x42, 0xd5, 0x46, 0xf7, 0x52, 0xe7, 0x58, 0xac, 0xfc, 0x9a, 0x6d, 0x36, 0x4d, 0xa0, 0x22 }));
        internal static readonly Guid IReferenceQuaternion_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xbb, 0x04, 0x70, 0xb2, 0x14, 0xc0, 0xce, 0x5d, 0x9a, 0x21, 0x79, 0x9c, 0x5a, 0x3c, 0x14, 0x61 }));
        internal static readonly Guid IReferenceVector2_IID = new(new ReadOnlySpan<byte>(new byte[] { 0x9e, 0xa6, 0xf6, 0x48, 0x65, 0x84, 0xae, 0x57, 0x94, 0x00, 0x97, 0x64, 0x08, 0x7f, 0x65, 0xad }));
        internal static readonly Guid IReferenceVector3_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xff, 0x70, 0xe7, 0x1e, 0x54, 0xc9, 0xca, 0x59, 0xa7, 0x54, 0x61, 0x99, 0xa9, 0xbe, 0x28, 0x2c }));
        internal static readonly Guid IReferenceVector4_IID = new(new ReadOnlySpan<byte>(new byte[] { 0xc9, 0x43, 0xe8, 0xa5, 0x20, 0xed, 0x39, 0x53, 0x8f, 0x8d, 0x9f, 0xe4, 0x04, 0xcf, 0x36, 0x54 }));
#else
        internal static readonly Guid IReferenceOfPoint_IID = new(0x84F14C22, 0xA00A, 0x5272, 0x8D, 0x3D, 0x82, 0x11, 0x2E, 0x66, 0xDF, 0x00);
        internal static readonly Guid IReferenceOfSize_IID = new(0x61723086, 0x8E53, 0x5276, 0x9F, 0x36, 0x2A, 0x4B, 0xB9, 0x3E, 0x2B, 0x75);
        internal static readonly Guid IReferenceOfRect_IID = new(0x80423F11, 0x054F, 0x5EAC, 0xAF, 0xD3, 0x63, 0xB6, 0xCE, 0x15, 0xE7, 0x7B);
        internal static readonly Guid IReferenceMatrix3x2_IID = new(0x76358cfd, 0x2cbd, 0x525b, 0xa4, 0x9e, 0x90, 0xee, 0x18, 0x24, 0x7b, 0x71);
        internal static readonly Guid IReferenceMatrix4x4_IID = new(0xdacbffdc, 0x68ef, 0x5fd0, 0xb6, 0x57, 0x78, 0x2d, 0x0a, 0xc9, 0x80, 0x7e);
        internal static readonly Guid IReferencePlane_IID = new(0x46d542a1, 0x52f7, 0x58e7, 0xac, 0xfc, 0x9a, 0x6d, 0x36, 0x4d, 0xa0, 0x22);
        internal static readonly Guid IReferenceQuaternion_IID = new(0xb27004bb, 0xc014, 0x5dce, 0x9a, 0x21, 0x79, 0x9c, 0x5a, 0x3c, 0x14, 0x61);
        internal static readonly Guid IReferenceVector2_IID = new(0x48f6a69e, 0x8465, 0x57ae, 0x94, 0x00, 0x97, 0x64, 0x08, 0x7f, 0x65, 0xad);
        internal static readonly Guid IReferenceVector3_IID = new(0x1ee770ff, 0xc954, 0x59ca, 0xa7, 0x54, 0x61, 0x99, 0xa9, 0xbe, 0x28, 0x2c);
        internal static readonly Guid IReferenceVector4_IID = new(0xa5e843c9, 0xed20, 0x5339, 0x8f, 0x8d, 0x9f, 0xe4, 0x04, 0xcf, 0x36, 0x54);
#endif
    }
}

#if NET
    namespace WinRT
{
    internal interface IWinRTNullableTypeDetails
    {
        object GetNullableValue(IInspectable inspectable);
    }

    public sealed class StructTypeDetails<T, TAbi> : IWinRTExposedTypeDetails, IWinRTNullableTypeDetails where T: struct where TAbi : unmanaged
    {
        private static readonly Guid PIID = ABI.System.Nullable<T>.PIID;

        [SkipLocalsInit]
        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            Span<ComWrappers.ComInterfaceEntry> entries = stackalloc ComWrappers.ComInterfaceEntry[2];
            int count = 0;

            if (FeatureSwitches.EnableIReferenceSupport)
            {
                entries[count++] = new ComWrappers.ComInterfaceEntry
                {
                    IID = ABI.Windows.Foundation.ManagedIPropertyValueImpl.IID,
                    Vtable = ABI.Windows.Foundation.ManagedIPropertyValueImpl.AbiToProjectionVftablePtr
                };

                entries[count++] = new ComWrappers.ComInterfaceEntry
                {
                    IID = PIID,
                    Vtable = ABI.Windows.Foundation.BoxedValueIReferenceImpl<T, TAbi>.AbiToProjectionVftablePtr
                };
            }

            return entries.Slice(0, count).ToArray();
        }

        unsafe object IWinRTNullableTypeDetails.GetNullableValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            TAbi __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in PIID), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, void*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                if (typeof(T) == typeof(TAbi))
                {
                    return __retval;
                }
                else 
                {
                    return Marshaler<T>.FromAbi(__retval);
                }
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }

    public abstract class DelegateTypeDetails<T> : IWinRTExposedTypeDetails, IWinRTNullableTypeDetails where T : global::System.Delegate
    {
        private static readonly Guid PIID = ABI.System.Nullable<T>.PIID;

        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            return GetExposedInterfaces(GetDelegateInterface());
        }

        public static ComWrappers.ComInterfaceEntry[] GetExposedInterfaces(ComWrappers.ComInterfaceEntry delegateInterface)
        {
            Span<ComWrappers.ComInterfaceEntry> entries = stackalloc ComWrappers.ComInterfaceEntry[3];
            int count = 0;

            entries[count++] = delegateInterface;

            if (FeatureSwitches.EnableIReferenceSupport)
            {
                entries[count++] = new ComWrappers.ComInterfaceEntry
                {
                    IID = ABI.Windows.Foundation.ManagedIPropertyValueImpl.IID,
                    Vtable = ABI.Windows.Foundation.ManagedIPropertyValueImpl.AbiToProjectionVftablePtr
                };

                entries[count++] = new ComWrappers.ComInterfaceEntry
                {
                    IID = PIID,
                    Vtable = ABI.System.Nullable_Delegate<T>.AbiToProjectionVftablePtr
                };
            }

            return entries.Slice(0, count).ToArray();
        }

        public abstract ComWrappers.ComInterfaceEntry GetDelegateInterface();

        unsafe object IWinRTNullableTypeDetails.GetNullableValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in PIID), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new ABI.System.Nullable(Marshaler<T>.FromAbi(__retval));
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__retval);
                Marshal.Release(nullablePtr);
            }
        }
    }
}
#endif

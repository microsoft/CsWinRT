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

        private static unsafe int Do_Abi_get_Value_0_DateTimeOffset(void* thisPtr, global::ABI.System.DateTimeOffset* result)
        {
            if (result is null)
            {
                return unchecked((int)0x80004003);
            }

            try
            {
                T unboxedValue = (T)global::WinRT.ComWrappersSupport.FindObject<object>(new IntPtr(thisPtr));

                Unsafe.WriteUnaligned(result, global::ABI.System.DateTimeOffset.FromManaged(Unsafe.As<T, global::System.DateTimeOffset>(ref unboxedValue)));

                return 0;
            }
            catch (global::System.Exception __exception__)
            {
                Unsafe.WriteUnaligned<T>(result, default);

                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.NotSupportedIfDynamicCodeIsNotAvailable)]
#endif
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
                global::ABI.System.Nullable_Delegates.GetValueDelegateAbi stub = new(Do_Abi_get_Value_0_Blittable);

                nativePtr = Marshal.GetFunctionPointerForDelegate(stub);

                return stub;
            }

            if (typeof(T) == typeof(global::System.DateTimeOffset))
            {
                global::ABI.System.Nullable_Delegates.GetValueDelegateAbiDateTimeOffset stub = new(Do_Abi_get_Value_0_DateTimeOffset);

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
                global::ABI.System.Nullable<T>.Vftbl.get_Value_0_Type ??= Projections.GetAbiDelegateType(typeof(void*), Marshaler<T>.AbiType.MakeByRefType(), typeof(int));

                Delegate stub = Delegate.CreateDelegate(
                    global::ABI.System.Nullable<T>.Vftbl.get_Value_0_Type,
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
        private static readonly global::ABI.System.Nullable_Delegates.GetValueDelegateAbi GetValue;

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
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
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
            if (RuntimeFeature.IsDynamicCodeCompiled)
#endif
            {
                var __params = new object[] { thisPtr, null };
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                try
                {
                    marshallingDelegate.DynamicInvokeAbi(__params);
                    return Marshaler<T>.FromAbi(__params[1]);
                }
                finally
                {
                    Marshaler<T>.DisposeAbi(__params[1]);
                }
#pragma warning restore IL3050
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
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID.IID_NullableString), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(MarshalString.FromAbi(__retval));
            }
            finally
            {
                MarshalString.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("e5198cc8-2873-55f5-b0a1-84ff9e4aad62")]
    internal static class Nullable_byte
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};u1)";

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
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID.IID_NullableDateTimeOffset), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, DateTimeOffset*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return DateTimeOffset.FromAbi(__retval);
            }
            finally
            {
                DateTimeOffset.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("604d0c4c-91de-5c2a-935f-362f13eaf800")]
    internal static class Nullable_TimeSpan
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.TimeSpan;i8))";

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
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID.IID_NullableTimeSpan), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, TimeSpan*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return TimeSpan.FromAbi(__retval);
            }
            finally
            {
                TimeSpan.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("06dccc90-a058-5c88-87b7-6f3360a2fc16")]
    internal static class Nullable_Object
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};cinterface(IInspectable))";

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
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID.IID_NullableType), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Type*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(Type.FromAbi(__retval));
            }
            finally
            {
                Type.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("6ff27a1e-4b6a-59b7-b2c3-d1f2ee474593")]
    internal static class Nullable_Exception
    {
        public static string GetGuidSignature() => "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};struct(Windows.Foundation.HResult;i4))";

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
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IID.IID_NullableException, out nullablePtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID.IID_NullableException), out nullablePtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Exception*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(Exception.FromAbi(__retval));
            }
            finally
            {
                Exception.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("25230F05-B49C-57EE-8961-5373D98E1AB1")]
    internal static class Nullable_EventHandler
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
        private static readonly Nullable_Delegates.GetValueDelegate _Get_Value_0;
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
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IID.IID_NullableEventHandler), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(EventHandler.FromAbi(__retval));
            }
            finally
            {
                EventHandler.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("1eeae0cb-8f57-5c37-a087-a55d46e2fe3f")]
    internal static class Nullable_PropertyChangedEventHandler
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;

        public static Guid IID => FeatureSwitches.UseWindowsUIXamlProjections 
            ? global::WinRT.Interop.IID.IID_WUX_NullablePropertyChangedEventHandler 
            : global::WinRT.Interop.IID.IID_MUX_NullablePropertyChangedEventHandler;

#if !NET
        private static readonly Nullable_Delegates.GetValueDelegate _Get_Value_0;
#endif

        unsafe static Nullable_PropertyChangedEventHandler()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Nullable_PropertyChangedEventHandler), sizeof(IInspectable.Vftbl) + sizeof(IntPtr));
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
            global::System.ComponentModel.PropertyChangedEventHandler ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.PropertyChangedEventHandler>(thisPtr);
                *__return_value__ = global::ABI.System.ComponentModel.PropertyChangedEventHandler.FromManaged(____return_value__);
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
            Guid iid = IID;
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in iid), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(global::ABI.System.ComponentModel.PropertyChangedEventHandler.FromAbi(__retval));
            }
            finally
            {
                global::ABI.System.ComponentModel.PropertyChangedEventHandler.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("779d5a21-0e7d-5476-bb90-27fa3b4b8de5")]
    internal static class Nullable_NotifyCollectionChangedEventHandler
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;

        public static Guid IID => FeatureSwitches.UseWindowsUIXamlProjections
            ? global::WinRT.Interop.IID.IID_WUX_NotifyCollectionChangedEventHandler
            : global::WinRT.Interop.IID.IID_MUX_NotifyCollectionChangedEventHandler;

#if !NET
        private static readonly Nullable_Delegates.GetValueDelegate _Get_Value_0;
#endif

        unsafe static Nullable_NotifyCollectionChangedEventHandler()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Nullable_NotifyCollectionChangedEventHandler), sizeof(IInspectable.Vftbl) + sizeof(IntPtr));
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
            global::System.Collections.Specialized.NotifyCollectionChangedEventHandler ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>(thisPtr);
                *__return_value__ = global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.FromManaged(____return_value__);
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
            Guid iid = IID;
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in iid), out nullablePtr));
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.FromAbi(__retval));
            }
            finally
            {
                global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.DisposeAbi(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    [Guid("61C17706-2D65-11E0-9AE8-D48564015472")]
    internal static class Nullable_Delegate<T> where T : global::System.Delegate
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(Nullable<T>));

        public static readonly IntPtr AbiToProjectionVftablePtr;

        private static readonly Nullable_Delegates.GetValueDelegate _Get_Value_0;

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
                *__return_value__ = (IntPtr)MarshalGeneric<T>.FromManaged(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        public static readonly Guid PIID = Nullable<T>.PIID;

        public static unsafe Nullable GetValue(IInspectable inspectable)
        {
            IntPtr nullablePtr = IntPtr.Zero;
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in PIID, out nullablePtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in PIID), out nullablePtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)nullablePtr)[6](nullablePtr, &__retval));
                return new Nullable(MarshalDelegate.FromAbi<T>(__retval));
            }
            finally
            {
                MarshalExtensions.ReleaseIfNotNull(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
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
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
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
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
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
        private static readonly Guid IID = NullableType.GetIID<T>();

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
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }
    }

    internal static class NullableType
    {
        internal static Guid GetIID<T>()
        {
            if (typeof(T) == typeof(int)) return IID.IID_NullableInt;
            if (typeof(T) == typeof(byte)) return IID.IID_NullableByte;
            if (typeof(T) == typeof(bool)) return IID.IID_NullableBool;
            if (typeof(T) == typeof(sbyte)) return IID.IID_NullableSByte;
            if (typeof(T) == typeof(short)) return IID.IID_NullableShort;
            if (typeof(T) == typeof(ushort)) return IID.IID_NullableUShort;
            if (typeof(T) == typeof(char)) return IID.IID_NullableChar;
            if (typeof(T) == typeof(uint)) return IID.IID_NullableUInt;
            if (typeof(T) == typeof(long)) return IID.IID_NullableLong;
            if (typeof(T) == typeof(ulong)) return IID.IID_NullableULong;
            if (typeof(T) == typeof(float)) return IID.IID_NullableFloat;
            if (typeof(T) == typeof(double)) return IID.IID_NullableDouble;
            if (typeof(T) == typeof(Guid)) return IID.IID_NullableGuid;
            if (typeof(T) == typeof(global::System.Type)) return IID.IID_NullableType;
            if (typeof(T) == typeof(global::System.TimeSpan)) return IID.IID_NullableTimeSpan;
            if (typeof(T) == typeof(global::System.DateTimeOffset)) return IID.IID_NullableDateTimeOffset;
            if (typeof(T) == typeof(global::Windows.Foundation.Point)) return IID.IID_IReferenceOfPoint;
            if (typeof(T) == typeof(global::Windows.Foundation.Size)) return IID.IID_IReferenceOfSize;
            if (typeof(T) == typeof(global::Windows.Foundation.Rect)) return IID.IID_IReferenceOfRect;
            if (typeof(T) == typeof(global::System.Numerics.Matrix3x2)) return IID.IID_IReferenceMatrix3x2;
            if (typeof(T) == typeof(global::System.Numerics.Matrix4x4)) return IID.IID_IReferenceMatrix4x4;
            if (typeof(T) == typeof(global::System.Numerics.Plane)) return IID.IID_IReferencePlane;
            if (typeof(T) == typeof(global::System.Numerics.Quaternion)) return IID.IID_IReferenceQuaternion;
            if (typeof(T) == typeof(global::System.Numerics.Vector2)) return IID.IID_IReferenceVector2;
            if (typeof(T) == typeof(global::System.Numerics.Vector3)) return IID.IID_IReferenceVector3;
            if (typeof(T) == typeof(global::System.Numerics.Vector4)) return IID.IID_IReferenceVector4;

            return GuidGenerator.CreateIIDUnsafe(typeof(Nullable<T>));
        }

        public static Func<IInspectable, object> GetValueFactory(global::System.Type type)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                throw new NotSupportedException("Support for 'IReference<T>' is not enabled.");
            }

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
            if (type == typeof(global::System.ComponentModel.PropertyChangedEventHandler)) return Nullable_PropertyChangedEventHandler.GetValue;
            if (type == typeof(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler)) return Nullable_NotifyCollectionChangedEventHandler.GetValue;
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

        // Gets the nullable type representation for some built-in known types.
        public static global::System.Type GetTypeAsNullableType(global::System.Type type)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                throw new NotSupportedException("Support for 'IReference<T>' is not enabled.");
            }

            if (type == typeof(int)) return typeof(global::System.Nullable<int>);
            if (type == typeof(byte)) return typeof(global::System.Nullable<byte>);
            if (type == typeof(bool)) return typeof(global::System.Nullable<bool>);
            if (type == typeof(sbyte)) return typeof(global::System.Nullable<sbyte>);
            if (type == typeof(short)) return typeof(global::System.Nullable<short>);
            if (type == typeof(ushort)) return typeof(global::System.Nullable<ushort>);
            if (type == typeof(char)) return typeof(global::System.Nullable<char>);
            if (type == typeof(uint)) return typeof(global::System.Nullable<uint>);
            if (type == typeof(long)) return typeof(global::System.Nullable<long>);
            if (type == typeof(ulong)) return typeof(global::System.Nullable<ulong>);
            if (type == typeof(float)) return typeof(global::System.Nullable<float>);
            if (type == typeof(double)) return typeof(global::System.Nullable<double>);
            if (type == typeof(Guid)) return typeof(global::System.Nullable<Guid>);
            if (type == typeof(global::System.TimeSpan)) return typeof(global::System.Nullable<global::System.TimeSpan>);
            if (type == typeof(global::System.DateTimeOffset)) return typeof(global::System.Nullable<global::System.DateTimeOffset>);
            if (type == typeof(global::Windows.Foundation.Point)) return typeof(global::System.Nullable<global::Windows.Foundation.Point>);
            if (type == typeof(global::Windows.Foundation.Size)) return typeof(global::System.Nullable<global::Windows.Foundation.Size>);
            if (type == typeof(global::Windows.Foundation.Rect)) return typeof(global::System.Nullable<global::Windows.Foundation.Rect>);
            if (type == typeof(global::System.Numerics.Matrix3x2)) return typeof(global::System.Nullable<global::System.Numerics.Matrix3x2>);
            if (type == typeof(global::System.Numerics.Matrix4x4)) return typeof(global::System.Nullable<global::System.Numerics.Matrix4x4>);
            if (type == typeof(global::System.Numerics.Plane)) return typeof(global::System.Nullable<global::System.Numerics.Plane>);
            if (type == typeof(global::System.Numerics.Quaternion)) return typeof(global::System.Nullable<global::System.Numerics.Quaternion>);
            if (type == typeof(global::System.Numerics.Vector2)) return typeof(global::System.Nullable<global::System.Numerics.Vector2>);
            if (type == typeof(global::System.Numerics.Vector3)) return typeof(global::System.Nullable<global::System.Numerics.Vector3>);
            if (type == typeof(global::System.Numerics.Vector4)) return typeof(global::System.Nullable<global::System.Numerics.Vector4>);

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
                    return nullableTypeDetails.GetNullableType();
                }
            }

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException($"Failed to construct nullable with type '{type}'.");
            }
#endif

            return null;
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
}

#if NET
    namespace WinRT
{
    internal interface IWinRTNullableTypeDetails
    {
        object GetNullableValue(IInspectable inspectable);
        Type GetNullableType();
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
                    IID = global::WinRT.Interop.IID.IID_IPropertyValue,
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
                    // If 'typeof(T) != typeof(TAbi)', then we know the type is not blittable.
                    // We can use the specific marshaller then to minimize binary size here.
                    return MarshalNonBlittable<T>.FromAbi(__retval);
                }
            }
            finally
            {
                // We only need to dispose in the non blittable case. Otherwise,
                // that value is returned directly to the caller and used as is.
                if (typeof(T) != typeof(TAbi))
                {
                    MarshalNonBlittable<T>.DisposeAbi(__retval);
                }

                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }

        Type IWinRTNullableTypeDetails.GetNullableType() => typeof(global::System.Nullable<T>);
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
                    IID = global::WinRT.Interop.IID.IID_IPropertyValue,
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
                return new ABI.System.Nullable(MarshalDelegate.FromAbi<T>(__retval));
            }
            finally
            {
                MarshalExtensions.ReleaseIfNotNull(__retval);
                MarshalExtensions.ReleaseIfNotNull(nullablePtr);
            }
        }

        // Delegates are handled separately.
        Type IWinRTNullableTypeDetails.GetNullableType() => throw new NotImplementedException();
    }

    public sealed class EnumTypeDetails<T> : IWinRTExposedTypeDetails, IWinRTNullableTypeDetails where T : unmanaged, Enum
    {
        [SkipLocalsInit]
        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            Span<ComWrappers.ComInterfaceEntry> entries = stackalloc ComWrappers.ComInterfaceEntry[2];
            int count = 0;

            if (FeatureSwitches.EnableIReferenceSupport)
            {
                entries[count++] = new ComWrappers.ComInterfaceEntry
                {
                    IID = global::WinRT.Interop.IID.IID_IPropertyValue,
                    Vtable = ABI.Windows.Foundation.ManagedIPropertyValueImpl.AbiToProjectionVftablePtr
                };

                if (typeof(T).IsDefined(typeof(FlagsAttribute)))
                {
                    entries[count++] = new ComWrappers.ComInterfaceEntry
                    {
                        IID = ABI.System.Nullable_FlagsEnum.GetIID(typeof(T)),
                        Vtable = ABI.System.Nullable_FlagsEnum.AbiToProjectionVftablePtr
                    };
                }
                else
                {
                    entries[count++] = new ComWrappers.ComInterfaceEntry
                    {
                        IID = ABI.System.Nullable_IntEnum.GetIID(typeof(T)),
                        Vtable = ABI.System.Nullable_IntEnum.AbiToProjectionVftablePtr
                    };
                }
            }

            return entries.Slice(0, count).ToArray();
        }

        Type IWinRTNullableTypeDetails.GetNullableType() => typeof(global::System.Nullable<T>);

        // Unboxing enums are handled separately.
        object IWinRTNullableTypeDetails.GetNullableValue(IInspectable inspectable) => throw new NotImplementedException();
    }
}
#endif

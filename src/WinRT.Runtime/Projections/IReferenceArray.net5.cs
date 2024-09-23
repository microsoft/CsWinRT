// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
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
                (*____return_value__Size, *__return_value__) = FromManagedArray(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static (int, IntPtr) FromManagedArray(T[] items)
        {
            // Blittable value types
            if (typeof(T) == typeof(byte) ||
                typeof(T) == typeof(short) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(long) ||
                typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(int) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(float) ||
                typeof(T) == typeof(double) ||
                typeof(T) == typeof(Guid) ||
                typeof(T) == typeof(global::System.Numerics.Vector2) ||
                typeof(T) == typeof(global::System.Numerics.Vector3) ||
                typeof(T) == typeof(global::System.Numerics.Vector4) ||
                typeof(T) == typeof(global::System.Numerics.Quaternion) ||
                typeof(T) == typeof(global::System.Numerics.Plane) ||
                typeof(T) == typeof(global::System.Numerics.Matrix3x2) ||
                typeof(T) == typeof(global::System.Numerics.Matrix4x4))
            {
                return MarshalBlittable<T>.FromManagedArray(items);
            }

            // Non-blittable value types
            if (typeof(T) == typeof(bool) ||
                typeof(T) == typeof(char) ||
                typeof(T) == typeof(TimeSpan) ||
                typeof(T) == typeof(DateTimeOffset))
            {
                return MarshalNonBlittable<T>.FromManagedArray(items);
            }

            // Other well-known reference types
            if (typeof(T) == typeof(string)) return MarshalString.FromManagedArray(Unsafe.As<string[]>(items));
            if (typeof(T) == typeof(Type)) return ABI.System.Type.FromManagedArray(Unsafe.As<Type[]>(items));
            if (typeof(T) == typeof(Exception)) return MarshalNonBlittable<Exception>.FromManagedArray(Unsafe.As<Exception[]>(items));
            if (typeof(T) == typeof(object)) return MarshalInspectable<object>.FromManagedArray(Unsafe.As<object[]>(items));

#if NET
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException($"Support for 'IReferenceArray<T>' for type '{typeof(T)}' is not available in AOT environments.");
            }
#endif
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            return Marshaler<T>.FromManagedArray(items);
#pragma warning restore IL3050
        }
    }

    // This type is only used in JIT scenarios, as all well known types supported for AOT use specialized 'IReferenceArray<T>' implementations.
    // We can't add '[RequiresDynamicCode]' on this type, as it's indirectly rooted by the interface type. Because it's never actually used
    // on AOT, we can just guard each AOT-unsafe code path with a feature check, and throw if on AOT. That avoids ILC warnings.
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
#if NET
            // This path is unreachable on AOT, but this helps ILC
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                throw new NotSupportedException("'IReferenceArray<T>' is not supported in AOT environments.");
            }
#endif

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            var wrapper = new IReferenceArray<T>(ObjectReference<IUnknownVftbl>.FromAbi(ptr, PIID));
            return wrapper.Value;
        }

        public static unsafe object GetValue(IInspectable inspectable)
        {
#if NET
            // This path is unreachable on AOT, but this fixes some ILC warnings
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                throw new NotSupportedException("'IReferenceArray<T>' is not supported in AOT environments.");
            }
#endif

            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in PIID), out referenceArrayPtr)
#endif
                    );
                GC.KeepAlive(inspectable);
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return Marshaler<T>.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                Marshaler<T>.DisposeAbiArray((__retval_length, __retval_data));
                MarshalExtensions.ReleaseIfNotNull(referenceArrayPtr);
            }
#pragma warning disable IL3050
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

        public static readonly Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(IReferenceArray<T>));

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
#if NET
                // This path is unreachable on AOT, but this fixes some ILC warnings
                if (!RuntimeFeature.IsDynamicCodeSupported)
                {
                    throw new NotSupportedException("'IReferenceArray<T>' is not supported in AOT environments.");
                }
#endif

                int __retval_length = default;
                IntPtr __retval_data = default;
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                try
                {
                    ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval_length, &__retval_data));
                    GC.KeepAlive(_obj);
                    return Marshaler<T>.FromAbiArray((__retval_length, __retval_data));
                }
                finally
                {
                    Marshaler<T>.DisposeAbiArray((__retval_length, __retval_data));
                }
#pragma warning restore IL3050
            }
        }
    }

    internal static class IReferenceArray_Delegates
    {
        public unsafe delegate int get_Value_0(IntPtr thisPtr, int* ____return_value__Size, IntPtr* __return_value__);
    }

    internal static class IReferenceArrayType
    {
        // Gets the IReferenceArray type representation for some built-in known types.
        public static global::System.Type GetTypeAsIReferenceArrayType(global::System.Type type)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                throw new NotSupportedException("Support for 'IReferenceArray<T>' is not enabled.");
            }

            if (type == typeof(int)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<int>), typeof(global::ABI.Windows.Foundation.IReferenceArray<int>));
            if (type == typeof(byte)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<byte>), typeof(global::ABI.Windows.Foundation.IReferenceArray<byte>));
            if (type == typeof(bool)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<bool>), typeof(global::ABI.Windows.Foundation.IReferenceArray<bool>));
            if (type == typeof(sbyte)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<sbyte>), typeof(global::ABI.Windows.Foundation.IReferenceArray<sbyte>));
            if (type == typeof(short)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<short>), typeof(global::ABI.Windows.Foundation.IReferenceArray<short>));
            if (type == typeof(ushort)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<ushort>), typeof(global::ABI.Windows.Foundation.IReferenceArray<ushort>));
            if (type == typeof(char)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<char>), typeof(global::ABI.Windows.Foundation.IReferenceArray<char>));
            if (type == typeof(uint)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<uint>), typeof(global::ABI.Windows.Foundation.IReferenceArray<uint>));
            if (type == typeof(long)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<long>), typeof(global::ABI.Windows.Foundation.IReferenceArray<long>));
            if (type == typeof(ulong)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<ulong>), typeof(global::ABI.Windows.Foundation.IReferenceArray<ulong>));
            if (type == typeof(float)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<float>), typeof(global::ABI.Windows.Foundation.IReferenceArray<float>));
            if (type == typeof(double)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<double>), typeof(global::ABI.Windows.Foundation.IReferenceArray<double>));
            if (type == typeof(Guid)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<Guid>), typeof(global::ABI.Windows.Foundation.IReferenceArray<Guid>));
            if (type == typeof(global::System.TimeSpan)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.TimeSpan>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.TimeSpan>));
            if (type == typeof(global::System.DateTimeOffset)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.DateTimeOffset>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.DateTimeOffset>));
            if (type == typeof(global::Windows.Foundation.Point)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::Windows.Foundation.Point>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::Windows.Foundation.Point>));
            if (type == typeof(global::Windows.Foundation.Size)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::Windows.Foundation.Size>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::Windows.Foundation.Size>));
            if (type == typeof(global::Windows.Foundation.Rect)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::Windows.Foundation.Rect>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::Windows.Foundation.Rect>));
            if (type == typeof(global::System.Numerics.Matrix3x2)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.Numerics.Matrix3x2>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Matrix3x2>));
            if (type == typeof(global::System.Numerics.Matrix4x4)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.Numerics.Matrix4x4>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Matrix4x4>));
            if (type == typeof(global::System.Numerics.Plane)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.Numerics.Plane>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Plane>));
            if (type == typeof(global::System.Numerics.Quaternion)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.Numerics.Quaternion>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Quaternion>));
            if (type == typeof(global::System.Numerics.Vector2)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.Numerics.Vector2>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Vector2>));
            if (type == typeof(global::System.Numerics.Vector3)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.Numerics.Vector3>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Vector3>));
            if (type == typeof(global::System.Numerics.Vector4)) return GetArrayTypeAndRegisterHelperType(typeof(global::Windows.Foundation.IReferenceArray<global::System.Numerics.Vector4>), typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Vector4>));

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
                    return nullableTypeDetails.GetNullableArrayType();
                }
            }

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException($"Failed to construct IReferenceArray with type '{type}'.");
            }
#endif

            return null;
        }

        internal static Type GetArrayTypeAndRegisterHelperType(
            Type implementationType,
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                        DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type helperType)
        {
            ComWrappersSupport.RegisterHelperType(implementationType, helperType);
            return implementationType;
        }
    }
}
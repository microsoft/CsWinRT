// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.System;
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

            var wrapper = new IReferenceArray<T>(ObjectReference<IUnknownVftbl>.FromAbi(ptr, PIID));
            return wrapper.Value;
        }

        public static unsafe object GetValue(IInspectable inspectable)
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in PIID), out referenceArrayPtr)
#endif
                    );
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

    internal static class IReferenceArrayType
    {
        // Gets the IReferenceArray type representation for some built-in known types.
        public static global::System.Type GetTypeAsIReferenceArrayType(global::System.Type type)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                throw new NotSupportedException("Support for 'IReferenceArray<T>' is not enabled.");
            }

            if (type == typeof(int)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<int>);
            if (type == typeof(byte)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<byte>);
            if (type == typeof(bool)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<bool>);
            if (type == typeof(sbyte)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<sbyte>);
            if (type == typeof(short)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<short>);
            if (type == typeof(ushort)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<ushort>);
            if (type == typeof(char)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<char>);
            if (type == typeof(uint)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<uint>);
            if (type == typeof(long)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<long>);
            if (type == typeof(ulong)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<ulong>);
            if (type == typeof(float)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<float>);
            if (type == typeof(double)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<double>);
            if (type == typeof(Guid)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<Guid>);
            if (type == typeof(global::System.TimeSpan)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.TimeSpan>);
            if (type == typeof(global::System.DateTimeOffset)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.DateTimeOffset>);
            if (type == typeof(global::Windows.Foundation.Point)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::Windows.Foundation.Point>);
            if (type == typeof(global::Windows.Foundation.Size)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::Windows.Foundation.Size>);
            if (type == typeof(global::Windows.Foundation.Rect)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::Windows.Foundation.Rect>);
            if (type == typeof(global::System.Numerics.Matrix3x2)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Matrix3x2>);
            if (type == typeof(global::System.Numerics.Matrix4x4)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Matrix4x4>);
            if (type == typeof(global::System.Numerics.Plane)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Plane>);
            if (type == typeof(global::System.Numerics.Quaternion)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Quaternion>);
            if (type == typeof(global::System.Numerics.Vector2)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Vector2>);
            if (type == typeof(global::System.Numerics.Vector3)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Vector3>);
            if (type == typeof(global::System.Numerics.Vector4)) return typeof(global::ABI.Windows.Foundation.IReferenceArray<global::System.Numerics.Vector4>);

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

        public static Func<IInspectable, object> GetValueFactory(global::System.Type type)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                throw new NotSupportedException("Support for 'IReferenceArray<T>' is not enabled.");
            }

            return ComWrappersSupport.CreateReferenceCachingFactory(GetValueFactoryInternal(type));
        }

        private static Func<IInspectable, object> GetValueFactoryInternal(global::System.Type type)
        {
            if (type == typeof(string)) return GetStringValue;
            if (type == typeof(int)) return GetBlittableValue<int>;
            if (type == typeof(byte)) return GetBlittableValue<byte>;
            if (type == typeof(bool)) return GetBlittableValue<bool>;
            if (type == typeof(sbyte)) return GetBlittableValue<sbyte>;
            if (type == typeof(short)) return GetBlittableValue<short>;
            if (type == typeof(ushort)) return GetBlittableValue<ushort>;
            if (type == typeof(char)) return GetBlittableValue<char>;
            if (type == typeof(uint)) return GetBlittableValue<uint>;
            if (type == typeof(long)) return GetBlittableValue<long>;
            if (type == typeof(ulong)) return GetBlittableValue<ulong>;
            if (type == typeof(float)) return GetBlittableValue<float>;
            if (type == typeof(double)) return GetBlittableValue<double>;
            if (type == typeof(Guid)) return GetBlittableValue<Guid>;
            if (type == typeof(global::System.Type)) return GetTypeValue;
            if (type == typeof(global::System.TimeSpan)) return GetNonBlittableValue<global::System.TimeSpan>;
            if (type == typeof(global::System.Exception)) return GetNonBlittableValue<global::System.Exception>;
            if (type == typeof(global::System.DateTimeOffset)) return GetNonBlittableValue<global::System.DateTimeOffset>;
            if (type == typeof(global::Windows.Foundation.Point)) return GetBlittableValue<global::Windows.Foundation.Point>;
            if (type == typeof(global::Windows.Foundation.Size)) return GetBlittableValue<global::Windows.Foundation.Size>;
            if (type == typeof(global::Windows.Foundation.Rect)) return GetBlittableValue<global::Windows.Foundation.Rect>;
            if (type == typeof(global::System.Numerics.Matrix3x2)) return GetBlittableValue<global::System.Numerics.Matrix3x2>;
            if (type == typeof(global::System.Numerics.Matrix4x4)) return GetBlittableValue<global::System.Numerics.Matrix4x4>;
            if (type == typeof(global::System.Numerics.Plane)) return GetBlittableValue<global::System.Numerics.Plane>;
            if (type == typeof(global::System.Numerics.Quaternion)) return GetBlittableValue<global::System.Numerics.Quaternion>;
            if (type == typeof(global::System.Numerics.Vector2)) return GetBlittableValue<global::System.Numerics.Vector2>;
            if (type == typeof(global::System.Numerics.Vector3)) return GetBlittableValue<global::System.Numerics.Vector3>;
            if (type == typeof(global::System.Numerics.Vector4)) return GetBlittableValue<global::System.Numerics.Vector4>;
            if (type == typeof(global::System.EventHandler)) return GetEventHandlerValue;
            if (type == typeof(global::System.ComponentModel.PropertyChangedEventHandler)) return GetPropertyChangedEventHandlerValue;
            if (type == typeof(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler)) return GetNotifyCollectionChangedEventHandlerValue;
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

        private static unsafe object GetBlittableValue<T>(IInspectable inspectable) where T : unmanaged
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IReferenceArray<T>.PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IReferenceArray<T>.PIID), out referenceArrayPtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return MarshalBlittable<T>.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                MarshalBlittable<T>.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }

        private static unsafe object GetStringValue(IInspectable inspectable)
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IReferenceArray<string>.PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IReferenceArray<string>.PIID), out referenceArrayPtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return MarshalString.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                MarshalString.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }

        private static unsafe object GetTypeValue(IInspectable inspectable)
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IReferenceArray<string>.PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IReferenceArray<string>.PIID), out referenceArrayPtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return ABI.System.Type.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                ABI.System.Type.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }

        private static unsafe object GetNonBlittableValue<T>(IInspectable inspectable)
        {
#if NET
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException($"Cannot handle array marshalling for non blittable type '{typeof(T)}'.");
            }
#endif

            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IReferenceArray<string>.PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IReferenceArray<string>.PIID), out referenceArrayPtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return MarshalNonBlittable<T>.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                MarshalNonBlittable<T>.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }

        private static unsafe object GetEventHandlerValue(IInspectable inspectable)
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IReferenceArray<string>.PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IReferenceArray<string>.PIID), out referenceArrayPtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return ABI.System.EventHandler.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                ABI.System.EventHandler.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }

        private static unsafe object GetPropertyChangedEventHandlerValue(IInspectable inspectable)
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IReferenceArray<string>.PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IReferenceArray<string>.PIID), out referenceArrayPtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return ABI.System.ComponentModel.PropertyChangedEventHandler.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                ABI.System.ComponentModel.PropertyChangedEventHandler.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }

        private static unsafe object GetNotifyCollectionChangedEventHandlerValue(IInspectable inspectable)
        {
            IntPtr referenceArrayPtr = IntPtr.Zero;
            int __retval_length = default;
            IntPtr __retval_data = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(
#if NET8_0_OR_GREATER
                    Marshal.QueryInterface(inspectable.ThisPtr, in IReferenceArray<string>.PIID, out referenceArrayPtr)
#else
                    Marshal.QueryInterface(inspectable.ThisPtr, ref Unsafe.AsRef(in IReferenceArray<string>.PIID), out referenceArrayPtr)
#endif
                    );
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>**)referenceArrayPtr)[6](referenceArrayPtr, &__retval_length, &__retval_data));
                return ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.FromAbiArray((__retval_length, __retval_data));
            }
            finally
            {
                ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.DisposeAbiArray((__retval_length, __retval_data));
                Marshal.Release(referenceArrayPtr);
            }
        }
    }
}
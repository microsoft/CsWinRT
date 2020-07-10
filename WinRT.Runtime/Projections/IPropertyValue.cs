using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Windows.Foundation
{
    internal enum PropertyType : uint
    {
        Empty = 0,
        UInt8 = 0x1,
        Int16 = 0x2,
        UInt16 = 0x3,
        Int32 = 0x4,
        UInt32 = 0x5,
        Int64 = 0x6,
        UInt64 = 0x7,
        Single = 0x8,
        Double = 0x9,
        Char16 = 0xa,
        Boolean = 0xb,
        String = 0xc,
        Inspectable = 0xd,
        DateTime = 0xe,
        TimeSpan = 0xf,
        Guid = 0x10,
        Point = 0x11,
        Size = 0x12,
        Rect = 0x13,
        OtherType = 0x14,
        UInt8Array = 0x401,
        Int16Array = 0x402,
        UInt16Array = 0x403,
        Int32Array = 0x404,
        UInt32Array = 0x405,
        Int64Array = 0x406,
        UInt64Array = 0x407,
        SingleArray = 0x408,
        DoubleArray = 0x409,
        Char16Array = 0x40a,
        BooleanArray = 0x40b,
        StringArray = 0x40c,
        InspectableArray = 0x40d,
        DateTimeArray = 0x40e,
        TimeSpanArray = 0x40f,
        GuidArray = 0x410,
        PointArray = 0x411,
        SizeArray = 0x412,
        RectArray = 0x413,
        OtherTypeArray = 0x414,
    }

    [Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62")]
    internal interface IPropertyValue
    {
        byte GetUInt8();
        short GetInt16();
        ushort GetUInt16();
        int GetInt32();
        uint GetUInt32();
        long GetInt64();
        ulong GetUInt64();
        float GetSingle();
        double GetDouble();
        char GetChar16();
        bool GetBoolean();
        string GetString();
        Guid GetGuid();
        global::System.DateTimeOffset GetDateTime();
        global::System.TimeSpan GetTimeSpan();
        Point GetPoint();
        Size GetSize();
        Rect GetRect();
        void GetUInt8Array(out byte[] value);
        void GetInt16Array(out short[] value);
        void GetUInt16Array(out ushort[] value);
        void GetInt32Array(out int[] value);
        void GetUInt32Array(out uint[] value);
        void GetInt64Array(out long[] value);
        void GetUInt64Array(out ulong[] value);
        void GetSingleArray(out float[] value);
        void GetDoubleArray(out double[] value);
        void GetChar16Array(out char[] value);
        void GetBooleanArray(out bool[] value);
        void GetStringArray(out string[] value);
        void GetInspectableArray(out object[] value);
        void GetGuidArray(out Guid[] value);
        void GetDateTimeArray(out global::System.DateTimeOffset[] value);
        void GetTimeSpanArray(out global::System.TimeSpan[] value);
        void GetPointArray(out Point[] value);
        void GetSizeArray(out Size[] value);
        void GetRectArray(out Rect[] value);
        bool IsNumericScalar { get; }
        PropertyType Type { get; }
    }
}

namespace ABI.Windows.Foundation
{
    internal static class ManagedIPropertyValueImpl
    {
        private const int TYPE_E_TYPEMISMATCH = unchecked((int)0x80028CA0);
        private const int DISP_E_OVERFLOW = unchecked((int)0x8002000A);
        private static IPropertyValue.Vftbl AbiToProjectionVftable;
        public static IntPtr AbiToProjectionVftablePtr;

        static unsafe ManagedIPropertyValueImpl()
        {
            AbiToProjectionVftable = new IPropertyValue.Vftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                get_Type_0 = Do_Abi_get_Type_0,
                get_IsNumericScalar_1 = Do_Abi_get_IsNumericScalar_1,
                GetUInt8_2 = Do_Abi_GetUInt8_2,
                GetInt16_3 = Do_Abi_GetInt16_3,
                GetUInt16_4 = Do_Abi_GetUInt16_4,
                GetInt32_5 = Do_Abi_GetInt32_5,
                GetUInt32_6 = Do_Abi_GetUInt32_6,
                GetInt64_7 = Do_Abi_GetInt64_7,
                GetUInt64_8 = Do_Abi_GetUInt64_8,
                GetSingle_9 = Do_Abi_GetSingle_9,
                GetDouble_10 = Do_Abi_GetDouble_10,
                GetChar16_11 = Do_Abi_GetChar16_11,
                GetBoolean_12 = Do_Abi_GetBoolean_12,
                GetString_13 = Do_Abi_GetString_13,
                GetGuid_14 = Do_Abi_GetGuid_14,
                GetDateTime_15 = Do_Abi_GetDateTime_15,
                GetTimeSpan_16 = Do_Abi_GetTimeSpan_16,
                GetPoint_17 = Do_Abi_GetPoint_17,
                GetSize_18 = Do_Abi_GetSize_18,
                GetRect_19 = Do_Abi_GetRect_19,
                GetUInt8Array_20 = Do_Abi_GetUInt8Array_20,
                GetInt16Array_21 = Do_Abi_GetInt16Array_21,
                GetUInt16Array_22 = Do_Abi_GetUInt16Array_22,
                GetInt32Array_23 = Do_Abi_GetInt32Array_23,
                GetUInt32Array_24 = Do_Abi_GetUInt32Array_24,
                GetInt64Array_25 = Do_Abi_GetInt64Array_25,
                GetUInt64Array_26 = Do_Abi_GetUInt64Array_26,
                GetSingleArray_27 = Do_Abi_GetSingleArray_27,
                GetDoubleArray_28 = Do_Abi_GetDoubleArray_28,
                GetChar16Array_29 = Do_Abi_GetChar16Array_29,
                GetBooleanArray_30 = Do_Abi_GetBooleanArray_30,
                GetStringArray_31 = Do_Abi_GetStringArray_31,
                GetInspectableArray_32 = Do_Abi_GetInspectableArray_32,
                GetGuidArray_33 = Do_Abi_GetGuidArray_33,
                GetDateTimeArray_34 = Do_Abi_GetDateTimeArray_34,
                GetTimeSpanArray_35 = Do_Abi_GetTimeSpanArray_35,
                GetPointArray_36 = Do_Abi_GetPointArray_36,
                GetSizeArray_37 = Do_Abi_GetSizeArray_37,
                GetRectArray_38 = Do_Abi_GetRectArray_38
            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(ManagedIPropertyValueImpl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 39);
            Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        private static volatile Dictionary<Type, global::Windows.Foundation.PropertyType> s_numericScalarTypes;

        private static Dictionary<Type, global::Windows.Foundation.PropertyType> NumericScalarTypes
        {
            get
            {
                if (s_numericScalarTypes == null)
                {
                    var numericScalarTypes = new Dictionary<Type, global::Windows.Foundation.PropertyType> {
                        { typeof(byte), global::Windows.Foundation.PropertyType.UInt8 },
                        { typeof(short), global::Windows.Foundation.PropertyType.Int16 },
                        { typeof(ushort), global::Windows.Foundation.PropertyType.UInt16 },
                        { typeof(int), global::Windows.Foundation.PropertyType.Int32 },
                        { typeof(uint), global::Windows.Foundation.PropertyType.UInt32 },
                        { typeof(long), global::Windows.Foundation.PropertyType.Int64 },
                        { typeof(ulong), global::Windows.Foundation.PropertyType.UInt64 },
                        { typeof(float), global::Windows.Foundation.PropertyType.Single },
                        { typeof(double), global::Windows.Foundation.PropertyType.Double }
                    };

                    s_numericScalarTypes = numericScalarTypes;
                }

                return s_numericScalarTypes;
            }
        }

        /// <summary>
        /// Unbox a value of a projected Windows.Foundation struct type
        /// to a structurally equivalent type with the same name.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The object to unbox.</param>
        /// <returns>The unboxed value.</returns>
        private static T UnboxValue<T>(object value)
            where T : struct
        {
            Type valueType = value.GetType();

            if (valueType.FullName == typeof(T).FullName && Marshal.SizeOf(valueType) == Marshal.SizeOf<T>())
            {
                return Unsafe.As<Boxed<T>>(value).Value;
            }

            throw new InvalidCastException("", TYPE_E_TYPEMISMATCH);
        }

#pragma warning disable CS0649
        private class Boxed<T>
            where T : struct
        {
            public T Value;
        }
#pragma warning restore CS0649

        private static T[] UnboxArray<T>(object value)
            where T : struct
        {
            if (!(value is Array dataArray))
            {
                throw new InvalidCastException();
            }

            // If we do not have the correct array type, then we need to convert the array element-by-element
            // to a new array of the requested type
            T[] coercedArray = new T[dataArray.Length];
            for (int i = 0; i < dataArray.Length; ++i)
            {
                try
                {
                    coercedArray[i] = UnboxValue<T>(dataArray.GetValue(i));
                }
                catch (InvalidCastException elementCastException)
                {
                    //global::System.Exception e = new InvalidCastException(string.Format(SR.InvalidCast_WinRTIPropertyValueArrayCoersion, value.GetType(), typeof(T[]).Name, i, elementCastException.Message), elementCastException.HResult);
                    global::System.Exception e = new InvalidCastException("", elementCastException.HResult);
                    throw e;
                }
            }
            return coercedArray;
        }

        private static bool IsCoercable(object value)
        {
            // String <--> Guid is allowed
            // Converting from an object to a string, Guid, or numeric scalar is allowed.
            if (value.GetType() == typeof(string) || value.GetType() == typeof(Guid) || value.GetType() != typeof(object))
            {
                return true;
            }

            // All numeric scalars can also be coerced
            return NumericScalarTypes.TryGetValue(value.GetType(), out _);
        }

        /// <summary>
        /// Coerce the managd object to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private static T CoerceValue<T>(object value)
        {
            if (value is T u)
            {
                return u;
            }

            if (value is global::Windows.Foundation.IPropertyValue ipv)
            {
                if (typeof(T) == typeof(byte))
                {
                    return (T)(object)ipv.GetUInt8();
                }
                if (typeof(T) == typeof(short))
                {
                    return (T)(object)ipv.GetInt16();
                }
                if (typeof(T) == typeof(ushort))
                {
                    return (T)(object)ipv.GetUInt16();
                }
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)ipv.GetInt32();
                }
                if (typeof(T) == typeof(uint))
                {
                    return (T)(object)ipv.GetUInt32();
                }
                if (typeof(T) == typeof(long))
                {
                    return (T)(object)ipv.GetInt64();
                }
                if (typeof(T) == typeof(ulong))
                {
                    return (T)(object)ipv.GetUInt64();
                }
                if (typeof(T) == typeof(float))
                {
                    return (T)(object)ipv.GetSingle();
                }
                if (typeof(T) == typeof(double))
                {
                    return (T)(object)ipv.GetDouble();
                }
            }

            if (!IsCoercable(value))
            {
                throw new InvalidCastException();
            }

            try
            {
                if (value is string str && typeof(T) == typeof(Guid))
                {
                    return (T)(object)Guid.Parse(str);
                }
                else if (value is Guid guid && typeof(T) == typeof(string))
                {
                    return (T)(object)guid.ToString("D", global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    if (NumericScalarTypes.TryGetValue(typeof(T), out _))
                    {
                        return (T)Convert.ChangeType(value, typeof(T), global::System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (FormatException)
            {
                // throw new InvalidCastException(string.Format(SR.InvalidCast_WinRTIPropertyValueElement, value.GetType(), typeof(T).Name), TYPE_E_TYPEMISMATCH);
                throw new InvalidCastException("", TYPE_E_TYPEMISMATCH);
            }
            catch (InvalidCastException)
            {
                // throw new InvalidCastException(string.Format(SR.InvalidCast_WinRTIPropertyValueElement, value.GetType(), typeof(T).Name), TYPE_E_TYPEMISMATCH);
                throw new InvalidCastException("", TYPE_E_TYPEMISMATCH);
            }
            catch (OverflowException)
            {
                // throw new InvalidCastException(string.Format(SR.InvalidCast_WinRTIPropertyValueCoersion, value.GetType(), value, typeof(T).Name), DISP_E_OVERFLOW);
                throw new InvalidCastException("", DISP_E_OVERFLOW);
            }

            throw new InvalidCastException();
        }

        private static T[] CoerceArray<T>(object value)
        {
            if (value is T[] arr)
            {
                return arr;
            }

            if (!(value is Array dataArray))
            {
                throw new InvalidCastException();
            }

            // If we do not have the correct array type, then we need to convert the array element-by-element
            // to a new array of the requested type
            T[] coercedArray = new T[dataArray.Length];
            for (int i = 0; i < dataArray.Length; ++i)
            {
                try
                {
                    coercedArray[i] = CoerceValue<T>(dataArray.GetValue(i));
                }
                catch (InvalidCastException elementCastException)
                {
                    //global::System.Exception e = new InvalidCastException(string.Format(SR.InvalidCast_WinRTIPropertyValueArrayCoersion, value.GetType(), typeof(T[]).Name, i, elementCastException.Message), elementCastException.HResult);
                    global::System.Exception e = new InvalidCastException("", elementCastException.HResult);
                    throw e;
                }
            }
            return coercedArray;
        }

        private static unsafe int Do_Abi_GetUInt8_2(IntPtr thisPtr, out byte value)
        {
            value = default;
            try
            {
                value = CoerceValue<byte>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetInt16_3(IntPtr thisPtr, out short value)
        {
            value = default;
            try
            {
                value = CoerceValue<short>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetUInt16_4(IntPtr thisPtr, out ushort value)
        {
            value = default;
            try
            {
                value = CoerceValue<ushort>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetInt32_5(IntPtr thisPtr, out int value)
        {
            value = default;
            try
            {
                value = CoerceValue<int>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetUInt32_6(IntPtr thisPtr, out uint value)
        {
            value = default;
            try
            {
                value = CoerceValue<uint>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetInt64_7(IntPtr thisPtr, out long value)
        {
            value = default;
            try
            {
                value = CoerceValue<long>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetUInt64_8(IntPtr thisPtr, out ulong value)
        {
            value = default;
            try
            {
                value = CoerceValue<ulong>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetSingle_9(IntPtr thisPtr, out float value)
        {
            value = default;
            try
            {
                value = CoerceValue<float>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetDouble_10(IntPtr thisPtr, out double value)
        {
            value = default;
            try
            {
                value = CoerceValue<double>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetChar16_11(IntPtr thisPtr, out ushort value)
        {
            value = default;
            try
            {
                value = (ushort)CoerceValue<char>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetBoolean_12(IntPtr thisPtr, out byte value)
        {
            value = default;
            try
            {
                value = (byte)(CoerceValue<bool>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)) ? 1 : 0);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetString_13(IntPtr thisPtr, out IntPtr value)
        {
            value = default;
            try
            {
                value = MarshalString.FromManaged(CoerceValue<string>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetGuid_14(IntPtr thisPtr, out Guid value)
        {
            value = default;
            try
            {
                value = CoerceValue<Guid>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetDateTime_15(IntPtr thisPtr, out global::ABI.System.DateTimeOffset value)
        {
            value = default;
            try
            {
                value = ABI.System.DateTimeOffset.FromManaged(CoerceValue<global::System.DateTimeOffset>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetTimeSpan_16(IntPtr thisPtr, out global::ABI.System.TimeSpan value)
        {
            value = default;
            try
            {
                value = ABI.System.TimeSpan.FromManaged(CoerceValue<global::System.TimeSpan>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetPoint_17(IntPtr thisPtr, out global::Windows.Foundation.Point value)
        {
            value = default;
            try
            {
                value = UnboxValue<global::Windows.Foundation.Point>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetSize_18(IntPtr thisPtr, out global::Windows.Foundation.Size value)
        {
            value = default;
            try
            {
                value = UnboxValue<global::Windows.Foundation.Size>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetRect_19(IntPtr thisPtr, out global::Windows.Foundation.Rect value)
        {
            value = default;
            try
            {
                value = UnboxValue<global::Windows.Foundation.Rect>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetUInt8Array_20(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            byte[] __value = default;

            try
            {
                __value = CoerceArray<byte>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<byte>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetInt16Array_21(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            short[] __value = default;

            try
            {
                __value = CoerceArray<short>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<short>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetUInt16Array_22(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            ushort[] __value = default;

            try
            {
                __value = CoerceArray<ushort>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<ushort>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetInt32Array_23(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            int[] __value = default;

            try
            {
                __value = CoerceArray<int>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<int>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetUInt32Array_24(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            uint[] __value = default;

            try
            {
                __value = CoerceArray<uint>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<uint>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetInt64Array_25(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            long[] __value = default;

            try
            {
                __value = CoerceArray<long>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<long>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetUInt64Array_26(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            ulong[] __value = default;

            try
            {
                __value = CoerceArray<ulong>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<ulong>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetSingleArray_27(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            float[] __value = default;

            try
            {
                __value = CoerceArray<float>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<float>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetDoubleArray_28(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            double[] __value = default;

            try
            {
                __value = CoerceArray<double>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<double>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetChar16Array_29(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            char[] __value = default;

            try
            {
                __value = CoerceArray<char>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalNonBlittable<char>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetBooleanArray_30(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            bool[] __value = default;

            try
            {
                __value = CoerceArray<bool>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalNonBlittable<bool>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetStringArray_31(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            string[] __value = default;

            try
            {
                __value = CoerceArray<string>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalString.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetInspectableArray_32(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            object[] __value = default;

            try
            {
                __value = CoerceArray<object>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalInspectable.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetGuidArray_33(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            Guid[] __value = default;

            try
            {
                __value = CoerceArray<Guid>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<Guid>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetDateTimeArray_34(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            global::System.DateTimeOffset[] __value = default;

            try
            {
                __value = CoerceArray<global::System.DateTimeOffset>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalNonBlittable<global::System.DateTimeOffset>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetTimeSpanArray_35(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            global::System.TimeSpan[] __value = default;

            try
            {
                __value = CoerceArray<global::System.TimeSpan>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalNonBlittable<global::System.TimeSpan>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetPointArray_36(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            global::Windows.Foundation.Point[] __value = default;

            try
            {
                __value = UnboxArray<global::Windows.Foundation.Point>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<global::Windows.Foundation.Point>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetSizeArray_37(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            global::Windows.Foundation.Size[] __value = default;

            try
            {
                __value = UnboxArray<global::Windows.Foundation.Size>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<global::Windows.Foundation.Size>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetRectArray_38(IntPtr thisPtr, out int __valueSize, out IntPtr value)
        {

            value = default;
            __valueSize = default;
            global::Windows.Foundation.Rect[] __value = default;

            try
            {
                __value = UnboxArray<global::Windows.Foundation.Rect>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (__valueSize, value) = MarshalBlittable<global::Windows.Foundation.Rect>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_get_IsNumericScalar_1(IntPtr thisPtr, out byte value)
        {
            value = default;
            try
            {
                value = (byte)(NumericScalarTypes.TryGetValue(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).GetType(), out _) ? 1 : 0);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_get_Type_0(IntPtr thisPtr, out global::Windows.Foundation.PropertyType value)
        {
            value = default;
            try
            {
                value = GetPropertyTypeOfObject(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe global::Windows.Foundation.PropertyType GetPropertyTypeOfObject(object obj)
        {
            global::Windows.Foundation.PropertyType value;
            global::System.Type managedType = obj.GetType();
            bool isArray = managedType.IsArray;
            if (isArray)
            {
                managedType = managedType.GetElementType();
            }
            if (!NumericScalarTypes.TryGetValue(managedType, out value))
            {
                if (managedType == typeof(string))
                {
                    value = global::Windows.Foundation.PropertyType.String;
                }
                else if (managedType == typeof(char))
                {
                    value = global::Windows.Foundation.PropertyType.Char16;
                }
                else if (managedType == typeof(bool))
                {
                    value = global::Windows.Foundation.PropertyType.Boolean;
                }
                else if (managedType == typeof(global::System.DateTimeOffset))
                {
                    value = global::Windows.Foundation.PropertyType.DateTime;
                }
                else if (managedType == typeof(global::System.TimeSpan))
                {
                    value = global::Windows.Foundation.PropertyType.TimeSpan;
                }
                else if (managedType == typeof(global::System.Guid))
                {
                    value = global::Windows.Foundation.PropertyType.Guid;
                }
                else if (managedType.FullName == "Windows.Foundation.Point")
                {
                    value = global::Windows.Foundation.PropertyType.Point;
                }
                else if (managedType.FullName == "Windows.Foundation.Rect")
                {
                    value = global::Windows.Foundation.PropertyType.Rect;
                }
                else if (managedType.FullName == "Windows.Foundation.Size")
                {
                    value = global::Windows.Foundation.PropertyType.Size;
                }
                else if (managedType == typeof(object))
                {
                    value = global::Windows.Foundation.PropertyType.Inspectable;
                }
                else if (typeof(Delegate).IsAssignableFrom(managedType))
                {
                    value = global::Windows.Foundation.PropertyType.OtherType;
                }
                else if (!managedType.IsValueType && managedType != typeof(Type) && isArray)
                {
                    // Treat arrays of interfaces as though they are arrays of object.
                    value = global::Windows.Foundation.PropertyType.Inspectable;
                }
                else
                {
                    value = global::Windows.Foundation.PropertyType.OtherType;
                }
            }
            if (isArray)
            {
                // The array values for Windows.Foundation.PropertyType are all 1024 above their scalar equivalents
                value = (global::Windows.Foundation.PropertyType)((int)value + 1024);
            }

            return value;
        }
    }



    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62")]
    internal class IPropertyValue : global::Windows.Foundation.IPropertyValue
    {
        [Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public IPropertyValue_Delegates.get_Type_0 get_Type_0;
            public _get_PropertyAsBoolean get_IsNumericScalar_1;
            public IPropertyValue_Delegates.GetUInt8_2 GetUInt8_2;
            public IPropertyValue_Delegates.GetInt16_3 GetInt16_3;
            public IPropertyValue_Delegates.GetUInt16_4 GetUInt16_4;
            public IPropertyValue_Delegates.GetInt32_5 GetInt32_5;
            public IPropertyValue_Delegates.GetUInt32_6 GetUInt32_6;
            public IPropertyValue_Delegates.GetInt64_7 GetInt64_7;
            public IPropertyValue_Delegates.GetUInt64_8 GetUInt64_8;
            public IPropertyValue_Delegates.GetSingle_9 GetSingle_9;
            public IPropertyValue_Delegates.GetDouble_10 GetDouble_10;
            public IPropertyValue_Delegates.GetChar16_11 GetChar16_11;
            public IPropertyValue_Delegates.GetBoolean_12 GetBoolean_12;
            public IPropertyValue_Delegates.GetString_13 GetString_13;
            public IPropertyValue_Delegates.GetGuid_14 GetGuid_14;
            public IPropertyValue_Delegates.GetDateTime_15 GetDateTime_15;
            public IPropertyValue_Delegates.GetTimeSpan_16 GetTimeSpan_16;
            public IPropertyValue_Delegates.GetPoint_17 GetPoint_17;
            public IPropertyValue_Delegates.GetSize_18 GetSize_18;
            public IPropertyValue_Delegates.GetRect_19 GetRect_19;
            public IPropertyValue_Delegates.GetUInt8Array_20 GetUInt8Array_20;
            public IPropertyValue_Delegates.GetInt16Array_21 GetInt16Array_21;
            public IPropertyValue_Delegates.GetUInt16Array_22 GetUInt16Array_22;
            public IPropertyValue_Delegates.GetInt32Array_23 GetInt32Array_23;
            public IPropertyValue_Delegates.GetUInt32Array_24 GetUInt32Array_24;
            public IPropertyValue_Delegates.GetInt64Array_25 GetInt64Array_25;
            public IPropertyValue_Delegates.GetUInt64Array_26 GetUInt64Array_26;
            public IPropertyValue_Delegates.GetSingleArray_27 GetSingleArray_27;
            public IPropertyValue_Delegates.GetDoubleArray_28 GetDoubleArray_28;
            public IPropertyValue_Delegates.GetChar16Array_29 GetChar16Array_29;
            public IPropertyValue_Delegates.GetBooleanArray_30 GetBooleanArray_30;
            public IPropertyValue_Delegates.GetStringArray_31 GetStringArray_31;
            public IPropertyValue_Delegates.GetInspectableArray_32 GetInspectableArray_32;
            public IPropertyValue_Delegates.GetGuidArray_33 GetGuidArray_33;
            public IPropertyValue_Delegates.GetDateTimeArray_34 GetDateTimeArray_34;
            public IPropertyValue_Delegates.GetTimeSpanArray_35 GetTimeSpanArray_35;
            public IPropertyValue_Delegates.GetPointArray_36 GetPointArray_36;
            public IPropertyValue_Delegates.GetSizeArray_37 GetSizeArray_37;
            public IPropertyValue_Delegates.GetRectArray_38 GetRectArray_38;
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IPropertyValue(IObjectReference obj) => (obj != null) ? new IPropertyValue(obj) : null;
        public static implicit operator IPropertyValue(ObjectReference<Vftbl> obj) => (obj != null) ? new IPropertyValue(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IPropertyValue(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IPropertyValue(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }


        public unsafe byte GetUInt8()
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt8_2(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe short GetInt16()
        {
            short __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt16_3(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe ushort GetUInt16()
        {
            ushort __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt16_4(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe int GetInt32()
        {
            int __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt32_5(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe uint GetUInt32()
        {
            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt32_6(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe long GetInt64()
        {
            long __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt64_7(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe ulong GetUInt64()
        {
            ulong __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt64_8(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe float GetSingle()
        {
            float __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSingle_9(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe double GetDouble()
        {
            double __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetDouble_10(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe char GetChar16()
        {
            ushort __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetChar16_11(ThisPtr, out __retval));
            return (char)__retval;
        }

        public unsafe bool GetBoolean()
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetBoolean_12(ThisPtr, out __retval));
            return __retval != 0;
        }

        public unsafe string GetString()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetString_13(ThisPtr, out __retval));
                return MarshalString.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeAbi(__retval);
            }
        }

        public unsafe Guid GetGuid()
        {
            Guid __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetGuid_14(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe global::System.DateTimeOffset GetDateTime()
        {
            global::ABI.System.DateTimeOffset __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetDateTime_15(ThisPtr, out __retval));
                return global::ABI.System.DateTimeOffset.FromAbi(__retval);
            }
            finally
            {
                global::ABI.System.DateTimeOffset.DisposeAbi(__retval);
            }
        }

        public unsafe global::System.TimeSpan GetTimeSpan()
        {
            global::ABI.System.TimeSpan __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetTimeSpan_16(ThisPtr, out __retval));
                return global::ABI.System.TimeSpan.FromAbi(__retval);
            }
            finally
            {
                global::ABI.System.TimeSpan.DisposeAbi(__retval);
            }
        }

        public unsafe global::Windows.Foundation.Point GetPoint()
        {
            global::Windows.Foundation.Point __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetPoint_17(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe global::Windows.Foundation.Size GetSize()
        {
            global::Windows.Foundation.Size __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSize_18(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe global::Windows.Foundation.Rect GetRect()
        {
            global::Windows.Foundation.Rect __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetRect_19(ThisPtr, out __retval));
            return __retval;
        }

        public unsafe void GetUInt8Array(out byte[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt8Array_20(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<byte>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<byte>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetInt16Array(out short[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt16Array_21(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<short>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<short>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetUInt16Array(out ushort[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt16Array_22(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<ushort>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<ushort>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetInt32Array(out int[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt32Array_23(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<int>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<int>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetUInt32Array(out uint[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt32Array_24(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<uint>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<uint>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetInt64Array(out long[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt64Array_25(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<long>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<long>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetUInt64Array(out ulong[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt64Array_26(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<ulong>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<ulong>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetSingleArray(out float[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSingleArray_27(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<float>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<float>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetDoubleArray(out double[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetDoubleArray_28(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<double>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<double>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetChar16Array(out char[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetChar16Array_29(ThisPtr, out __value_length, out __value_data));
                value = MarshalNonBlittable<char>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalNonBlittable<char>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetBooleanArray(out bool[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetBooleanArray_30(ThisPtr, out __value_length, out __value_data));
                value = MarshalNonBlittable<bool>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalNonBlittable<bool>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetStringArray(out string[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetStringArray_31(ThisPtr, out __value_length, out __value_data));
                value = MarshalString.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalString.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetInspectableArray(out object[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInspectableArray_32(ThisPtr, out __value_length, out __value_data));
                value = MarshalInspectable.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalInspectable.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetGuidArray(out Guid[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetGuidArray_33(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<Guid>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<Guid>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetDateTimeArray(out global::System.DateTimeOffset[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetDateTimeArray_34(ThisPtr, out __value_length, out __value_data));
                value = MarshalNonBlittable<global::System.DateTimeOffset>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalNonBlittable<global::System.DateTimeOffset>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetTimeSpanArray(out global::System.TimeSpan[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetTimeSpanArray_35(ThisPtr, out __value_length, out __value_data));
                value = MarshalNonBlittable<global::System.TimeSpan>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalNonBlittable<global::System.TimeSpan>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetPointArray(out global::Windows.Foundation.Point[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetPointArray_36(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<global::Windows.Foundation.Point>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<global::Windows.Foundation.Point>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetSizeArray(out global::Windows.Foundation.Size[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSizeArray_37(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<global::Windows.Foundation.Size>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<global::Windows.Foundation.Size>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe void GetRectArray(out global::Windows.Foundation.Rect[] value)
        {
            int __value_length = default;
            IntPtr __value_data = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetRectArray_38(ThisPtr, out __value_length, out __value_data));
                value = MarshalBlittable<global::Windows.Foundation.Rect>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalBlittable<global::Windows.Foundation.Rect>.DisposeAbiArray((__value_length, __value_data));
            }
        }

        public unsafe bool IsNumericScalar
        {
            get
            {
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_IsNumericScalar_1(ThisPtr, out __retval));
                return __retval != 0;
            }
        }

        public unsafe global::Windows.Foundation.PropertyType Type
        {
            get
            {
                global::Windows.Foundation.PropertyType __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Type_0(ThisPtr, out __retval));
                return __retval;
            }
        }
    }

    internal static class IPropertyValue_Delegates
    {
        public unsafe delegate int get_Type_0(IntPtr thisPtr, out global::Windows.Foundation.PropertyType value);
        public unsafe delegate int GetUInt8_2(IntPtr thisPtr, out byte value);
        public unsafe delegate int GetInt16_3(IntPtr thisPtr, out short value);
        public unsafe delegate int GetUInt16_4(IntPtr thisPtr, out ushort value);
        public unsafe delegate int GetInt32_5(IntPtr thisPtr, out int value);
        public unsafe delegate int GetUInt32_6(IntPtr thisPtr, out uint value);
        public unsafe delegate int GetInt64_7(IntPtr thisPtr, out long value);
        public unsafe delegate int GetUInt64_8(IntPtr thisPtr, out ulong value);
        public unsafe delegate int GetSingle_9(IntPtr thisPtr, out float value);
        public unsafe delegate int GetDouble_10(IntPtr thisPtr, out double value);
        public unsafe delegate int GetChar16_11(IntPtr thisPtr, out ushort value);
        public unsafe delegate int GetBoolean_12(IntPtr thisPtr, out byte value);
        public unsafe delegate int GetString_13(IntPtr thisPtr, out IntPtr value);
        public unsafe delegate int GetGuid_14(IntPtr thisPtr, out Guid value);
        public unsafe delegate int GetDateTime_15(IntPtr thisPtr, out global::ABI.System.DateTimeOffset value);
        public unsafe delegate int GetTimeSpan_16(IntPtr thisPtr, out global::ABI.System.TimeSpan value);
        public unsafe delegate int GetPoint_17(IntPtr thisPtr, out global::Windows.Foundation.Point value);
        public unsafe delegate int GetSize_18(IntPtr thisPtr, out global::Windows.Foundation.Size value);
        public unsafe delegate int GetRect_19(IntPtr thisPtr, out global::Windows.Foundation.Rect value);
        public unsafe delegate int GetUInt8Array_20(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetInt16Array_21(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetUInt16Array_22(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetInt32Array_23(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetUInt32Array_24(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetInt64Array_25(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetUInt64Array_26(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetSingleArray_27(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetDoubleArray_28(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetChar16Array_29(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetBooleanArray_30(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetStringArray_31(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetInspectableArray_32(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetGuidArray_33(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetDateTimeArray_34(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetTimeSpanArray_35(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetPointArray_36(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetSizeArray_37(IntPtr thisPtr, out int __valueSize, out IntPtr value);
        public unsafe delegate int GetRectArray_38(IntPtr thisPtr, out int __valueSize, out IntPtr value);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

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

        internal static readonly Guid IID = new(0x4BD682DD, 0x7554, 0x40E9, 0x9A, 0x9B, 0x82, 0x65, 0x4E, 0xDE, 0x7E, 0x62);

        private static readonly Delegate[] DelegateCache = new Delegate[39];

        static unsafe ManagedIPropertyValueImpl()
        {
            AbiToProjectionVftable = new IPropertyValue.Vftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                _get_Type_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new IPropertyValue_Delegates.get_Type_0(Do_Abi_get_Type_0)).ToPointer(),
                _get_IsNumericScalar_1 = Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new IPropertyValue_Delegates.get_IsNumericScalar_1(Do_Abi_get_IsNumericScalar_1)).ToPointer(),
                _GetUInt8_2 = Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new IPropertyValue_Delegates.GetUInt8_2(Do_Abi_GetUInt8_2)).ToPointer(),
                _GetInt16_3 = Marshal.GetFunctionPointerForDelegate(DelegateCache[3] = new IPropertyValue_Delegates.GetInt16_3(Do_Abi_GetInt16_3)).ToPointer(),
                _GetUInt16_4 = Marshal.GetFunctionPointerForDelegate(DelegateCache[4] = new IPropertyValue_Delegates.GetUInt16_4(Do_Abi_GetUInt16_4)).ToPointer(),
                _GetInt32_5 = Marshal.GetFunctionPointerForDelegate(DelegateCache[5] = new IPropertyValue_Delegates.GetInt32_5(Do_Abi_GetInt32_5)).ToPointer(),
                _GetUInt32_6 = Marshal.GetFunctionPointerForDelegate(DelegateCache[6] = new IPropertyValue_Delegates.GetUInt32_6(Do_Abi_GetUInt32_6)).ToPointer(),
                _GetInt64_7 = Marshal.GetFunctionPointerForDelegate(DelegateCache[7] = new IPropertyValue_Delegates.GetInt64_7(Do_Abi_GetInt64_7)).ToPointer(),
                _GetUInt64_8 = Marshal.GetFunctionPointerForDelegate(DelegateCache[8] = new IPropertyValue_Delegates.GetUInt64_8(Do_Abi_GetUInt64_8)).ToPointer(),
                _GetSingle_9 = Marshal.GetFunctionPointerForDelegate(DelegateCache[9] = new IPropertyValue_Delegates.GetSingle_9(Do_Abi_GetSingle_9)).ToPointer(),
                _GetDouble_10 = Marshal.GetFunctionPointerForDelegate(DelegateCache[10] = new IPropertyValue_Delegates.GetDouble_10(Do_Abi_GetDouble_10)).ToPointer(),
                _GetChar16_11 = Marshal.GetFunctionPointerForDelegate(DelegateCache[11] = new IPropertyValue_Delegates.GetChar16_11(Do_Abi_GetChar16_11)).ToPointer(),
                _GetBoolean_12 = Marshal.GetFunctionPointerForDelegate(DelegateCache[12] = new IPropertyValue_Delegates.GetBoolean_12(Do_Abi_GetBoolean_12)).ToPointer(),
                _GetString_13 = Marshal.GetFunctionPointerForDelegate(DelegateCache[13] = new IPropertyValue_Delegates.GetString_13(Do_Abi_GetString_13)).ToPointer(),
                _GetGuid_14 = Marshal.GetFunctionPointerForDelegate(DelegateCache[14] = new IPropertyValue_Delegates.GetGuid_14(Do_Abi_GetGuid_14)).ToPointer(),
                _GetDateTime_15 = Marshal.GetFunctionPointerForDelegate(DelegateCache[15] = new IPropertyValue_Delegates.GetDateTime_15(Do_Abi_GetDateTime_15)).ToPointer(),
                _GetTimeSpan_16 = Marshal.GetFunctionPointerForDelegate(DelegateCache[16] = new IPropertyValue_Delegates.GetTimeSpan_16(Do_Abi_GetTimeSpan_16)).ToPointer(),
                _GetPoint_17 = Marshal.GetFunctionPointerForDelegate(DelegateCache[17] = new IPropertyValue_Delegates.GetPoint_17(Do_Abi_GetPoint_17)).ToPointer(),
                _GetSize_18 = Marshal.GetFunctionPointerForDelegate(DelegateCache[18] = new IPropertyValue_Delegates.GetSize_18(Do_Abi_GetSize_18)).ToPointer(),
                _GetRect_19 = Marshal.GetFunctionPointerForDelegate(DelegateCache[19] = new IPropertyValue_Delegates.GetRect_19(Do_Abi_GetRect_19)).ToPointer(),
                _GetUInt8Array_20 = Marshal.GetFunctionPointerForDelegate(DelegateCache[20] = new IPropertyValue_Delegates.GetUInt8Array_20(Do_Abi_GetUInt8Array_20)).ToPointer(),
                _GetInt16Array_21 = Marshal.GetFunctionPointerForDelegate(DelegateCache[21] = new IPropertyValue_Delegates.GetInt16Array_21(Do_Abi_GetInt16Array_21)).ToPointer(),
                _GetUInt16Array_22 = Marshal.GetFunctionPointerForDelegate(DelegateCache[22] = new IPropertyValue_Delegates.GetUInt16Array_22(Do_Abi_GetUInt16Array_22)).ToPointer(),
                _GetInt32Array_23 = Marshal.GetFunctionPointerForDelegate(DelegateCache[23] = new IPropertyValue_Delegates.GetInt32Array_23(Do_Abi_GetInt32Array_23)).ToPointer(),
                _GetUInt32Array_24 = Marshal.GetFunctionPointerForDelegate(DelegateCache[24] = new IPropertyValue_Delegates.GetUInt32Array_24(Do_Abi_GetUInt32Array_24)).ToPointer(),
                _GetInt64Array_25 = Marshal.GetFunctionPointerForDelegate(DelegateCache[25] = new IPropertyValue_Delegates.GetInt64Array_25(Do_Abi_GetInt64Array_25)).ToPointer(),
                _GetUInt64Array_26 = Marshal.GetFunctionPointerForDelegate(DelegateCache[26] = new IPropertyValue_Delegates.GetUInt64Array_26(Do_Abi_GetUInt64Array_26)).ToPointer(),
                _GetSingleArray_27 = Marshal.GetFunctionPointerForDelegate(DelegateCache[27] = new IPropertyValue_Delegates.GetSingleArray_27(Do_Abi_GetSingleArray_27)).ToPointer(),
                _GetDoubleArray_28 = Marshal.GetFunctionPointerForDelegate(DelegateCache[28] = new IPropertyValue_Delegates.GetDoubleArray_28(Do_Abi_GetDoubleArray_28)).ToPointer(),
                _GetChar16Array_29 = Marshal.GetFunctionPointerForDelegate(DelegateCache[29] = new IPropertyValue_Delegates.GetChar16Array_29(Do_Abi_GetChar16Array_29)).ToPointer(),
                _GetBooleanArray_30 = Marshal.GetFunctionPointerForDelegate(DelegateCache[30] = new IPropertyValue_Delegates.GetBooleanArray_30(Do_Abi_GetBooleanArray_30)).ToPointer(),
                _GetStringArray_31 = Marshal.GetFunctionPointerForDelegate(DelegateCache[31] = new IPropertyValue_Delegates.GetStringArray_31(Do_Abi_GetStringArray_31)).ToPointer(),
                _GetInspectableArray_32 = Marshal.GetFunctionPointerForDelegate(DelegateCache[32] = new IPropertyValue_Delegates.GetInspectableArray_32(Do_Abi_GetInspectableArray_32)).ToPointer(),
                _GetGuidArray_33 = Marshal.GetFunctionPointerForDelegate(DelegateCache[33] = new IPropertyValue_Delegates.GetGuidArray_33(Do_Abi_GetGuidArray_33)).ToPointer(),
                _GetDateTimeArray_34 = Marshal.GetFunctionPointerForDelegate(DelegateCache[34] = new IPropertyValue_Delegates.GetDateTimeArray_34(Do_Abi_GetDateTimeArray_34)).ToPointer(),
                _GetTimeSpanArray_35 = Marshal.GetFunctionPointerForDelegate(DelegateCache[35] = new IPropertyValue_Delegates.GetTimeSpanArray_35(Do_Abi_GetTimeSpanArray_35)).ToPointer(),
                _GetPointArray_36 = Marshal.GetFunctionPointerForDelegate(DelegateCache[36] = new IPropertyValue_Delegates.GetPointArray_36(Do_Abi_GetPointArray_36)).ToPointer(),
                _GetSizeArray_37 = Marshal.GetFunctionPointerForDelegate(DelegateCache[37] = new IPropertyValue_Delegates.GetSizeArray_37(Do_Abi_GetSizeArray_37)).ToPointer(),
                _GetRectArray_38 = Marshal.GetFunctionPointerForDelegate(DelegateCache[38] = new IPropertyValue_Delegates.GetRectArray_38(Do_Abi_GetRectArray_38)).ToPointer(),

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
        private sealed class Boxed<T>
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
                else if (typeof(T) == typeof(byte))
                {
                    return (T)(object)Convert.ToByte(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(short))
                {
                    return (T)(object)Convert.ToInt16(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(ushort))
                {
                    return (T)(object)Convert.ToUInt16(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)Convert.ToInt32(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(uint))
                {
                    return (T)(object)Convert.ToUInt32(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(long))
                {
                    return (T)(object)Convert.ToInt64(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(ulong))
                {
                    return (T)(object)Convert.ToUInt64(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(float))
                {
                    return (T)(object)Convert.ToSingle(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)Convert.ToDouble(value, global::System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    Debug.Assert(!NumericScalarTypes.ContainsKey(typeof(T)));
                    throw new InvalidCastException("", TYPE_E_TYPEMISMATCH);
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


        private static unsafe int Do_Abi_GetUInt8_2(IntPtr thisPtr, byte* value)
        {
            try
            {
                *value = CoerceValue<byte>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetInt16_3(IntPtr thisPtr, short* value)
        {
            try
            {
                *value = CoerceValue<short>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetUInt16_4(IntPtr thisPtr, ushort* value)
        {
            try
            {
                *value = CoerceValue<ushort>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetInt32_5(IntPtr thisPtr, int* value)
        {
            try
            {
                *value = CoerceValue<int>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetUInt32_6(IntPtr thisPtr, uint* value)
        {
            try
            {
                *value = CoerceValue<uint>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetInt64_7(IntPtr thisPtr, long* value)
        {
            try
            {
                *value = CoerceValue<long>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetUInt64_8(IntPtr thisPtr, ulong* value)
        {
            try
            {
                *value = CoerceValue<ulong>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetSingle_9(IntPtr thisPtr, float* value)
        {
            try
            {
                *value = CoerceValue<float>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetDouble_10(IntPtr thisPtr, double* value)
        {
            try
            {
                *value = CoerceValue<double>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetChar16_11(IntPtr thisPtr, ushort* value)
        {
            
            try
            {
                *value = (ushort)CoerceValue<char>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetBoolean_12(IntPtr thisPtr, byte* value)
        {
            
            try
            {
                *value = (byte)(CoerceValue<bool>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)) ? 1 : 0);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetString_13(IntPtr thisPtr, IntPtr* value)
        {
            
            try
            {
                *value = MarshalString.FromManaged(CoerceValue<string>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetGuid_14(IntPtr thisPtr, Guid* value)
        {
            
            try
            {
                *value = CoerceValue<Guid>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetDateTime_15(IntPtr thisPtr, global::ABI.System.DateTimeOffset* value)
        {
            
            try
            {
                *value = ABI.System.DateTimeOffset.FromManaged(CoerceValue<global::System.DateTimeOffset>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetTimeSpan_16(IntPtr thisPtr, global::ABI.System.TimeSpan* value)
        {
            
            try
            {
                *value = ABI.System.TimeSpan.FromManaged(CoerceValue<global::System.TimeSpan>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr)));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetPoint_17(IntPtr thisPtr, global::Windows.Foundation.Point* value)
        {
            
            try
            {
                *value = UnboxValue<global::Windows.Foundation.Point>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetSize_18(IntPtr thisPtr, global::Windows.Foundation.Size* value)
        {
            
            try
            {
                *value = UnboxValue<global::Windows.Foundation.Size>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetRect_19(IntPtr thisPtr, global::Windows.Foundation.Rect* value)
        {
            
            try
            {
                *value = UnboxValue<global::Windows.Foundation.Rect>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetUInt8Array_20(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {
            byte[] __value = default;

            try
            {
                __value = CoerceArray<byte>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<byte>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetInt16Array_21(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            short[] __value = default;

            try
            {
                __value = CoerceArray<short>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<short>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetUInt16Array_22(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            ushort[] __value = default;

            try
            {
                __value = CoerceArray<ushort>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<ushort>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetInt32Array_23(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            int[] __value = default;

            try
            {
                __value = CoerceArray<int>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<int>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetUInt32Array_24(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            uint[] __value = default;

            try
            {
                __value = CoerceArray<uint>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<uint>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetInt64Array_25(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            long[] __value = default;

            try
            {
                __value = CoerceArray<long>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<long>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetUInt64Array_26(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            ulong[] __value = default;

            try
            {
                __value = CoerceArray<ulong>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<ulong>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetSingleArray_27(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            float[] __value = default;

            try
            {
                __value = CoerceArray<float>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<float>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetDoubleArray_28(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            double[] __value = default;

            try
            {
                __value = CoerceArray<double>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<double>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetChar16Array_29(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            char[] __value = default;

            try
            {
                __value = CoerceArray<char>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalNonBlittable<char>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetBooleanArray_30(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            bool[] __value = default;

            try
            {
                __value = CoerceArray<bool>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalNonBlittable<bool>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetStringArray_31(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            string[] __value = default;

            try
            {
                __value = CoerceArray<string>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalString.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetInspectableArray_32(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            object[] __value = default;

            try
            {
                __value = CoerceArray<object>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalInspectable<object>.FromManagedArray(__value);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetGuidArray_33(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            Guid[] __value = default;

            try
            {
                __value = CoerceArray<Guid>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<Guid>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetDateTimeArray_34(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            global::System.DateTimeOffset[] __value = default;

            try
            {
                __value = CoerceArray<global::System.DateTimeOffset>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalNonBlittable<global::System.DateTimeOffset>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetTimeSpanArray_35(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            global::System.TimeSpan[] __value = default;

            try
            {
                __value = CoerceArray<global::System.TimeSpan>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalNonBlittable<global::System.TimeSpan>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetPointArray_36(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            global::Windows.Foundation.Point[] __value = default;

            try
            {
                __value = UnboxArray<global::Windows.Foundation.Point>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<global::Windows.Foundation.Point>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetSizeArray_37(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            global::Windows.Foundation.Size[] __value = default;

            try
            {
                __value = UnboxArray<global::Windows.Foundation.Size>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<global::Windows.Foundation.Size>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_GetRectArray_38(IntPtr thisPtr, int* __valueSize, IntPtr* value)
        {

            
            
            global::Windows.Foundation.Rect[] __value = default;

            try
            {
                __value = UnboxArray<global::Windows.Foundation.Rect>(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
                (*__valueSize, *value) = MarshalBlittable<global::Windows.Foundation.Rect>.FromManagedArray(__value);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_get_IsNumericScalar_1(IntPtr thisPtr, byte* value)
        {
            
            try
            {
                *value = (byte)(NumericScalarTypes.TryGetValue(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).GetType(), out _) ? 1 : 0);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }


        private static unsafe int Do_Abi_get_Type_0(IntPtr thisPtr, global::Windows.Foundation.PropertyType* value)
        {
            
            try
            {
                *value = GetPropertyTypeOfObject(global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr));
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
                else if (string.CompareOrdinal(managedType.FullName, "Windows.Foundation.Point") == 0)
                {
                    value = global::Windows.Foundation.PropertyType.Point;
                }
                else if (string.CompareOrdinal(managedType.FullName, "Windows.Foundation.Rect") == 0)
                {
                    value = global::Windows.Foundation.PropertyType.Rect;
                }
                else if (string.CompareOrdinal(managedType.FullName, "Windows.Foundation.Size") == 0)
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
    internal unsafe class IPropertyValue : global::Windows.Foundation.IPropertyValue
    {
        [Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            internal void* _get_Type_0;
            public delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.PropertyType, int> get_Type_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.PropertyType, int>)_get_Type_0; set => _get_Type_0 = value; }
            public void* _get_IsNumericScalar_1;
            public delegate* unmanaged[Stdcall]<IntPtr, out byte, int> get_IsNumericScalar_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, out byte, int>)_get_IsNumericScalar_1; set => _get_IsNumericScalar_1 = value; }
            internal void* _GetUInt8_2;
            public delegate* unmanaged[Stdcall]<IntPtr, out byte, int> GetUInt8_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, out byte, int>)_GetUInt8_2; set => _GetUInt8_2 = value; }
            internal void* _GetInt16_3;
            public delegate* unmanaged[Stdcall]<IntPtr, out short, int> GetInt16_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, out short, int>)_GetInt16_3; set => _GetInt16_3 = value; }
            internal void* _GetUInt16_4;
            public delegate* unmanaged[Stdcall]<IntPtr, out ushort, int> GetUInt16_4 { get => (delegate* unmanaged[Stdcall]<IntPtr, out ushort, int>)_GetUInt16_4; set => _GetUInt16_4 = value; }
            internal void* _GetInt32_5;
            public delegate* unmanaged[Stdcall]<IntPtr, out int, int> GetInt32_5 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int, int>)_GetInt32_5; set => _GetInt32_5 = value; }
            internal void* _GetUInt32_6;
            public delegate* unmanaged[Stdcall]<IntPtr, out uint, int> GetUInt32_6 { get => (delegate* unmanaged[Stdcall]<IntPtr, out uint, int>)_GetUInt32_6; set => _GetUInt32_6 = value; }
            internal void* _GetInt64_7;
            public delegate* unmanaged[Stdcall]<IntPtr, out long, int> GetInt64_7 { get => (delegate* unmanaged[Stdcall]<IntPtr, out long, int>)_GetInt64_7; set => _GetInt64_7 = value; }
            internal void* _GetUInt64_8;
            public delegate* unmanaged[Stdcall]<IntPtr, out ulong, int> GetUInt64_8 { get => (delegate* unmanaged[Stdcall]<IntPtr, out ulong, int>)_GetUInt64_8; set => _GetUInt64_8 = value; }
            internal void* _GetSingle_9;
            public delegate* unmanaged[Stdcall]<IntPtr, out float, int> GetSingle_9 { get => (delegate* unmanaged[Stdcall]<IntPtr, out float, int>)_GetSingle_9; set => _GetSingle_9 = value; }
            internal void* _GetDouble_10;
            public delegate* unmanaged[Stdcall]<IntPtr, out double, int> GetDouble_10 { get => (delegate* unmanaged[Stdcall]<IntPtr, out double, int>)_GetDouble_10; set => _GetDouble_10 = value; }
            internal void* _GetChar16_11;
            public delegate* unmanaged[Stdcall]<IntPtr, out ushort, int> GetChar16_11 { get => (delegate* unmanaged[Stdcall]<IntPtr, out ushort, int>)_GetChar16_11; set => _GetChar16_11 = value; }
            internal void* _GetBoolean_12;
            public delegate* unmanaged[Stdcall]<IntPtr, out byte, int> GetBoolean_12 { get => (delegate* unmanaged[Stdcall]<IntPtr, out byte, int>)_GetBoolean_12; set => _GetBoolean_12 = value; }
            internal void* _GetString_13;
            public delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int> GetString_13 { get => (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)_GetString_13; set => _GetString_13 = value; }
            internal void* _GetGuid_14;
            public delegate* unmanaged[Stdcall]<IntPtr, out Guid, int> GetGuid_14 { get => (delegate* unmanaged[Stdcall]<IntPtr, out Guid, int>)_GetGuid_14; set => _GetGuid_14 = value; }
            internal void* _GetDateTime_15;
            public delegate* unmanaged[Stdcall]<IntPtr, out global::ABI.System.DateTimeOffset, int> GetDateTime_15 { get => (delegate* unmanaged[Stdcall]<IntPtr, out global::ABI.System.DateTimeOffset, int>)_GetDateTime_15; set => _GetDateTime_15 = value; }
            internal void* _GetTimeSpan_16;
            public delegate* unmanaged[Stdcall]<IntPtr, out global::ABI.System.TimeSpan, int> GetTimeSpan_16 { get => (delegate* unmanaged[Stdcall]<IntPtr, out global::ABI.System.TimeSpan, int>)_GetTimeSpan_16; set => _GetTimeSpan_16 = value; }
            internal void* _GetPoint_17;
            public delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.Point, int> GetPoint_17 { get => (delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.Point, int>)_GetPoint_17; set => _GetPoint_17 = value; }
            internal void* _GetSize_18;
            public delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.Size, int> GetSize_18 { get => (delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.Size, int>)_GetSize_18; set => _GetSize_18 = value; }
            internal void* _GetRect_19;
            public delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.Rect, int> GetRect_19 { get => (delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.Foundation.Rect, int>)_GetRect_19; set => _GetRect_19 = value; }
            internal void* _GetUInt8Array_20;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetUInt8Array_20 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetUInt8Array_20; set => _GetUInt8Array_20 = value; }
            internal void* _GetInt16Array_21;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetInt16Array_21 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetInt16Array_21; set => _GetInt16Array_21 = value; }
            internal void* _GetUInt16Array_22;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetUInt16Array_22 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetUInt16Array_22; set => _GetUInt16Array_22 = value; }
            internal void* _GetInt32Array_23;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetInt32Array_23 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetInt32Array_23; set => _GetInt32Array_23 = value; }
            internal void* _GetUInt32Array_24;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetUInt32Array_24 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetUInt32Array_24; set => _GetUInt32Array_24 = value; }
            internal void* _GetInt64Array_25;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetInt64Array_25 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetInt64Array_25; set => _GetInt64Array_25 = value; }
            internal void* _GetUInt64Array_26;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetUInt64Array_26 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetUInt64Array_26; set => _GetUInt64Array_26 = value; }
            internal void* _GetSingleArray_27;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetSingleArray_27 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetSingleArray_27; set => _GetSingleArray_27 = value; }
            internal void* _GetDoubleArray_28;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetDoubleArray_28 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetDoubleArray_28; set => _GetDoubleArray_28 = value; }
            internal void* _GetChar16Array_29;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetChar16Array_29 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetChar16Array_29; set => _GetChar16Array_29 = value; }
            internal void* _GetBooleanArray_30;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetBooleanArray_30 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetBooleanArray_30; set => _GetBooleanArray_30 = value; }
            internal void* _GetStringArray_31;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetStringArray_31 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetStringArray_31; set => _GetStringArray_31 = value; }
            internal void* _GetInspectableArray_32;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetInspectableArray_32 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetInspectableArray_32; set => _GetInspectableArray_32 = value; }
            internal void* _GetGuidArray_33;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetGuidArray_33 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetGuidArray_33; set => _GetGuidArray_33 = value; }
            internal void* _GetDateTimeArray_34;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetDateTimeArray_34 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetDateTimeArray_34; set => _GetDateTimeArray_34 = value; }
            internal void* _GetTimeSpanArray_35;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetTimeSpanArray_35 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetTimeSpanArray_35; set => _GetTimeSpanArray_35 = value; }
            internal void* _GetPointArray_36;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetPointArray_36 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetPointArray_36; set => _GetPointArray_36 = value; }
            internal void* _GetSizeArray_37;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetSizeArray_37 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetSizeArray_37; set => _GetSizeArray_37 = value; }
            internal void* _GetRectArray_38;
            public delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int> GetRectArray_38 { get => (delegate* unmanaged[Stdcall]<IntPtr, out int , out IntPtr, int>)_GetRectArray_38; set => _GetRectArray_38 = value; }
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
                value = MarshalInspectable<object>.FromAbiArray((__value_length, __value_data));
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbiArray((__value_length, __value_data));
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
        public unsafe delegate int get_Type_0(IntPtr thisPtr, global::Windows.Foundation.PropertyType* value);
        public unsafe delegate int get_IsNumericScalar_1(IntPtr thisPtr, byte* value);
        public unsafe delegate int GetUInt8_2(IntPtr thisPtr, byte* value);
        public unsafe delegate int GetInt16_3(IntPtr thisPtr, short* value);
        public unsafe delegate int GetUInt16_4(IntPtr thisPtr, ushort* value);
        public unsafe delegate int GetInt32_5(IntPtr thisPtr, int* value);
        public unsafe delegate int GetUInt32_6(IntPtr thisPtr, uint* value);
        public unsafe delegate int GetInt64_7(IntPtr thisPtr, long* value);
        public unsafe delegate int GetUInt64_8(IntPtr thisPtr, ulong* value);
        public unsafe delegate int GetSingle_9(IntPtr thisPtr, float* value);
        public unsafe delegate int GetDouble_10(IntPtr thisPtr, double* value);
        public unsafe delegate int GetChar16_11(IntPtr thisPtr, ushort* value);
        public unsafe delegate int GetBoolean_12(IntPtr thisPtr, byte* value);
        public unsafe delegate int GetString_13(IntPtr thisPtr, IntPtr* value);
        public unsafe delegate int GetGuid_14(IntPtr thisPtr, Guid* value);
        public unsafe delegate int GetDateTime_15(IntPtr thisPtr, global::ABI.System.DateTimeOffset* value);
        public unsafe delegate int GetTimeSpan_16(IntPtr thisPtr, global::ABI.System.TimeSpan* value);
        public unsafe delegate int GetPoint_17(IntPtr thisPtr, global::Windows.Foundation.Point* value);
        public unsafe delegate int GetSize_18(IntPtr thisPtr, global::Windows.Foundation.Size* value);
        public unsafe delegate int GetRect_19(IntPtr thisPtr, global::Windows.Foundation.Rect* value);
        public unsafe delegate int GetUInt8Array_20(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetInt16Array_21(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetUInt16Array_22(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetInt32Array_23(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetUInt32Array_24(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetInt64Array_25(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetUInt64Array_26(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetSingleArray_27(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetDoubleArray_28(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetChar16Array_29(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetBooleanArray_30(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetStringArray_31(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetInspectableArray_32(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetGuidArray_33(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetDateTimeArray_34(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetTimeSpanArray_35(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetPointArray_36(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetSizeArray_37(IntPtr thisPtr, int* __valueSize, IntPtr* value);
        public unsafe delegate int GetRectArray_38(IntPtr thisPtr, int* __valueSize, IntPtr* value);
    }
}

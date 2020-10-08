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

                _get_Type_0 = (delegate* unmanaged<IntPtr, global::Windows.Foundation.PropertyType*, int>)&Do_Abi_get_Type_0,
                _get_IsNumericScalar_1 = (delegate* unmanaged<IntPtr, byte*, int>)&Do_Abi_get_IsNumericScalar_1,
                _GetUInt8_2 = (delegate* unmanaged<IntPtr, byte*, int>)&Do_Abi_GetUInt8_2,
                _GetInt16_3 = (delegate* unmanaged<IntPtr, short*, int>)&Do_Abi_GetInt16_3,
                _GetUInt16_4 = (delegate* unmanaged<IntPtr, ushort*, int>)&Do_Abi_GetUInt16_4,
                _GetInt32_5 = (delegate* unmanaged<IntPtr, int*, int>)&Do_Abi_GetInt32_5,
                _GetUInt32_6 = (delegate* unmanaged<IntPtr, uint*, int>)&Do_Abi_GetUInt32_6,
                _GetInt64_7 = (delegate* unmanaged<IntPtr, long*, int>)&Do_Abi_GetInt64_7,
                _GetUInt64_8 = (delegate* unmanaged<IntPtr, ulong*, int>)&Do_Abi_GetUInt64_8,
                _GetSingle_9 = (delegate* unmanaged<IntPtr, float*, int>)&Do_Abi_GetSingle_9,
                _GetDouble_10 = (delegate* unmanaged<IntPtr, double*, int>)&Do_Abi_GetDouble_10,
                _GetChar16_11 = (delegate* unmanaged<IntPtr, ushort*, int>)&Do_Abi_GetChar16_11,
                _GetBoolean_12 = (delegate* unmanaged<IntPtr, byte*, int>)&Do_Abi_GetBoolean_12,
                _GetString_13 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetString_13,
                _GetGuid_14 = (delegate* unmanaged<IntPtr, Guid*, int>)&Do_Abi_GetGuid_14,
                _GetDateTime_15 = (delegate* unmanaged<IntPtr, global::ABI.System.DateTimeOffset*, int>)&Do_Abi_GetDateTime_15,
                _GetTimeSpan_16 = (delegate* unmanaged<IntPtr, global::ABI.System.TimeSpan*, int>)&Do_Abi_GetTimeSpan_16,
                _GetPoint_17 = (delegate* unmanaged<IntPtr, global::Windows.Foundation.Point*, int>)&Do_Abi_GetPoint_17,
                _GetSize_18 = (delegate* unmanaged<IntPtr, global::Windows.Foundation.Size*, int>)&Do_Abi_GetSize_18,
                _GetRect_19 = (delegate* unmanaged<IntPtr, global::Windows.Foundation.Rect*, int>)&Do_Abi_GetRect_19,
                _GetUInt8Array_20 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetUInt8Array_20,
                _GetInt16Array_21 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetInt16Array_21,
                _GetUInt16Array_22 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetUInt16Array_22,
                _GetInt32Array_23 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetInt32Array_23,
                _GetUInt32Array_24 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetUInt32Array_24,
                _GetInt64Array_25 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetInt64Array_25,
                _GetUInt64Array_26 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetUInt64Array_26,
                _GetSingleArray_27 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetSingleArray_27,
                _GetDoubleArray_28 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetDoubleArray_28,
                _GetChar16Array_29 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetChar16Array_29,
                _GetBooleanArray_30 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetBooleanArray_30,
                _GetStringArray_31 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetStringArray_31,
                _GetInspectableArray_32 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetInspectableArray_32,
                _GetGuidArray_33 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetGuidArray_33,
                _GetDateTimeArray_34 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetDateTimeArray_34,
                _GetTimeSpanArray_35 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetTimeSpanArray_35,
                _GetPointArray_36 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetPointArray_36,
                _GetSizeArray_37 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetSizeArray_37,
                _GetRectArray_38 = (delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetRectArray_38,

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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


        [UnmanagedCallersOnly]

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



    [DynamicInterfaceCastableImplementation]
    [Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62")]
    internal unsafe interface IPropertyValue : global::Windows.Foundation.IPropertyValue
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

        unsafe byte global::Windows.Foundation.IPropertyValue.GetUInt8()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt8_2(ThisPtr, out __retval));
            return __retval;
        }

        unsafe short global::Windows.Foundation.IPropertyValue.GetInt16()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            short __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt16_3(ThisPtr, out __retval));
            return __retval;
        }

        unsafe ushort global::Windows.Foundation.IPropertyValue.GetUInt16()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            ushort __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt16_4(ThisPtr, out __retval));
            return __retval;
        }

        unsafe int global::Windows.Foundation.IPropertyValue.GetInt32()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            int __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt32_5(ThisPtr, out __retval));
            return __retval;
        }

        unsafe uint global::Windows.Foundation.IPropertyValue.GetUInt32()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt32_6(ThisPtr, out __retval));
            return __retval;
        }

        unsafe long global::Windows.Foundation.IPropertyValue.GetInt64()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            long __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInt64_7(ThisPtr, out __retval));
            return __retval;
        }

        unsafe ulong global::Windows.Foundation.IPropertyValue.GetUInt64()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            ulong __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetUInt64_8(ThisPtr, out __retval));
            return __retval;
        }

        unsafe float global::Windows.Foundation.IPropertyValue.GetSingle()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            float __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSingle_9(ThisPtr, out __retval));
            return __retval;
        }

        unsafe double global::Windows.Foundation.IPropertyValue.GetDouble()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            double __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetDouble_10(ThisPtr, out __retval));
            return __retval;
        }

        unsafe char global::Windows.Foundation.IPropertyValue.GetChar16()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            ushort __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetChar16_11(ThisPtr, out __retval));
            return (char)__retval;
        }

        unsafe bool global::Windows.Foundation.IPropertyValue.GetBoolean()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetBoolean_12(ThisPtr, out __retval));
            return __retval != 0;
        }

        unsafe string global::Windows.Foundation.IPropertyValue.GetString()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe Guid global::Windows.Foundation.IPropertyValue.GetGuid()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            Guid __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetGuid_14(ThisPtr, out __retval));
            return __retval;
        }

        unsafe global::System.DateTimeOffset global::Windows.Foundation.IPropertyValue.GetDateTime()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe global::System.TimeSpan global::Windows.Foundation.IPropertyValue.GetTimeSpan()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe global::Windows.Foundation.Point global::Windows.Foundation.IPropertyValue.GetPoint()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            global::Windows.Foundation.Point __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetPoint_17(ThisPtr, out __retval));
            return __retval;
        }

        unsafe global::Windows.Foundation.Size global::Windows.Foundation.IPropertyValue.GetSize()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            global::Windows.Foundation.Size __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSize_18(ThisPtr, out __retval));
            return __retval;
        }

        unsafe global::Windows.Foundation.Rect global::Windows.Foundation.IPropertyValue.GetRect()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            global::Windows.Foundation.Rect __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetRect_19(ThisPtr, out __retval));
            return __retval;
        }

        unsafe void global::Windows.Foundation.IPropertyValue.GetUInt8Array(out byte[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetInt16Array(out short[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetUInt16Array(out ushort[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetInt32Array(out int[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetUInt32Array(out uint[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetInt64Array(out long[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetUInt64Array(out ulong[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetSingleArray(out float[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetDoubleArray(out double[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetChar16Array(out char[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetBooleanArray(out bool[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetStringArray(out string[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetInspectableArray(out object[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetGuidArray(out Guid[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetDateTimeArray(out global::System.DateTimeOffset[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetTimeSpanArray(out global::System.TimeSpan[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetPointArray(out global::Windows.Foundation.Point[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetSizeArray(out global::Windows.Foundation.Size[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe void global::Windows.Foundation.IPropertyValue.GetRectArray(out global::Windows.Foundation.Rect[] value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
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

        unsafe bool global::Windows.Foundation.IPropertyValue.IsNumericScalar
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_IsNumericScalar_1(ThisPtr, out __retval));
                return __retval != 0;
            }
        }

        unsafe global::Windows.Foundation.PropertyType global::Windows.Foundation.IPropertyValue.Type
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Foundation.IPropertyValue).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
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

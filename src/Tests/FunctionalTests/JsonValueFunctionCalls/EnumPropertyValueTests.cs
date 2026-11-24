// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace JsonValueFunctionCalls;

internal static unsafe class EnumPropertyValueTests
{
    public static void Run()
    {
        Check(AsyncStatus.Error, 3);
        Check((AsyncStatus)(-1), -1);
        Check((PropertyType)300, 300);
        Check(CollectionChange.ItemInserted, 1);
        Check(NotifyCollectionChangedAction.Add, 0);
        Check((InputStreamOptions)uint.MaxValue, uint.MaxValue);
        Check((Windows.System.VirtualKey)3, 3);
        Check((Windows.Storage.FileAttributes)uint.MaxValue, uint.MaxValue);
    }

    private static void Check(object value, long expected)
    {
        void* unknown = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
        nint propertyValue = 0;

        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)unknown, in WellKnownInterfaceIIDs.IID_IPropertyValue, out propertyValue));
            void** vtable = *(void***)propertyValue;
            PropertyType type = default;
            byte isNumeric = 1;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, PropertyType*, int>)vtable[6])(propertyValue, &type));
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, byte*, int>)vtable[7])(propertyValue, &isNumeric));
            Check(type == PropertyType.OtherType && isNumeric == 0, "Enum property values must retain their OtherType identity.");

            int int32 = 0;
            int hresult = ((delegate* unmanaged[MemberFunction]<nint, int*, int>)vtable[11])(propertyValue, &int32);
            if (expected > int.MaxValue)
            {
                Check(hresult == unchecked((int)0x8002000A), "Out-of-range enum coercion must report DISP_E_OVERFLOW.");
            }
            else
            {
                Marshal.ThrowExceptionForHR(hresult);
                Check(int32 == expected, "Enum GetInt32 returned an incorrect value.");
            }

            uint uint32 = 0;
            hresult = ((delegate* unmanaged[MemberFunction]<nint, uint*, int>)vtable[12])(propertyValue, &uint32);
            if (expected < 0)
            {
                Check(hresult == unchecked((int)0x8002000A), "Negative enum coercion to UInt32 must report DISP_E_OVERFLOW.");
            }
            else
            {
                Marshal.ThrowExceptionForHR(hresult);
                Check(uint32 == (uint)expected, "Enum GetUInt32 returned an incorrect value.");
            }

            long int64 = 0;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, long*, int>)vtable[13])(propertyValue, &int64));
            Check(int64 == expected, "Enum GetInt64 returned an incorrect value.");

            ulong uint64 = 0;
            hresult = ((delegate* unmanaged[MemberFunction]<nint, ulong*, int>)vtable[14])(propertyValue, &uint64);
            if (expected < 0)
            {
                Check(hresult == unchecked((int)0x8002000A), "Negative enum coercion to UInt64 must report DISP_E_OVERFLOW.");
            }
            else
            {
                Marshal.ThrowExceptionForHR(hresult);
                Check(uint64 == (ulong)expected, "Enum GetUInt64 returned an incorrect value.");
            }

            hresult = ((delegate* unmanaged[MemberFunction]<nint, int*, int>)vtable[11])(propertyValue, null);
            Check(hresult == unchecked((int)0x80004003), "Enum getters must reject null output pointers.");
        }
        finally
        {
            WindowsRuntimeMarshal.Free((void*)propertyValue);
            WindowsRuntimeMarshal.Free(unknown);
            GC.KeepAlive(value);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

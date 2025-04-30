// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable IDE0060

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IPropertyValue</c> implementation for managed types.
/// </summary>
/// <remarks>
/// Unlike other "Impl" types, <c>IPropertyValue</c> is implemented in a specialized manner on different types.
/// This type provides shared paths for some implementations, and then some specific full implementations.
/// </remarks>
/// <see href="https://learn.microsoft.com/en-us/uwp/api/windows.foundation.ipropertyvalue"/>
public static unsafe class IPropertyValueImpl
{
    /// <summary>
    /// Shared stub that always throws <see cref="InvalidCastException"/> for any type.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint32array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static int ThrowStubForGetOverloads(void* thisPtr, void* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            throw new InvalidCastException("", WellKnownErrorCodes.TYPE_E_TYPEMISMATCH);
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <summary>
    /// Shared stub that always throws <see cref="InvalidCastException"/> for any array type.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint32array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static int ThrowStubForGetArrayOverloads(void* thisPtr, int* size, void** value)
    {
        if (size == null || value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            throw new InvalidCastException("", WellKnownErrorCodes.TYPE_E_TYPEMISMATCH);
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable IDE0060, IDE1006

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Shared stub that returns <see langword="true"/>.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.isnumericscalar"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT get_IsNumericScalarTrue(void* thisPtr, bool* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = true;

        return WellKnownErrorCodes.S_OK;
    }

    /// <summary>
    /// Shared stub that returns <see langword="false"/>.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.isnumericscalar"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT get_IsNumericScalarFalse(void* thisPtr, bool* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = false;

        return WellKnownErrorCodes.S_OK;
    }

    /// <summary>
    /// Shared stub that always throws <see cref="InvalidCastException"/> for any type.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint32array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT ThrowStubForGetOverloads(void* thisPtr, void* value)
    {
        if (value is null)
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
    internal static HRESULT ThrowStubForGetArrayOverloads(void* thisPtr, int* size, void** value)
    {
        if (size is null || value is null)
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

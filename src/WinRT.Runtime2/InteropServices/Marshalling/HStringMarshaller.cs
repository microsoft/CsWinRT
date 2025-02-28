// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for the Windows Runtime <c>HSTRING</c> type.
/// </summary>
[SupportedOSPlatform("windows6.2")]
public static unsafe class HStringMarshaller
{
    /// <summary>
    /// Converts a <see cref="string"/> value to an unmanaged <c>HSTRING</c>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> value to convert.</param>
    /// <returns>The resulting <c>HSTRING</c>.</returns>
    public static HSTRING ConvertToUnmanaged(string? value)
    {
        if (value is null)
        {
            return (HSTRING)null;
        }

        fixed (char* valuePtr = value)
        {
            HSTRING handle;

            Marshal.ThrowExceptionForHR(WindowsRuntimeImports.WindowsCreateString(valuePtr, (uint)value.Length, &handle));

            return handle;
        }
    }

    /// <summary>
    /// Converts an unmanaged <c>HSTRING</c> to a <see cref="string"/> value.
    /// </summary>
    /// <param name="value">The <c>HSTRING</c> to convert.</param>
    /// <returns>The resulting <see cref="string"/> value.</returns>
    public static string ConvertToManaged(HSTRING value)
    {
        if (value == (HSTRING)null)
        {
            return "";
        }

        uint length;
        char* buffer = WindowsRuntimeImports.WindowsGetStringRawBuffer(value, &length);

        return new(buffer, 0, (int)length);
    }

    /// <summary>
    /// Converts an input <c>HSTRING</c> value to a <see cref="ReadOnlySpan{T}"/> value.
    /// </summary>
    /// <param name="value">The input <c>HSTRING</c> value to marshal.</param>
    /// <returns>The resulting <see cref="ReadOnlySpan{T}"/> value.</returns>
    /// <remarks>
    /// <para>
    /// This method is equivalent to <see cref="ConvertToManaged"/>, but it does not create a new <see cref="string"/> instance.
    /// Doing so makes it zero-allocation, but extra care should be taken by callers to ensure that the returned value does not
    /// escape the scope where the source <c>HSTRING</c> is valid.
    /// </para>
    /// <para>
    /// For instance, if this method is invoked in the scope of a method that receives the <c>HSTRING</c> value as one of
    /// its parameters, the resulting <see cref="ReadOnlySpan{T}"/> is always valid for the scope of such method. But, if
    /// the <c>HSTRING</c> was created by reference in a given scope, the resulting <see cref="ReadOnlySpan{T}"/> value
    /// will also only be valid within such scope, and should not be used outside of it.
    /// </para>
    /// </remarks>
    public static ReadOnlySpan<char> ConvertToManagedUnsafe(HSTRING value)
    {
        if (value == (HSTRING)null)
        {
            return "";
        }

        uint length;
        char* buffer = WindowsRuntimeImports.WindowsGetStringRawBuffer(value, &length);

        return new(buffer, (int)length);
    }

    /// <summary>
    /// Release a given <c>HSTRING</c> value.
    /// </summary>
    /// <param name="value">The <c>HSTRING</c> to free.</param>
    public static void Free(HSTRING value)
    {
        // We technically don't need this check, since 'WindowsDeleteString' can handle 'null' values
        // as well. However, we can check this to avoid going through the native call if not needed.
        if (value == (HSTRING)null)
        {
            return;
        }

        // We can ignore the return value, as this method always returns 'S_OK'
        _ = WindowsRuntimeImports.WindowsDeleteString(value);
    }
}
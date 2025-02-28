// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// Represents the backing header for an <c>HSTRING</c> value passed by reference (without copying).
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/hstring/ns-hstring-hstring_header"/>
public ref struct HStringHeader
{
    /// <summary>
    /// The underlying header for the <c>HSTRING</c> reference.
    /// </summary>
    internal HSTRING_HEADER _header;
}

/// <summary>
/// A marshaller for the Windows Runtime <c>HSTRING</c> type.
/// </summary>
public static unsafe class HStringMarshaller
{
    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> value to an unmanaged <c>HSTRING</c>.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> value to convert.</param>
    /// <returns>The resulting <c>HSTRING</c>.</returns>
    public static HSTRING ConvertToUnmanaged(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
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
    /// Converts a text buffer (eg. a pinned <see cref="string"/>) to a fast-pass <c>HSTRING</c> value.
    /// </summary>
    /// <param name="value">The input text buffer to wrap with the resulting <c>HSTRING</c> value.</param>
    /// <param name="length">
    /// The length of the input text buffer. It should be <see langword="null"/>
    /// if and only if <paramref name="value"/> is <see langword="null"/>.
    /// </param>
    /// <param name="header">
    /// The resulting <see cref="HStringHeader"/> instance. This must be kept in scope as long
    /// as the returned <c>HSTRING</c> value is being used. It is not valid to escape that value,
    /// as the header is required to exist for the fast-pass <c>HSTRING</c> to be valid.
    /// </param>
    /// <returns>The resulting fast-pass <c>HSTRING</c>.</returns>
    /// <remarks>
    /// Because the resulting <c>HSTRING</c> is fast-pass, it is not necessary to call <see cref="Free"/> on it.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winstring/nf-winstring-windowscreatestringreference#remarks"/>
    public static HSTRING ConvertToUnmanagedUnsafe(char* value, int? length, out HStringHeader header)
    {
        // If the value is 'null', just return an empty 'HSTRING'. We don't need
        // to validate the length, as that is always expected to be 'null' as well
        // in this case. It's the caller's responsibility to keep it in sync.
        if (value is null)
        {
            Unsafe.SkipInit(out header);

            return (HSTRING)null;
        }

        fixed (HSTRING_HEADER* headerPtr = &header._header)
        {
            HSTRING result;
            HRESULT hr = WindowsRuntimeImports.WindowsCreateStringReference(
                sourceString: value,
                length: (uint)length.GetValueOrDefault(),
                hstringHeader: headerPtr,
                @string: &result);

            Marshal.ThrowExceptionForHR(hr);

            return result;
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
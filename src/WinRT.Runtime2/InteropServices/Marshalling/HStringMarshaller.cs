// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// Represents a reference to a fast-pass <c>HSTRING</c> value (passed without copying).
/// </summary>
public ref struct HStringReference
{
    /// <summary>
    /// The underlying header for the <c>HSTRING</c> reference.
    /// </summary>
    internal HSTRING_HEADER _header;

    /// <summary>
    /// The fast-pass <c>HSTRING</c> value.
    /// </summary>
    internal HSTRING _hstring;

    /// <summary>
    /// Gets the fast-pass <c>HSTRING</c> value for this reference.
    /// </summary>
    /// <remarks>
    /// It is not valid to escape this value outside of the scope of the current <see cref="HStringReference"/> instance.
    /// </remarks>
    public readonly HSTRING HString => _hstring;
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
    /// <param name="reference">
    /// <para>
    /// The resulting <see cref="HStringReference"/> instance. This must be kept in scope as long
    /// as the <c>HSTRING</c> value retrieved from it is being used. It is not valid to escape that
    /// value, as the reference is required to exist for the fast-pass <c>HSTRING</c> to be valid.
    /// </para>
    /// <para>
    /// This value is returned by <see langword="out"/> and not directly, for performance reasons. The size of this
    /// value is not small, as it contains the backing header for the resulting <c>HSTRING</c> value. Returning it
    /// by <see langword="out"/> allows avoiding unnecessary copies when constructing and returning it.
    /// </para>
    /// </param>
    /// <remarks>
    /// Because the resulting <c>HSTRING</c> is fast-pass, it is not necessary to call <see cref="Free"/> on it.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winstring/nf-winstring-windowscreatestringreference#remarks"/>
    public static void ConvertToUnmanagedUnsafe(char* value, int? length, out HStringReference reference)
    {
        // If the value is 'null', just return an empty 'HSTRING'. We don't need
        // to validate the length, as that is always expected to be 'null' as well
        // in this case. It's the caller's responsibility to keep it in sync.
        if (value is null)
        {
            Unsafe.SkipInit(out reference._header);

            // We explicitly want to make sure the resulting 'HSTRING' is 'null'.
            // The header doesn't need to be initialized, as it won't be used.
            reference._hstring = (HSTRING)null;

            return;
        }

        fixed (HSTRING_HEADER* headerPtr = &reference._header)
        fixed (HSTRING* hstringPtr = &reference._hstring)
        {
            HRESULT hr = WindowsRuntimeImports.WindowsCreateStringReference(
                sourceString: value,
                length: (uint)length.GetValueOrDefault(),
                hstringHeader: headerPtr,
                @string: hstringPtr);

            Marshal.ThrowExceptionForHR(hr);
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
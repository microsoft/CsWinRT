// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for the Windows Runtime <c>HSTRING</c> type.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class HStringMarshaller
{
    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> value to an unmanaged <c>HSTRING</c>.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> value to convert.</param>
    /// <returns>The resulting <c>HSTRING</c>.</returns>
    /// <remarks>It is responsibility of callers to free the returned <c>HSTRING</c> value.</remarks>
    public static HSTRING ConvertToUnmanaged(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return null;
        }

        fixed (char* valuePtr = value)
        {
            HSTRING handle;

            WindowsRuntimeImports.WindowsCreateString(valuePtr, (uint)value.Length, &handle).Assert();

            return handle;
        }
    }

    /// <summary>
    /// Converts a text buffer (eg. a pinned <see cref="string"/>) to a fast-pass <c>HSTRING</c> value.
    /// </summary>
    /// <param name="value">
    /// <para>
    /// The input text buffer to wrap with the resulting <c>HSTRING</c> value.
    /// </para>
    /// <para>
    /// The buffer must be <see langword="null"/>-terminated.
    /// </para>
    /// </param>
    /// <param name="length">
    /// <para>
    /// The length of the input text buffer. It should be <see langword="null"/> if and only if <paramref name="value"/> is <see langword="null"/>.
    /// </para>
    /// <para>
    /// The length should not include the <see langword="null"/> terminator character at the end of <paramref name="value"/>.
    /// </para>
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
    /// <see href="https://learn.microsoft.com/windows/win32/api/winstring/nf-winstring-windowscreatestringreference#remarks"/>
    public static void ConvertToUnmanagedUnsafe(char* value, int? length, out HStringReference reference)
    {
        Unsafe.SkipInit(out reference);

        // If the value is 'null', just return an empty 'HSTRING'. We don't need
        // to validate the length, as that is always expected to be 'null' as well
        // in this case. It's the caller's responsibility to keep it in sync.
        if (value is null)
        {
            // We explicitly want to make sure the resulting 'HSTRING' is 'null'.
            // The header doesn't need to be initialized, as it won't be used.
            reference._hstring = null;

            return;
        }

        // We can avoid pinning here, as 'HStringReference' is a 'ref struct', which means
        // it's guaranteed to be on the stack, and therefore pinned (as in, it can't move).
        // This means that just taking the address is safe, and slightly more efficient.
        HSTRING_HEADER* headerPtr = &((HStringReference*)Unsafe.AsPointer(ref reference))->_header;
        HSTRING* hstringPtr = &((HStringReference*)Unsafe.AsPointer(ref reference))->_hstring;

        // Create the fast-pass 'HSTRING' on the target location. The fact the resulting
        // 'HSTRING' instance points to the header is why it should never be copied too.
        WindowsRuntimeImports.WindowsCreateStringReference(
            sourceString: value,
            length: (uint)length.GetValueOrDefault(),
            hstringHeader: headerPtr,
            @string: hstringPtr).Assert();
    }

    /// <summary>
    /// Converts an array of <see cref="string"/> values to unmanaged fast-pass <c>HSTRING</c> values.
    /// </summary>
    /// <param name="values">
    /// The span of <see cref="string"/> values to convert.
    /// </param>
    /// <param name="hstringHeaderArray">
    /// Pointer to an array of <see cref="HSTRING_HEADER"/> structures, one for each string in <paramref name="values"/>.
    /// This is a pointer to ensure that the array is fixed and the caller should ensure to keep it fixed while using the hstrings.
    /// </param>
    /// <param name="hstrings">
    /// Span to receive the resulting unmanaged <c>HSTRING</c> values. Each element corresponds to a string in <paramref name="values"/>.
    /// </param>
    /// <param name="pinnedGCHandles">
    /// Span to receive the pinned GC handles for each string. These must be disposed by caller by calling <see cref="Dispose"/> after use to release pinned memory.
    /// </param>
    /// <remarks>
    /// This method creates fast-pass <c>HSTRING</c> values for each string in the input array. The caller is responsible for disposing the pinned GC handles.
    /// </remarks>
    public static void ConvertToUnmanagedUnsafe(ReadOnlySpan<string> values, HSTRING_HEADER* hstringHeaderArray, Span<nint> hstrings, Span<nint> pinnedGCHandles)
    {
        int i = 0;
        try
        {
            // Create the fast-pass 'HSTRING' for each string in values.
            foreach (string value in values)
            {
                int idx = i;
                PinnedGCHandle<string> pinnedGCHandle = new(value);
                // We use i to keep track of the gc handle index to dispose up to.
                // Given it is possible getting the GC handle fails, we increment the
                // index after we have gotten it.
                pinnedGCHandles[i++] = PinnedGCHandle<string>.ToIntPtr(pinnedGCHandle);

                // Create the fast-pass 'HSTRING' on the target location.
                HSTRING hstring;
                WindowsRuntimeImports.WindowsCreateStringReference(
                    sourceString: pinnedGCHandle.GetAddressOfStringData(),
                    length: (uint)value.Length,
                    hstringHeader: hstringHeaderArray + idx,
                    @string: &hstring).Assert();
                hstrings[idx] = (nint)hstring;
            }
        }
        catch (Exception)
        {
            for (int j = 0; j < i; j++)
            {
                PinnedGCHandle<string>.FromIntPtr(pinnedGCHandles[j]).Dispose();
            }

            // Given the caller will also try to dispose the handles as part of its cleanup,
            // clear the span if we had freed it as part of exception handling. This allows
            // to make sure we don't run into issues if we hadn't populated the entire span
            // Given this is an edge case, we do the clear here rather than always in the caller.
            pinnedGCHandles.Clear();

            throw;
        }
    }

    /// <summary>
    /// Converts an unmanaged <c>HSTRING</c> to a <see cref="string"/> value.
    /// </summary>
    /// <param name="value">The <c>HSTRING</c> to convert.</param>
    /// <returns>The resulting <see cref="string"/> value.</returns>
    public static string ConvertToManaged(HSTRING value)
    {
        if (value is null)
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
        if (value is null)
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
    /// <remarks>It is responsibility of callers to ensure this method is only called once per <c>HSTRING</c> value.</remarks>
    public static void Free(HSTRING value)
    {
        // We technically don't need this check, since 'WindowsDeleteString' can handle 'null' values
        // as well. However, we can check this to avoid going through the native call if not needed.
        if (value is null)
        {
            return;
        }

        // We can ignore the return value, as this method always returns 'S_OK'
        _ = WindowsRuntimeImports.WindowsDeleteString(value);
    }

    /// <summary>
    /// Releases all pinned GC handles represented by the provided span.
    /// </summary>
    /// <param name="pinnedGCHandles">A span containing nints for the pinned GC handles to be released.</param>
    public static void Dispose(Span<nint> pinnedGCHandles)
    {
        foreach (nint value in pinnedGCHandles)
        {
            PinnedGCHandle<string>.FromIntPtr(value).Dispose();
        }
    }
}
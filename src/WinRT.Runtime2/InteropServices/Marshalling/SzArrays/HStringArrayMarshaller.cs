// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of the Windows Runtime <c>HSTRING</c> type.
/// </summary>
public static unsafe class HStringArrayMarshaller
{
    /// <summary>
    /// Marshals a managed array of <see cref="string"/> values to unmanaged fast-pass <c>HSTRING</c> values.
    /// </summary>
    /// <param name="source">The source array of <see cref="string"/> values.</param>
    /// <param name="hstringHeaders">
    /// A pinned array of <see cref="HStringHeader"/> values, one for each value in <paramref name="source"/>. Note that this parameter
    /// is a pointer and not a <see cref="ReadOnlySpan{T}"/> to enforce that it must be pinned on the caller side before this method is
    /// called. Additionally, callers must ensure that this array is pinned as long as the <paramref name="hstrings"/> values are used.
    /// </param>
    /// <param name="hstrings">The resulting unmanaged <c>HSTRING</c> values. Each element corresponds to a value in <paramref name="source"/>.</param>
    /// <param name="pinnedGCHandles">
    /// The resulting <see cref="PinnedGCHandle{T}"/> values used for pinning values in <paramref name="source"/>. These must be disposed by the caller
    /// when the <paramref name="hstrings"/> values are no longer used, by calling <see cref="Dispose(ReadOnlySpan{nint})"/>. Note that this parameter
    /// uses <see cref="nint"/> and not <see cref="PinnedGCHandle{T}"/> intentionally, to improve array pooling performance for callers (more common type).
    /// </param>
    /// <remarks>
    /// <para>
    /// This method creates fast-pass <c>HSTRING</c> values for each <see cref="string"/> value in <paramref name="source"/>.
    /// </para>
    /// <para>
    /// Callers must ensure that:
    /// <list type="bullet">
    ///   <item>
    ///     The size of <paramref name="source"/>, <paramref name="hstringHeaders"/>, <paramref name="hstrings"/>, and <paramref name="pinnedGCHandles"/>,
    ///     is the same. In particular, if the size of <paramref name="hstringHeaders"/> is incorrect, this will result in undefined behavior.
    ///   </item>
    ///   <item>The <paramref name="hstringHeaders"/> array remains pinned as long as <paramref name="hstrings"/> is in use.</item>
    ///   <item>The values in <paramref name="pinnedGCHandles"/> are disposed by calling <see cref="Dispose(ReadOnlySpan{nint})"/>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static void ConvertToUnmanagedUnsafe(
        ReadOnlySpan<string?> source,
        HStringHeader* hstringHeaders,
        Span<nint> hstrings,
        Span<nint> pinnedGCHandles)
    {
        int i = 0;

        try
        {
            // Create the fast-pass 'HSTRING' for each string in values
            for (; i < source.Length; i++)
            {
                string? value = source[i];

                // If the value is 'null', we can avoid the overhead of creating the GC handle
                // entirely, on top of also skipping the call to 'WindowsCreateStringReference'.
                if (value is null)
                {
                    pinnedGCHandles[i] = default;
                    hstrings[i] = default;
                }
                else
                {
                    PinnedGCHandle<string?> pinnedGCHandle = new(value);

                    // Create the GC handle first to pin the 'string' and store it for later
                    pinnedGCHandles[i] = PinnedGCHandle<string?>.ToIntPtr(pinnedGCHandle);

                    HSTRING hstring;

                    // Create the fast-pass 'HSTRING' value (this is why we need the GC handles)
                    WindowsRuntimeImports.WindowsCreateStringReference(
                        sourceString: pinnedGCHandle.GetAddressOfStringData(),
                        length: (uint)value.Length,
                        hstringHeader: (HSTRING_HEADER*)&hstringHeaders[i],
                        @string: &hstring).Assert();

                    hstrings[i] = (nint)hstring;
                }
            }
        }
        catch
        {
            // We don't need to release the fast-pass 'HSTRING' values in case of exceptions,
            // however we need to make sure that all GC handles are disposed, to avoid leaks.
            for (int j = 0; j < i; j++)
            {
                PinnedGCHandle<string>.FromIntPtr(pinnedGCHandles[j]).Dispose();
            }

            // Because the caller will also try to dispose the GC handles as part of its cleanup,
            // we must clear the span from here in case of exceptions, as we have already disposed
            // all handles ourselves. This ensures that there can't be accidental double disposal
            // of any handles, and that callers don't try to dispose handles that are created over
            // uninitialized memory, leading to undefined behavior. Because this is an edge case,
            // we do this additional cleanup here in the exceptional case, and not always in the caller.
            pinnedGCHandles.Clear();

            throw;
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    public static void ConvertToUnmanaged(ReadOnlySpan<string?> source, out uint size, out void** array)
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        void** destination = (void**)Marshal.AllocCoTaskMem(sizeof(void*) * source.Length);

        int i = 0;

        try
        {
            // Marshal all input 'string'-s with 'HStringMarshaller' (note that 'HSTRING' is not a COM object)
            for (; i < source.Length; i++)
            {
                destination[i] = HStringMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // Make sure to release all marshalled objects so far (this shouldn't ever throw)
            for (int j = 0; j < i; j++)
            {
                HStringMarshaller.Free(destination[j]);
            }

            // Also release the allocated array to avoid leaking
            Marshal.FreeCoTaskMem((nint)destination);

            throw;
        }

        size = (uint)source.Length;
        array = destination;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToManaged"/>
    public static string[] ConvertToManaged(uint size, void** value)
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        string[] array = new string[(int)size];

        for (int i = 0; i < size; i++)
        {
            array[i] = HStringMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    public static void CopyToUnmanaged(ReadOnlySpan<string?> source, uint size, void** destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(source.Length, size);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(destination);

        int i = 0;

        try
        {
            // Marshal the items in the input span
            for (; i < source.Length; i++)
            {
                destination[i] = HStringMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // Release resources for any items, if we failed
            for (int j = 0; j < i; j++)
            {
                HStringMarshaller.Free(destination[j]);
            }

            throw;
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToManaged"/>
    public static void CopyToManaged(uint size, void** source, Span<string> destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(size, destination.Length);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        for (uint i = 0; i < size; i++)
        {
            destination[(int)i] = HStringMarshaller.ConvertToManaged(source[i]);
        }
    }

    /// <summary>
    /// Releases all <see cref="PinnedGCHandle{T}"/> values from a provided span.
    /// </summary>
    /// <param name="pinnedGCHandles">The target <see cref="PinnedGCHandle{T}"/> values to dispose.</param>
    /// <remarks>
    /// This method is meant to be used to dispose <see cref="PinnedGCHandle{T}"/> values created by <see cref="ConvertToUnmanagedUnsafe"/>.
    /// </remarks>
    public static void Dispose(ReadOnlySpan<nint> pinnedGCHandles)
    {
        foreach (nint value in pinnedGCHandles)
        {
            PinnedGCHandle<string>.FromIntPtr(value).Dispose();
        }
    }

    /// <inheritdoc cref="WindowsRuntimeUnknownArrayMarshaller.Dispose"/>
    public static void Dispose(uint size, void** array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (uint i = 0; i < size; i++)
        {
            HStringMarshaller.Free(array[i]);
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller.Free"/>
    public static void Free(uint size, void** array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (uint i = 0; i < size; i++)
        {
            HStringMarshaller.Free(array[i]);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }
}
#endif
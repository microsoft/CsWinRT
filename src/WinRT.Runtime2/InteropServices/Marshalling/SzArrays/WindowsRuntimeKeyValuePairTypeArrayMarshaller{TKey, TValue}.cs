// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of <see cref="KeyValuePair{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeKeyValuePairTypeArrayMarshaller<TKey, TValue>
{
    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void ConvertToUnmanaged<TElementMarshaller>(ReadOnlySpan<KeyValuePair<TKey, TValue>> source, out uint size, out void** array)
        where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>
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
            // Marshal all array elements (the ABI type for 'KeyValuePair<,>' is just 'void*')
            for (; i < source.Length; i++)
            {
                destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]).DetachThisPtrUnsafe();
            }
        }
        catch
        {
            // Release any allocated native 'IKeyValuePair<,>' values
            for (int j = 0; j < i; j++)
            {
                WindowsRuntimeUnknownMarshaller.Free(destination[j]);
            }

            // Also release the allocated array to avoid leaking
            Marshal.FreeCoTaskMem((nint)destination);

            throw;
        }

        size = (uint)source.Length;
        array = destination;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToManaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static KeyValuePair<TKey, TValue>[] ConvertToManaged<TElementMarshaller>(uint size, void** value)
        where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[(int)size];

        for (int i = 0; i < size; i++)
        {
            array[i] = TElementMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToUnmanaged<TElementMarshaller>(ReadOnlySpan<KeyValuePair<TKey, TValue>> source, uint size, void** destination)
        where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>
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
                destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]).DetachThisPtrUnsafe();
            }
        }
        catch
        {
            // Release resources for any items, if we failed
            for (int j = 0; j < i; j++)
            {
                WindowsRuntimeUnknownMarshaller.Free(destination[j]);
            }

            throw;
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToManaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToManaged<TElementMarshaller>(uint size, void** source, Span<KeyValuePair<TKey, TValue>> destination)
        where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(size, destination.Length);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        for (uint i = 0; i < size; i++)
        {
            destination[(int)i] = TElementMarshaller.ConvertToManaged(source[i]);
        }
    }
}
#endif
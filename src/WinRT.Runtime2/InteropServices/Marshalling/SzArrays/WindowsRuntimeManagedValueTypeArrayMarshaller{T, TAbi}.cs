// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of managed Windows Runtime types.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
/// <typeparam name="TAbi">The ABI type for type <typeparamref name="T"/>.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeManagedValueTypeArrayMarshaller<T, TAbi>
    where T : struct
    where TAbi : unmanaged
{
    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void ConvertToUnmanaged<TElementMarshaller>(ReadOnlySpan<T> source, out uint size, out TAbi* array)
        where TElementMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        TAbi* destination = (TAbi*)Marshal.AllocCoTaskMem(sizeof(T) * source.Length);

        int i = 0;

        try
        {
            // Marshal all array elements with the provided element marshaller.
            // Because the native type contains resources, this might throw.
            for (; i < source.Length; i++)
            {
                destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // Make sure to release all native resources for marshalled values
            for (int j = 0; j < i; j++)
            {
                TElementMarshaller.Dispose(destination[j]);
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
    public static T[] ConvertToManaged<TElementMarshaller>(uint size, TAbi* value)
        where TElementMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        T[] array = GC.AllocateUninitializedArray<T>((int)size);

        for (int i = 0; i < size; i++)
        {
            array[i] = TElementMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToUnmanaged<TElementMarshaller>(ReadOnlySpan<T> source, uint size, TAbi* destination)
        where TElementMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>
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
                destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // Release resources for any items, if we failed
            for (int j = 0; j < i; j++)
            {
                TElementMarshaller.Dispose(destination[j]);
            }

            throw;
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToManaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToManaged<TElementMarshaller>(uint size, TAbi* source, Span<T> destination)
        where TElementMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>
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

    /// <inheritdoc cref="WindowsRuntimeUnknownArrayMarshaller.Dispose"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void Dispose<TElementMarshaller>(uint size, TAbi* array)
        where TElementMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (int i = 0; i < size; i++)
        {
            TElementMarshaller.Dispose(array[i]);
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller.Free"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void Free<TElementMarshaller>(uint size, TAbi* array)
        where TElementMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (int i = 0; i < size; i++)
        {
            TElementMarshaller.Dispose(array[i]);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }
}
#endif

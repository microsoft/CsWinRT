// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of the Windows Runtime <c>HSTRING</c> type.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class HStringArrayMarshaller
{
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
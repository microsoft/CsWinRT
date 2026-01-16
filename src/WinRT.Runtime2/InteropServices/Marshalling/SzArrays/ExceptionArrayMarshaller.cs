// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of the Windows Runtime <see cref="Exception"/> type.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class ExceptionArrayMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    public static void ConvertToUnmanaged(ReadOnlySpan<Exception?> source, out uint size, out ABI.System.Exception* array)
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        ABI.System.Exception* destination = (ABI.System.Exception*)Marshal.AllocCoTaskMem(sizeof(ABI.System.Exception) * source.Length);

        try
        {
            // Marshal all input 'Exception'-s with 'ExceptionMarshaller' (note that 'HResult' is blittable)
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = ABI.System.ExceptionMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // Release the allocated array to avoid leaking (this shouldn't really happen)
            Marshal.FreeCoTaskMem((nint)destination);

            throw;
        }

        size = (uint)source.Length;
        array = destination;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToManaged"/>
    public static Exception?[] ConvertToManaged(uint size, ABI.System.Exception* value)
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        Exception?[] array = new Exception[(int)size];

        for (int i = 0; i < size; i++)
        {
            array[i] = ABI.System.ExceptionMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    public static void CopyToUnmanaged(ReadOnlySpan<Exception?> source, uint size, ABI.System.Exception* destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(source.Length, size);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(destination);

        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = ABI.System.ExceptionMarshaller.ConvertToUnmanaged(source[i]);
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToManaged"/>
    public static void CopyToManaged(uint size, ABI.System.Exception* source, Span<Exception?> destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(size, destination.Length);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        for (uint i = 0; i < size; i++)
        {
            destination[(int)i] = ABI.System.ExceptionMarshaller.ConvertToManaged(source[i]);
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller.Free"/>
    public static void Free(uint size, ABI.System.Exception* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        Marshal.FreeCoTaskMem((nint)array);
    }
}
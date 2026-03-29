// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of some Windows Runtime types represented as unknown COM interfaces.
/// </summary>
/// <remarks>
/// This type mirrors <see cref="WindowsRuntimeUnknownMarshaller"/>, but for arrays.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeUnknownArrayMarshaller
{
    /// <summary>
    /// Disposes all values in the specified array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    public static void Dispose(uint size, void** array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (uint i = 0; i < size; i++)
        {
            WindowsRuntimeUnknownMarshaller.Free(array[i]);
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
            WindowsRuntimeUnknownMarshaller.Free(array[i]);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }
}
#endif

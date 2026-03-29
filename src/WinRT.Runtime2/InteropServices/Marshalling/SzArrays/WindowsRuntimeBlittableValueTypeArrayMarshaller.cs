// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of blittable Windows Runtime types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeBlittableValueTypeArrayMarshaller
{
    /// <summary>
    /// Frees the specified array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    public static void Free(uint size, void* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        Marshal.FreeCoTaskMem((nint)array);
    }
}
#endif

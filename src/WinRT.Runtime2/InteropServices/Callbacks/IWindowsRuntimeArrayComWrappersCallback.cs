// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for callbacks for <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>, for Windows Runtime arrays.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public unsafe interface IWindowsRuntimeArrayComWrappersCallback
{
    /// <summary>
    /// Creates a managed Windows Runtime array for a given (unboxed) native array.
    /// </summary>
    /// <param name="count">The length of the native array.</param>
    /// <param name="value">The input native array to marshal.</param>
    /// <returns>The resulting managed Windows Runtime array.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="value"/> parameter may be a specific array pointer depending on the specific
    /// use of each <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementation, as defined by the
    /// invoked method in one of the available marshaller types for CsWinRT. It is guaranteed to always
    /// exactly be a pointer to an array of the exact type expected by callers (and already unboxed).
    /// </para>
    /// <para>
    /// This method will be called from the <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/> method,
    /// so implementations must not call back into <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>
    /// themselves, but rather they should just marshal the object directly, and return it. Calling back into
    /// <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/> is not supported, and it will likely lead
    /// to a stack overflow exception.
    /// </para>
    /// </remarks>
    static abstract Array CreateArray(uint count, void* value);
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IVectorView&lt;T&gt;</c> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IVectorViewMethodsImpl<T>
{
    /// <summary>
    /// Returns the item at the specified index in the vector view.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1.getat"/>
    static abstract T GetAt(WindowsRuntimeObjectReference thisReference, uint index);
}

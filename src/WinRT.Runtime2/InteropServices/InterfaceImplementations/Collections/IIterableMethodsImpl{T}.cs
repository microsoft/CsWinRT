// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIterableMethodsImpl<T>
{
    /// <summary>
    /// Returns an iterator for the items in the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The iterator.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterable-1.first"/>
    static abstract IEnumerator<T> First(WindowsRuntimeObjectReference thisReference);
}
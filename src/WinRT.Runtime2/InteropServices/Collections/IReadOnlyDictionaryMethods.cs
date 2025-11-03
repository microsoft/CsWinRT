// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyDictionaryMethods
{
    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        // The vtable slot for 'get_Size' and the desired semantics are identical between 'IMapView<T>' and 'IVectorView<T>'
        return IReadOnlyListMethods.Count(thisReference);
    }
}

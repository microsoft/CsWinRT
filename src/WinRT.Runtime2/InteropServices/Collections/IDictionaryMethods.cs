// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IDictionaryMethods
{
    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        return IMapMethods.Count(thisReference);
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Clear"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        IMapMethods.Clear(thisReference);
    }
}

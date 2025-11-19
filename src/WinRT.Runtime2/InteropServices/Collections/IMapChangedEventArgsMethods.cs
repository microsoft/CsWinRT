// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IMapChangedEventArgsMethods
{
    /// <summary>
    /// Gets the type of change that occurred in the map.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The type of change in the map.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapchangedeventargs-1.collectionchange"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static CollectionChange CollectionChange(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        CollectionChange result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IMapChangedEventArgsVftbl*)*(void***)thisPtr)->get_CollectionChange(thisPtr, &result));

        return result;
    }
}
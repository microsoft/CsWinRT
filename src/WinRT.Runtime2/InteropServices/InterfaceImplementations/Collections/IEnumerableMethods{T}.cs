// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="IEnumerable{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumerableMethods<T>
{
    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    /// <typeparam name="TMethods">The <see cref="IIterableMethodsImpl{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerator<T> GetEnumerator<TMethods>(WindowsRuntimeObjectReference thisReference)
        where TMethods : IIterableMethodsImpl<T>
    {
        IEnumerator<T> enumerator = TMethods.First(thisReference);

        // If the returned enumerator is an 'IEnumeratorAdapter<T>' instance, just unwrap it and
        // return the original managed iterator. This can happen if an enumerator is marshalled
        // to native on a type that doesn't have marshalling info, and is then reused in some
        // way that causes it to be returned to managed from a native 'IIterable<T>.First' call.
        if (enumerator is IEnumeratorAdapter<T> adapter)
        {
            return adapter.Enumerator;
        }

        // Otherwise, return the enumerator instance directly. This will be either some RCW for a native
        // object implementing 'IEnumerator<T>', or a managed object that was passed to native as a CCW.
        return enumerator;
    }
}
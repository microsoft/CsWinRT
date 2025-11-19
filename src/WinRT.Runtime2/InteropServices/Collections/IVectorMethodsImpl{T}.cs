// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IVectorMethodsImpl<T>
{
    /// <summary>
    /// Returns the item at the specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.getat"/>
    static abstract T GetAt(WindowsRuntimeObjectReference thisReference, uint index);

    /// <summary>
    /// Sets the value at the specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index at which to set the value.</param>
    /// <param name="value">The item to set.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.setat"/>
    static abstract void SetAt(WindowsRuntimeObjectReference thisReference, uint index, T value);

    /// <summary>
    /// Appends an item to the end of the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="value">The item to append to the vector.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.append"/>
    static abstract void Append(WindowsRuntimeObjectReference thisReference, T value);

    /// <summary>
    /// Retrieves the index of a specified item in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="value">The item to find in the vector.</param>
    /// <param name="index">If the item is found, this is the zero-based index of the item; otherwise, this parameter is <c>0</c>.</param>
    /// <returns><see langword="true"/> if the item is found; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.indexof"/>
    static abstract bool IndexOf(WindowsRuntimeObjectReference thisReference, T value, out uint index);

    /// <summary>
    /// Inserts an item at a specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index.</param>
    /// <param name="value">The item to insert.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.insertat"/>
    static abstract void InsertAt(WindowsRuntimeObjectReference thisReference, uint index, T value);
}
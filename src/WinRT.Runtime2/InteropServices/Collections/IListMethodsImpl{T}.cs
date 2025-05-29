// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="IList{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IListMethodsImpl<T>
{
    /// <inheritdoc cref="IList{T}.this"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract T Item(WindowsRuntimeObjectReference thisReference, int index);

    /// <inheritdoc cref="IList{T}.this"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="item">The item to set.</param>
    static abstract void Item(WindowsRuntimeObjectReference thisReference, int index, T item);

    /// <inheritdoc cref="ICollection{T}.Add"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract void Add(WindowsRuntimeObjectReference thisReference, T item);

    /// <inheritdoc cref="ICollection{T}.Contains"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract bool Contains(WindowsRuntimeObjectReference thisReference, T item);

    /// <inheritdoc cref="ICollection{T}.CopyTo"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract void CopyTo(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex);

    /// <inheritdoc cref="ICollection{T}.Remove"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract bool Remove(WindowsRuntimeObjectReference thisReference, T item);

    /// <inheritdoc cref="IList{T}.IndexOf"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract int IndexOf(WindowsRuntimeObjectReference thisReference, T item);

    /// <inheritdoc cref="IList{T}.Insert"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract void Insert(WindowsRuntimeObjectReference thisReference, int index, T item);
}

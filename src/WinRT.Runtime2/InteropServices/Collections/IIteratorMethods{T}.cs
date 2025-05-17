// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for specialized generic implementations of native methods for <c>Windows.Foundation.Collections.IIterator&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIteratorMethods<T>
{
    /// <summary>
    /// Gets the current item in the collection.
    /// </summary>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.Current</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.current"/>
    static abstract T Current(WindowsRuntimeObjectReference thisObject);

    /// <summary>
    /// Gets a value that indicates whether the iterator refers to a current item or is at the end of the collection.
    /// </summary>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.HasCurrent</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.hascurrent"/>
    static abstract bool HasCurrent(WindowsRuntimeObjectReference thisObject);

    /// <summary>
    /// Advances the iterator to the next item in the collection.
    /// </summary>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.MoveNext</c> method.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.movenext"/>
    static abstract bool MoveNext(WindowsRuntimeObjectReference thisObject);

    /// <summary>
    /// Retrieves multiple items from the iterator.
    /// </summary>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="items">The target <see cref="Span{T}"/> to write items into.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.GetMany</c> method.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.getmany"/>
    static abstract uint GetMany(WindowsRuntimeObjectReference thisObject, Span<T> items);
}

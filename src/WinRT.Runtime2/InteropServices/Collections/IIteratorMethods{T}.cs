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
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIteratorMethods<T>
{
    /// <summary>
    /// Gets the current item in the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.Current</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.current"/>
    static abstract T Current(WindowsRuntimeObjectReference thisReference);

    /// <summary>
    /// Gets a value that indicates whether the iterator refers to a current item or is at the end of the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.HasCurrent</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.hascurrent"/>
    static abstract bool HasCurrent(WindowsRuntimeObjectReference thisReference);

    /// <summary>
    /// Advances the iterator to the next item in the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.MoveNext</c> method.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.movenext"/>
    static abstract bool MoveNext(WindowsRuntimeObjectReference thisReference);
}

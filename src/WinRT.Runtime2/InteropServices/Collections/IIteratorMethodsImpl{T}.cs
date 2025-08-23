// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IIterator&lt;T&gt;</c> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIteratorMethodsImpl<T>
{
    /// <summary>
    /// Gets the current item in the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The current element.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.current"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static abstract T Current(WindowsRuntimeObjectReference thisReference);
}

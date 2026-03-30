// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Collections.Generic;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
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
#endif
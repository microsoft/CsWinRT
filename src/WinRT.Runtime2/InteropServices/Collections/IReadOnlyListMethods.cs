// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="System.Collections.Generic.IReadOnlyList{T}"/> types.
/// </summary>
public static class IReadOnlyListMethods
{
    /// <inheritdoc cref="System.Collections.Generic.IReadOnlyCollection{T}.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        uint count = IVectorViewMethods.Size(thisReference);

        // 'IVectorView<T>' uses 'uint' for the size, so we need to validate it's in the valid 'int' range
        InvalidOperationException.ThrowIfCollectionBackingListTooLarge(count);

        return (int)count;
    }
}
#endif
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="System.Collections.Generic.IReadOnlyList{T}"/> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListMethods
{
    /// <inheritdoc cref="System.Collections.Generic.IReadOnlyCollection{T}.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        uint count = IVectorViewMethods.Size(thisReference);

        // 'IVectorView<T>' uses 'uint' for the size, so we need to validate it's in the valid 'int' range
        if (count > int.MaxValue)
        {
            [DoesNotReturn]
            static void ThrowInvalidOperationException()
                => throw new InvalidOperationException("InvalidOperation_CollectionBackingListTooLarge");

            ThrowInvalidOperationException();
        }

        return (int)count;
    }
}

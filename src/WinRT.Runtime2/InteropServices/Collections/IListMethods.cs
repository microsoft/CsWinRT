// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of <see cref="System.Collections.Generic.IList{T}"/> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IListMethods
{
    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        // The vtable slot for 'get_Size' and the desired semantics are identical between 'IVector<T>' and 'IVectorView<T>'
        return IReadOnlyListMethods.Count(thisReference);
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Clear"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        IVectorMethods.Clear(thisReference);
    }

    /// <inheritdoc cref="System.Collections.Generic.IList{T}.RemoveAt"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void RemoveAt(WindowsRuntimeObjectReference thisReference, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // Defer the bounds checks to the native implementation, like with the indexer methods
            IVectorMethods.RemoveAt(thisReference, (uint)index);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}

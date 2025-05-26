// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of <see cref="System.Collections.Generic.IReadOnlyList{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IReadOnlyListMethods<T>
{
    /// <inheritdoc cref="System.Collections.Generic.IReadOnlyList{T}.this[int]"/>
    /// <typeparam name="TMethods">The <see cref="IVectorViewMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static T Item<TMethods>(WindowsRuntimeObjectReference thisReference, int index)
        where TMethods : IVectorViewMethods<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // The native implementation will perform the bounds check, so we avoid doing an
            // extra native call just to get the size of the collection. If the call fails
            // because the index is not valid, we translate the exception to the rigth one.
            return TMethods.GetAt(thisReference, (uint)index);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}

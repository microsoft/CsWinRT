// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime <see cref="IReadOnlyList{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeReadOnlyList<T> : WindowsRuntimeObject, IReadOnlyList<T>, IWindowsRuntimeInterface<IReadOnlyList<T>>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeReadOnlyList{T}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeReadOnlyList(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc cref="WindowsRuntimeEnumerable{T}.FirstNative"/>
    protected abstract IEnumerator<T> FirstNative();

    /// <summary>
    /// Returns the item at the specified index in the vector view.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IVectorView&lt;T&gt;.GetAt</c> method.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1.getat"/>
    protected abstract T GetAt(uint index);

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            uint count = IVectorViewMethods.Size(NativeObjectReference);

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

    /// <inheritdoc/>
    public T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            try
            {
                // The native implementation will perform the bounds check, so we avoid doing an
                // extra native call just to get the size of the collection. If the call fails
                // because the index is not valid, we translate the exception to the rigth one.
                return GetAt((uint)index);
            }
            catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        return FirstNative();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IReadOnlyList<T>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

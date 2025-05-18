// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsRuntime.InteropServices;

#pragma warning disable CA1816

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime <see cref="IEnumerator{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeEnumerator<T> : WindowsRuntimeObject, IEnumerator<T>, IWindowsRuntimeInterface<IEnumerator<T>>
{
    private bool _hadCurrent = true;
    private bool _isInitialized = false;
    private T _current = default!;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeEnumerator{T}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeEnumerator(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <summary>
    /// Gets the current item in the collection.
    /// </summary>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.Current</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.current"/>
    protected abstract T CurrentNative { get; }

    /// <summary>
    /// Gets a value that indicates whether the iterator refers to a current item or is at the end of the collection.
    /// </summary>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.HasCurrent</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.hascurrent"/>
    protected abstract bool HasCurrentNative { get; }

    /// <summary>
    /// Advances the iterator to the next item in the collection.
    /// </summary>
    /// <returns>
    /// True if the iterator refers to a valid item in the collection, false if the iterator passes the end of the collection.
    /// </returns>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.MoveNext</c> method.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.movenext"/>
    protected abstract bool MoveNextNative();

    /// <inheritdoc/>
    public T Current { get; }

    /// <inheritdoc/>
    object IEnumerator.Current => Current!;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        return false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public void Reset()
    {
        throw new NotSupportedException("Calling 'IEnumerator.Reset()' is not supported on native Windows Runtime enumerators.");
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerator<T>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

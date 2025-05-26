// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using WindowsRuntime.InteropServices;

#pragma warning disable CA1816

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime <see cref="IList{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeList<T> : WindowsRuntimeObject,
    IList<T>,
    IReadOnlyList<T>,
    IWindowsRuntimeInterface<IList<T>>,
    IWindowsRuntimeInterface<IEnumerable<T>>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeList{T}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeList(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc cref="WindowsRuntimeReadOnlyList{T}.IID_IIterable"/>
    protected abstract ref readonly Guid IID_IIterable { get; }

    /// <inheritdoc cref="WindowsRuntimeReadOnlyList{T}.IIterableObjectReference"/>
    [field: AllowNull, MaybeNull]
    protected WindowsRuntimeObjectReference IIterableObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeInspectableObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in IID_IIterable),
                    comparand: null);

                return field;
            }

            return field ?? InitializeInspectableObjectReference();
        }
    }

    /// <inheritdoc/>
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public int Count => IListMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public abstract T this[int index] { get; set; }

    /// <inheritdoc/>
    public abstract int IndexOf(T item);

    /// <inheritdoc/>
    public abstract void Insert(int index, T item);

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        IListMethods.RemoveAt(NativeObjectReference, index);
    }

    /// <inheritdoc/>
    public abstract void Add(T item);

    /// <inheritdoc/>
    public void Clear()
    {
        IListMethods.Clear(NativeObjectReference);
    }

    /// <inheritdoc/>
    public abstract bool Contains(T item);

    /// <inheritdoc/>
    public abstract void CopyTo(T[] array, int arrayIndex);

    /// <inheritdoc/>
    public abstract bool Remove(T item);

    /// <inheritdoc/>
    public abstract IEnumerator<T> GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IList<T>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerable<T>>.GetInterface()
    {
        return IIterableObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

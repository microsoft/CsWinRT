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
/// The implementation of all projected Windows Runtime <see cref="IList{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <typeparam name="TIIterable">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> interface type.</typeparam>
/// <typeparam name="TIIterableMethods">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> implementation type.</typeparam>
/// <typeparam name="TIVectorMethods">The <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeList<
    T,
    TIIterable,
    TIIterableMethods,
    TIVectorMethods> : WindowsRuntimeObject,
    IList<T>,
    IReadOnlyList<T>,
    IWindowsRuntimeInterface<IList<T>>,
    IWindowsRuntimeInterface<IEnumerable<T>>
    where TIIterable : IWindowsRuntimeInterface
    where TIIterableMethods : IIterableMethodsImpl<T>
    where TIVectorMethods : IVectorMethodsImpl<T>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeList{T, TIIterable, TIEnumerableMethods, TIListMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeList(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc cref="WindowsRuntimeReadOnlyList{T, TIIterable, TIEnumerableMethods, TIReadOnlyListMethods}.IIterableObjectReference"/>
    private WindowsRuntimeObjectReference IIterableObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeInspectableObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in TIIterable.IID),
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
    public T this[int index]
    {
        get => IListMethods<T>.Item<TIVectorMethods>(NativeObjectReference, index);
        set => IListMethods<T>.Item<TIVectorMethods>(NativeObjectReference, index, value);
    }

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        return IListMethods<T>.IndexOf<TIVectorMethods>(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public void Insert(int index, T item)
    {
        IListMethods<T>.Insert<TIVectorMethods>(NativeObjectReference, index, item);
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        IListMethods.RemoveAt(NativeObjectReference, index);
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        IListMethods<T>.Add<TIVectorMethods>(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        IListMethods.Clear(NativeObjectReference);
    }

    /// <inheritdoc/>
    public bool Contains(T item)
    {
        return IListMethods<T>.Contains<TIVectorMethods>(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        IListMethods<T>.CopyTo<TIVectorMethods>(NativeObjectReference, array, arrayIndex);
    }

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        return IListMethods<T>.Remove<TIVectorMethods>(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        return TIIterableMethods.First(IIterableObjectReference);
    }

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

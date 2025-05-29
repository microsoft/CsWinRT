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
/// <typeparam name="TIIterable">The <see cref="IEnumerable{T}"/> interface type.</typeparam>
/// <typeparam name="TIEnumerableMethods">The <see cref="IEnumerableMethodsImpl{T}"/> implementation type.</typeparam>
/// <typeparam name="TIListMethods">The <see cref="IListMethodsImpl{T}"/> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeList<
    T,
    TIIterable,
    TIEnumerableMethods,
    TIListMethods> : WindowsRuntimeObject,
    IList<T>,
    IReadOnlyList<T>,
    IWindowsRuntimeInterface<IList<T>>,
    IWindowsRuntimeInterface<IEnumerable<T>>
    where TIIterable : IWindowsRuntimeInterface
    where TIEnumerableMethods : IEnumerableMethodsImpl<T>
    where TIListMethods : IListMethodsImpl<T>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeList{T, TIIterable, TIEnumerableMethods, TIListMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeList(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc cref="WindowsRuntimeReadOnlyList{T, TIIterable, TIEnumerableMethods, TIReadOnlyListMethods}.IIterableObjectReference"/>
    [field: AllowNull, MaybeNull]
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
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public int Count => IListMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public T this[int index]
    {
        get => TIListMethods.Item(NativeObjectReference, index);
        set => TIListMethods.Item(NativeObjectReference, index, value);
    }

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        return TIListMethods.IndexOf(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public void Insert(int index, T item)
    {
        TIListMethods.Insert(NativeObjectReference, index, item);
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        IListMethods.RemoveAt(NativeObjectReference, index);
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        TIListMethods.Add(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        IListMethods.Clear(NativeObjectReference);
    }

    /// <inheritdoc/>
    public bool Contains(T item)
    {
        return TIListMethods.Contains(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        TIListMethods.CopyTo(NativeObjectReference, array, arrayIndex);
    }

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        return TIListMethods.Remove(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        return TIEnumerableMethods.GetEnumerator(IIterableObjectReference);
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
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

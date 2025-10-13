// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation.Collections;
using WindowsRuntime.InteropServices;

#pragma warning disable CA1816

namespace WindowsRuntime;

/// <summary>
/// The implementation of all projected Windows Runtime <see cref="IObservableVector{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <typeparam name="TIIterable">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> interface type.</typeparam>
/// <typeparam name="TIIterableMethods">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> implementation type.</typeparam>
/// <typeparam name="TIVector">The <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c> interface type.</typeparam>
/// <typeparam name="TIVectorMethods">The <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c> implementation type.</typeparam>
/// <typeparam name="TIObservableVectorMethods">The <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iobservablevector-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeObservableVector<
    T,
    TIIterable,
    TIIterableMethods,
    TIVector,
    TIVectorMethods,
    TIObservableVectorMethods> : WindowsRuntimeObject,
    IObservableVector<T>,
    IList<T>,
    IReadOnlyList<T>,
    IWindowsRuntimeInterface<IObservableVector<T>>,
    IWindowsRuntimeInterface<IList<T>>,
    IWindowsRuntimeInterface<IEnumerable<T>>
    where TIIterable : IWindowsRuntimeInterface
    where TIIterableMethods : IIterableMethodsImpl<T>
    where TIVector : IWindowsRuntimeInterface
    where TIVectorMethods : IVectorMethodsImpl<T>
    where TIObservableVectorMethods : IObservableVectorMethodsImpl<T>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObservableVector{T, TIIterable, TIIterableMethods, TIVector, TIVectorMethods, TIObservableVectorMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeObservableVector(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c> for the current object.
    /// </summary>
    private WindowsRuntimeObjectReference IVectorObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIVectorObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in TIVector.IID),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIVectorObjectReference();
        }
    }

    /// <inheritdoc cref="WindowsRuntimeReadOnlyList{T, TIIterable, TIEnumerableMethods, TIReadOnlyListMethods}.IIterableObjectReference"/>
    private WindowsRuntimeObjectReference IIterableObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIIterableObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in TIIterable.IID),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIIterableObjectReference();
        }
    }

    /// <inheritdoc/>
    public event VectorChangedEventHandler<T>? VectorChanged
    {
        add => TIObservableVectorMethods.VectorChanged(this, NativeObjectReference).Subscribe(value);
        remove => TIObservableVectorMethods.VectorChanged(this, NativeObjectReference).Unsubscribe(value);
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public int Count => IListMethods.Count(IVectorObjectReference);

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public T this[int index]
    {
        get => IListMethods<T>.Item<TIVectorMethods>(IVectorObjectReference, index);
        set => IListMethods<T>.Item<TIVectorMethods>(IVectorObjectReference, index, value);
    }

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        return IListMethods<T>.IndexOf<TIVectorMethods>(IVectorObjectReference, item);
    }

    /// <inheritdoc/>
    public void Insert(int index, T item)
    {
        IListMethods<T>.Insert<TIVectorMethods>(IVectorObjectReference, index, item);
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        IListMethods.RemoveAt(IVectorObjectReference, index);
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        IListMethods<T>.Add<TIVectorMethods>(IVectorObjectReference, item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        IListMethods.Clear(IVectorObjectReference);
    }

    /// <inheritdoc/>
    public bool Contains(T item)
    {
        return IListMethods<T>.Contains<TIVectorMethods>(IVectorObjectReference, item);
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        IListMethods<T>.CopyTo<TIVectorMethods>(IVectorObjectReference, array, arrayIndex);
    }

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        return IListMethods<T>.Remove<TIVectorMethods>(IVectorObjectReference, item);
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
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IObservableVector<T>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IList<T>>.GetInterface()
    {
        return IVectorObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerable<T>>.GetInterface()
    {
        return IIterableObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

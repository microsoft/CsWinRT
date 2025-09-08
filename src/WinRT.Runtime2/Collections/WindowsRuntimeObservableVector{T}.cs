// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
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
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeObservableVector<
    T,
    TIIterable,
    TIIterableMethods,
    TIVector,
    TIVectorMethods,
    TIObservableVectorMethods> : WindowsRuntimeList<T, TIIterable, TIIterableMethods, TIVectorMethods>,
    IObservableVector<T>,
    IWindowsRuntimeInterface<IObservableVector<T>>
    where TIIterable : IWindowsRuntimeInterface
    where TIIterableMethods : IIterableMethodsImpl<T>
    where TIVector : IWindowsRuntimeInterface
    where TIVectorMethods : IVectorMethodsImpl<T>
    where TIObservableVectorMethods : IObservableVectorMethodsImpl<T>
{
    /// <summary>
    /// The cached object reference for <see cref="IObservableVector{T}"/> for the current object.
    /// </summary>
    private readonly WindowsRuntimeObjectReference _observableVectorObjectReference;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeList{T, TIIterable, TIEnumerableMethods, TIListMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeObservableVector(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference.As(in TIVector.IID))
    {
        _observableVectorObjectReference = nativeObjectReference;
    }

    /// <inheritdoc/>
    public event VectorChangedEventHandler<T>? VectorChanged
    {
        add => TIObservableVectorMethods.VectorChanged(this, _observableVectorObjectReference).Subscribe(value);
        remove => TIObservableVectorMethods.VectorChanged(this, _observableVectorObjectReference).Unsubscribe(value);
    }

    /// <inheritdoc/>
    public WindowsRuntimeObjectReferenceValue GetInterface()
    {
        return _observableVectorObjectReference.AsValue();
    }
}

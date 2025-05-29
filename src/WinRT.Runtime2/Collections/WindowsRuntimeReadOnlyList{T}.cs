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

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime <see cref="IReadOnlyList{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <typeparam name="TIIterable">The <see cref="IEnumerable{T}"/> interface type.</typeparam>
/// <typeparam name="TIEnumerableMethods">The <see cref="IEnumerableMethodsImpl{T}"/> implementation type.</typeparam>
/// <typeparam name="TIReadOnlyListMethods">The <see cref="IReadOnlyListMethodsImpl{T}"/> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeReadOnlyList<
    T,
    TIIterable,
    TIEnumerableMethods,
    TIReadOnlyListMethods> : WindowsRuntimeObject,
    IReadOnlyList<T>,
    IWindowsRuntimeInterface<IReadOnlyList<T>>,
    IWindowsRuntimeInterface<IEnumerable<T>>
    where TIIterable : IWindowsRuntimeInterface
    where TIEnumerableMethods : IEnumerableMethodsImpl<T>
    where TIReadOnlyListMethods : IReadOnlyListMethodsImpl<T>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeList{T, TIIterable, TIEnumerableMethods, TIListMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeReadOnlyList(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> for the current object.
    /// </summary>
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
    public int Count => IReadOnlyListMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    public T this[int index] => TIReadOnlyListMethods.Item(NativeObjectReference, index);

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
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IReadOnlyList<T>>.GetInterface()
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

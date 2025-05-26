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
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeReadOnlyList<T> : WindowsRuntimeObject,
    IReadOnlyList<T>,
    IWindowsRuntimeInterface<IReadOnlyList<T>>,
    IWindowsRuntimeInterface<IEnumerable<T>>
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

    /// <summary>
    /// Gets the IID for the <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> interface.
    /// </summary>
    protected abstract ref readonly Guid IID_IIterable { get; }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>IInspectable</c> for the current object.
    /// </summary>
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

    /// <inheritdoc cref="WindowsRuntimeEnumerable{T}.FirstNative"/>
    protected abstract IEnumerator<T> FirstNative();

    /// <inheritdoc cref="IReadOnlyList{T}.this[int]"/>
    protected abstract T Item(int index);

    /// <inheritdoc/>
    int IReadOnlyCollection<T>.Count => IReadOnlyListMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    T IReadOnlyList<T>.this[int index] => Item(index);

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return FirstNative();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return FirstNative();
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
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

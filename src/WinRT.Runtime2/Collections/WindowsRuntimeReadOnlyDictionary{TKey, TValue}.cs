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
/// The implementation of all projected Windows Runtime <see cref="IReadOnlyDictionary{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
/// <typeparam name="TIIterable">The <see cref="IEnumerable{T}"/> interface type.</typeparam>
/// <typeparam name="TIIterableMethods">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> implementation type.</typeparam>
/// <typeparam name="TIMapViewMethods">The <c>Windows.Foundation.Collections.IMapView&lt;K, V&gt;</c> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeReadOnlyDictionary<
    TKey,
    TValue,
    TIIterable,
    TIIterableMethods,
    TIMapViewMethods> : WindowsRuntimeObject,
    IReadOnlyDictionary<TKey, TValue>,
    IWindowsRuntimeInterface<IReadOnlyDictionary<TKey, TValue>>,
    IWindowsRuntimeInterface<IEnumerable<KeyValuePair<TKey, TValue>>>
    where TIIterable : IWindowsRuntimeInterface
    where TIIterableMethods : IIterableMethodsImpl<KeyValuePair<TKey, TValue>>
    where TIMapViewMethods : IMapViewMethodsImpl<TKey, TValue>
{
    /// <summary>
    /// The <see cref="ReadOnlyDictionaryKeyCollection{TKey, TValue}"/> instance, if initialized.
    /// </summary>
    private ReadOnlyDictionaryKeyCollection<TKey, TValue>? _keys;

    /// <summary>
    /// The <see cref="ReadOnlyDictionaryValueCollection{TKey, TValue}"/> instance, if initialized.
    /// </summary>
    private ReadOnlyDictionaryValueCollection<TKey, TValue>? _values;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeReadOnlyDictionary{TKey, TValue, TIIterable, TIEnumerableMethods, TIReadOnlyDictionaryMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeReadOnlyDictionary(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> for the current object.
    /// </summary>
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public IEnumerable<TKey> Keys => _keys ??= new ReadOnlyDictionaryKeyCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    public IEnumerable<TValue> Values => _values ??= new ReadOnlyDictionaryValueCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    public int Count => IReadOnlyDictionaryMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    public TValue this[TKey key] => IReadOnlyDictionaryMethods<TKey, TValue>.Item<TIMapViewMethods>(NativeObjectReference, key);

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
    {
        return IReadOnlyDictionaryMethods<TKey, TValue>.ContainsKey<TIMapViewMethods>(NativeObjectReference, key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return IReadOnlyDictionaryMethods<TKey, TValue>.TryGetValue<TIMapViewMethods>(NativeObjectReference, key, out value);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return TIIterableMethods.First(IIterableObjectReference);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IReadOnlyDictionary<TKey, TValue>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerable<KeyValuePair<TKey, TValue>>>.GetInterface()
    {
        return IIterableObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

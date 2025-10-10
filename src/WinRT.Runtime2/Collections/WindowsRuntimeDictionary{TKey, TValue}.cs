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
/// The implementation of all projected Windows Runtime <see cref="IDictionary{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <typeparam name="TIIterable">The <see cref="IEnumerable{T}"/> interface type.</typeparam>
/// <typeparam name="TIIterableMethods">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> implementation type.</typeparam>
/// <typeparam name="TIMapMethods">The <c>Windows.Foundation.Collections.IMap&lt;K, V&gt;</c> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeDictionary<
    TKey,
    TValue,
    TIIterable,
    TIIterableMethods,
    TIMapMethods> : WindowsRuntimeObject,
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,
    IWindowsRuntimeInterface<IDictionary<TKey, TValue>>,
    IWindowsRuntimeInterface<IEnumerable<KeyValuePair<TKey, TValue>>>
    where TIIterable : IWindowsRuntimeInterface
    where TIIterableMethods : IIterableMethodsImpl<KeyValuePair<TKey, TValue>>
    where TIMapMethods : IMapMethodsImpl<TKey, TValue>
{
    /// <summary>
    /// The <see cref="DictionaryKeyCollection{TKey, TValue}"/> instance, if initialized.
    /// </summary>
    private DictionaryKeyCollection<TKey, TValue>? _keys;

    /// <summary>
    /// The <see cref="DictionaryValueCollection{TKey, TValue}"/> instance, if initialized.
    /// </summary>
    private DictionaryValueCollection<TKey, TValue>? _values;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeDictionary{TKey, TValue, TIIterable, TIEnumerableMethods, TIDictionaryMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeDictionary(WindowsRuntimeObjectReference nativeObjectReference)
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
    public ICollection<TKey> Keys => _keys ??= new DictionaryKeyCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc/>
    public ICollection<TValue> Values => _values ??= new DictionaryValueCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc/>
    public int Count => IReadOnlyDictionaryMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get => IDictionaryMethods<TKey, TValue>.Item<TIMapMethods>(NativeObjectReference, key);
        set => IDictionaryMethods<TKey, TValue>.Item<TIMapMethods>(NativeObjectReference, key, value);
    }

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
    {
        IDictionaryMethods<TKey, TValue>.Add<TIMapMethods>(NativeObjectReference, key, value);
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
    {
        return IDictionaryMethods<TKey, TValue>.ContainsKey<TIMapMethods>(NativeObjectReference, key);
    }

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        return IDictionaryMethods<TKey, TValue>.Remove<TIMapMethods>(NativeObjectReference, key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return IDictionaryMethods<TKey, TValue>.TryGetValue<TIMapMethods>(NativeObjectReference, key, out value);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        IMapMethods.Clear(NativeObjectReference);
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        IDictionaryMethods<TKey, TValue>.Add<TIMapMethods>(NativeObjectReference, item.Key, item.Value);
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return IDictionaryMethods<TKey, TValue>.Contains<TIMapMethods>(NativeObjectReference, item);
    }

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        IDictionaryMethods<TKey, TValue>.CopyTo<TIMapMethods, TIIterableMethods>(
            thisIMapReference: NativeObjectReference,
            thisIIterableReference: IIterableObjectReference,
            array: array,
            arrayIndex: arrayIndex);
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return IDictionaryMethods<TKey, TValue>.Remove<TIMapMethods>(NativeObjectReference, item.Key);
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
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IDictionary<TKey, TValue>>.GetInterface()
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

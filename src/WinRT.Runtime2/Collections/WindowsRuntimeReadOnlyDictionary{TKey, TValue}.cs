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
/// The base class for all projected Windows Runtime <see cref="IReadOnlyDictionary{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeReadOnlyDictionary<TKey, TValue> : WindowsRuntimeObject,
    IReadOnlyDictionary<TKey, TValue>,
    IWindowsRuntimeInterface<IReadOnlyDictionary<TKey, TValue>>,
    IWindowsRuntimeInterface<IEnumerable<KeyValuePair<TKey, TValue>>>
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
    /// Creates a <see cref="WindowsRuntimeReadOnlyDictionary{TKey, TValue}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeReadOnlyDictionary(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the IID for the <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> interface.
    /// </summary>
    protected abstract ref readonly Guid IID_IIterable { get; }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> for the current object.
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

    /// <inheritdoc/>
    public IEnumerable<TKey> Keys => _keys ??= new ReadOnlyDictionaryKeyCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    public IEnumerable<TValue> Values => _values ??= new ReadOnlyDictionaryValueCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    public int Count => IReadOnlyDictionaryMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    public abstract TValue this[TKey key] { get; }

    /// <inheritdoc/>
    public abstract bool ContainsKey(TKey key);

    /// <inheritdoc/>
    public abstract bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

    /// <inheritdoc/>
    public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

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
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

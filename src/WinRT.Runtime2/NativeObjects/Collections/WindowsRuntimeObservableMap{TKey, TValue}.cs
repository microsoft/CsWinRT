// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation.Collections;
using WindowsRuntime.InteropServices;

#pragma warning disable CA1816

namespace WindowsRuntime;

/// <summary>
/// The implementation of all projected Windows Runtime <see cref="IObservableMap{K, V}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the observable map.</typeparam>
/// <typeparam name="TValue">The type of values in the observable map.</typeparam>
/// <typeparam name="TIIterable">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> interface type.</typeparam>
/// <typeparam name="TIIterableMethods">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> implementation type.</typeparam>
/// <typeparam name="TIMap">The <c>Windows.Foundation.Collections.IMap&lt;K, V&gt;</c> interface type.</typeparam>
/// <typeparam name="TIMapMethods">The <c>Windows.Foundation.Collections.IMap&lt;K, V&gt;</c> implementation type.</typeparam>
/// <typeparam name="TIObservableMapEventSourceFactory">The <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c> factory type for event source objects.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iobservablevector-1"/>
[WindowsRuntimeManagedOnlyType]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeObservableMap<
    TKey,
    TValue,
    TIIterable,
    TIIterableMethods,
    TIMap,
    TIMapMethods,
    TIObservableMapEventSourceFactory> : WindowsRuntimeObject,
    IObservableMap<TKey, TValue>,
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,
    IWindowsRuntimeInterface<IObservableMap<TKey, TValue>>,
    IWindowsRuntimeInterface<IDictionary<TKey, TValue>>,
    IWindowsRuntimeInterface<IEnumerable<KeyValuePair<TKey, TValue>>>
    where TIIterable : IWindowsRuntimeInterface
    where TIIterableMethods : IIterableMethodsImpl<KeyValuePair<TKey, TValue>>
    where TIMap : IWindowsRuntimeInterface
    where TIMapMethods : IMapMethodsImpl<TKey, TValue>
    where TIObservableMapEventSourceFactory : IObservableMapEventSourceFactory<TKey, TValue>
{
    /// <inheritdoc cref="WindowsRuntimeDictionary{TKey, TValue, TIIterable, TIIterableMethods, TIMapMethods}._keys"/>
    private DictionaryKeyCollection<TKey, TValue>? _keys;

    /// <inheritdoc cref="WindowsRuntimeDictionary{TKey, TValue, TIIterable, TIIterableMethods, TIMapMethods}._values"/>
    private DictionaryValueCollection<TKey, TValue>? _values;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObservableMap{TKey, TValue, TIIterable, TIIterableMethods, TIMap, TIMapMethods, TIObservableMapMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeObservableMap(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>Windows.Foundation.Collections.IMap&lt;K, V&gt;</c> for the current object.
    /// </summary>
    private WindowsRuntimeObjectReference IMapObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIMapObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in TIMap.IID),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIMapObjectReference();
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

    /// <summary>
    /// Gets the lazy-loaded <see cref="ABI.Windows.Foundation.Collections.MapChangedEventHandlerEventSource{K, V}"/> instance for the current object.
    /// </summary>
    private ABI.Windows.Foundation.Collections.MapChangedEventHandlerEventSource<TKey, TValue> MapChangedEventSource
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            ABI.Windows.Foundation.Collections.MapChangedEventHandlerEventSource<TKey, TValue> InitializeMapChangedEventSource()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: TIObservableMapEventSourceFactory.MapChanged(NativeObjectReference),
                    comparand: null);

                return field;
            }

            return field ?? InitializeMapChangedEventSource();
        }
    }

    /// <inheritdoc/>
    public event MapChangedEventHandler<TKey, TValue>? MapChanged
    {
        add => MapChangedEventSource.Subscribe(value);
        remove => MapChangedEventSource.Unsubscribe(value);
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    /// <remarks>
    /// The resulting object will be of type <see cref="DictionaryKeyCollection{TKey, TValue}"/>.
    /// </remarks>
    public ICollection<TKey> Keys => _keys ??= new DictionaryKeyCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    /// <remarks><inheritdoc cref="Keys" path="/remarks/node()"/></remarks>
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc/>
    /// <remarks>
    /// The resulting object will be of type <see cref="DictionaryValueCollection{TKey, TValue}"/>.
    /// </remarks>
    public ICollection<TValue> Values => _values ??= new DictionaryValueCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    /// <remarks><inheritdoc cref="Values" path="/remarks/node()"/></remarks>
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
        IDictionaryMethods<TKey, TValue>.CopyTo<TIIterableMethods>(
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
        return IEnumerableMethods<KeyValuePair<TKey, TValue>>.GetEnumerator<TIIterableMethods>(IIterableObjectReference);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IObservableMap<TKey, TValue>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IDictionary<TKey, TValue>>.GetInterface()
    {
        return IMapObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerable<KeyValuePair<TKey, TValue>>>.GetInterface()
    {
        return IIterableObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
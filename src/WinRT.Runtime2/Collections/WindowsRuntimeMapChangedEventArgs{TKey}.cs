// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Windows.Foundation.Collections;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime <see cref="IMapChangedEventArgs{K}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the map.</typeparam>
/// <typeparam name="TIMapChangedEventArgsMethods">The <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeMapChangedEventArgs<TKey, TIMapChangedEventArgsMethods> : WindowsRuntimeObject,
    IMapChangedEventArgs<TKey>,
    IWindowsRuntimeInterface<IMapChangedEventArgs<TKey>>
    where TIMapChangedEventArgsMethods : IMapChangedEventArgsMethodsImpl<TKey>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMapChangedEventArgs{TKey, TIMapChangedEventArgsMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeMapChangedEventArgs(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public CollectionChange CollectionChange => IMapChangedEventArgsMethods.CollectionChange(NativeObjectReference);

    /// <inheritdoc/>
    public TKey Key => TIMapChangedEventArgsMethods.Key(NativeObjectReference);

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IMapChangedEventArgs<TKey>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

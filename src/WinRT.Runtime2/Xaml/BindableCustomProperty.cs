// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Xaml;

/// <summary>
/// Provides information about a given managed property to support <see cref="IBindableCustomPropertyProvider"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.icustomproperty"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.icustomproperty"/>
public abstract class BindableCustomProperty
{
    /// <summary>
    /// Gets a value that determines whether the custom property supports read access.
    /// </summary>
    public abstract bool CanRead { get; }

    /// <summary>
    /// Gets a value that determines whether the custom property supports write access.
    /// </summary>
    public abstract bool CanWrite { get; }

    /// <summary>
    /// Gets the path-relevant name of the property.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the underlying type of the custom property.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// Gets the value of the custom property from a particular instance.
    /// </summary>
    /// <param name="target">The owning instance.</param>
    /// <returns>The retrieved value.</returns>
    public abstract object? GetValue(object target);

    /// <summary>
    /// Gets the value at an index location, for cases where the custom property has indexer support.
    /// </summary>
    /// <param name="target">The owning instance.</param>
    /// <param name="index">The index to get.</param>
    /// <returns>The retrieved value at the index.</returns>
    public abstract object? GetIndexedValue(object target, object index);

    /// <summary>
    /// Sets the custom property value on a specified instance.
    /// </summary>
    /// <param name="target">The owner instance.</param>
    /// <param name="value">The value to set.</param>
    public abstract void SetValue(object target, object? value);

    /// <summary>
    /// Sets the value at an index location, for cases where the custom property has indexer support.
    /// </summary>
    /// <param name="target">The owner instance.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="index">The index location to set to.</param>
    public abstract void SetIndexedValue(object target, object? value, object? index);
}

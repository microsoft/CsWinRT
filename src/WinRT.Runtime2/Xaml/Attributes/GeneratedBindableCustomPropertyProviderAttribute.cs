// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Xaml;

/// <summary>
/// An attribute used to indicate the properties which are bindable, for XAML (WinUI) scenarios.
/// </summary>
/// <remarks>
/// This attribute will cause binding code to be generated to provide support via the <c>Windows.UI.Xaml.Data.ICustomPropertyProvider</c>
/// and <c>Microsoft.UI.Xaml.Data.ICustomPropertyProvider</c> infrastructure, for the specified properties on the annotated type.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.icustompropertyprovider"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.icustompropertyprovider"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedBindableCustomPropertyProviderAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="GeneratedBindableCustomPropertyProviderAttribute"/> instance.
    /// </summary>
    /// <remarks>
    /// Using this constructor will mark all public properties as bindable.
    /// </remarks>
    public GeneratedBindableCustomPropertyProviderAttribute()
    {
    }

    /// <summary>
    /// Creates a new <see cref="GeneratedBindableCustomPropertyProviderAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="propertyNames">The name of the non-indexer public properties to mark as bindable.</param>
    /// <param name="indexerPropertyTypes">The parameter type of the indexer public properties to mark as bindable.</param>
    public GeneratedBindableCustomPropertyProviderAttribute(string[] propertyNames, Type[] indexerPropertyTypes)
    {
        PropertyNames = propertyNames;
        IndexerPropertyTypes = indexerPropertyTypes;
    }

    /// <summary>
    /// Gets the name of the non-indexer public properties to mark as bindable.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, all public properties are considered bindable.
    /// </remarks>
    public string[]? PropertyNames { get; }

    /// <summary>
    /// Gets the parameter type of the indexer public properties to mark as bindable.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, all indexer public properties are considered bindable.
    /// </remarks>
    public Type[]? IndexerPropertyTypes { get; }
}

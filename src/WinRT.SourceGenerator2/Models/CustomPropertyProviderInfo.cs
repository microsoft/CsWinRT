// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// A model describing a type that implements <c>ICustomPropertyProvider</c>.
/// </summary>
/// <param name="TypeHierarchy">The type hierarchy info for the annotated type.</param>
/// <param name="CustomProperties">The custom properties to generate code for on the annotated type.</param>
/// <param name="UseWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
internal sealed record CustomPropertyProviderInfo(
    HierarchyInfo TypeHierarchy,
    EquatableArray<CustomPropertyInfo> CustomProperties,
    bool UseWindowsUIXamlProjections)
{
    /// <summary>
    /// Gets the fully qualified name of the <c>ICustomPropertyProvider</c> interface to use.
    /// </summary>
    public string FullyQualifiedCustomPropertyProviderInterfaceName => UseWindowsUIXamlProjections
        ? "Windows.UI.Xaml.Data.ICustomPropertyProvider"
        : "Microsoft.UI.Xaml.Data.ICustomPropertyProvider";

    /// <summary>
    /// Gets the fully qualified name of the <c>ICustomProperty</c> interface to use.
    /// </summary>
    public string FullyQualifiedCustomPropertyInterfaceName => UseWindowsUIXamlProjections
        ? "Windows.UI.Xaml.Data.ICustomProperty"
        : "Microsoft.UI.Xaml.Data.ICustomProperty";
}
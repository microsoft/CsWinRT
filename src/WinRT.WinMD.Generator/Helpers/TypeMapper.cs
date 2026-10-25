// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using AsmResolver.DotNet;
using WindowsRuntime.Generator.Helpers;
using WindowsRuntime.WinMDGenerator.Models;

namespace WindowsRuntime.WinMDGenerator.Helpers;

/// <summary>
/// Maps .NET types to their Windows Runtime equivalents for WinMD generation.
/// </summary>
/// <remarks>
/// The mapping data is shared with the Roslyn authoring analyzers. This adapter preserves the
/// metadata-specific resolution of context-dependent types such as <see cref="Type"/>.
/// </remarks>
internal sealed class TypeMapper
{
    /// <summary>
    /// The mappings for the selected XAML projection mode.
    /// </summary>
    private readonly FrozenDictionary<string, WindowsRuntimeTypeMappings.Mapping> _typeMapping;

    /// <summary>
    /// Creates a new <see cref="TypeMapper"/> instance.
    /// </summary>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML instead of WinUI.</param>
    public TypeMapper(bool useWindowsUIXamlProjections)
    {
        _typeMapping = WindowsRuntimeTypeMappings.GetMappings(useWindowsUIXamlProjections);
    }

    /// <summary>
    /// Gets the custom mapping for <see cref="Type"/> based on the containing type context.
    /// </summary>
    /// <param name="containingType">The containing type for the type reference, if available.</param>
    /// <returns>The context-dependent Windows Runtime mapping.</returns>
    private static MappedTypeInfo GetSystemTypeMappingInfo(TypeDefinition? containingType)
    {
        bool isDefinedInAttribute = containingType is { BaseType.FullName: "System.Attribute" };

        return isDefinedInAttribute
            ? new("System", "Type", "mscorlib", true, false)
            : new("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", true, true);
    }

    /// <summary>
    /// Gets the .NET interfaces whose implementations are not exported to Windows Runtime metadata.
    /// </summary>
    public static FrozenSet<string> ImplementedInterfacesWithoutMapping => WindowsRuntimeTypeMappings.ImplementedInterfacesWithoutMapping;

    /// <summary>
    /// Checks whether a mapping exists for a fully qualified .NET metadata type name.
    /// </summary>
    /// <param name="typeName">The fully qualified .NET metadata type name.</param>
    /// <returns>Whether the type has a Windows Runtime mapping.</returns>
    public bool HasMappingForType(string typeName)
    {
        return _typeMapping.ContainsKey(typeName);
    }

    /// <summary>
    /// Gets the Windows Runtime mapping for a fully qualified .NET metadata type name.
    /// </summary>
    /// <param name="typeName">The fully qualified .NET metadata type name.</param>
    /// <returns>The fixed or context-dependent Windows Runtime mapping.</returns>
    public MappedType GetMappedType(string typeName)
    {
        WindowsRuntimeTypeMappings.Mapping mapping = _typeMapping[typeName];

        return typeName == "System.Type"
            ? new MappedType(GetSystemTypeMappingInfo)
            : new MappedType(mapping.Namespace, mapping.Name, mapping.Assembly, mapping.IsValueType, mapping.IsBlittable);
    }
}
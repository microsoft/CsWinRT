// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Discovery;

/// <summary>
/// Analyzes a compiled assembly to discover its public WinRT API surface.
/// </summary>
/// <remarks>
/// This type is responsible for scanning the input module and collecting all public types
/// that should be represented in the output WinMD. It discovers public classes, interfaces,
/// structs, enums, and delegates, including nested public types.
/// </remarks>
internal sealed class AssemblyAnalyzer
{
    /// <summary>
    /// The input module to analyze.
    /// </summary>
    private readonly ModuleDefinition _inputModule;

    /// <summary>
    /// Creates a new <see cref="AssemblyAnalyzer"/> instance for the given input module.
    /// </summary>
    /// <param name="inputModule">The compiled module to analyze.</param>
    public AssemblyAnalyzer(ModuleDefinition inputModule)
    {
        _inputModule = inputModule;
    }

    /// <summary>
    /// Gets the assembly name from the input module.
    /// </summary>
    public string AssemblyName => _inputModule.Assembly?.Name?.Value ?? _inputModule.Name!.Value;

    /// <summary>
    /// Discovers all public types in the input assembly that should be included in the WinMD.
    /// This includes public classes, interfaces, structs, enums, and delegates.
    /// </summary>
    /// <returns>A list of <see cref="TypeDefinition"/> instances representing the public API surface.</returns>
    public IReadOnlyList<TypeDefinition> DiscoverPublicTypes()
    {
        List<TypeDefinition> publicTypes = [];

        foreach (TypeDefinition type in _inputModule.TopLevelTypes)
        {
            CollectPublicTypes(type, publicTypes);
        }

        return publicTypes;
    }

    /// <summary>
    /// Recursively collects public types, including nested public types.
    /// </summary>
    /// <param name="type">The type to check and potentially collect.</param>
    /// <param name="publicTypes">The list to add public types to.</param>
    private static void CollectPublicTypes(TypeDefinition type, List<TypeDefinition> publicTypes)
    {
        if (!type.IsPublic && !type.IsNestedPublic)
        {
            return;
        }

        // We include classes, interfaces, structs, enums, and delegates
        if (type.IsClass || type.IsInterface || type.IsValueType || type.IsEnum || type.IsDelegate)
        {
            publicTypes.Add(type);
        }

        // Recurse into nested types
        foreach (TypeDefinition nestedType in type.NestedTypes)
        {
            CollectPublicTypes(nestedType, publicTypes);
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Discovery;

/// <summary>
/// Analyzes a compiled assembly to discover its public WinRT API surface.
/// </summary>
internal sealed class AssemblyAnalyzer
{
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
    private static void CollectPublicTypes(TypeDefinition type, List<TypeDefinition> publicTypes)
    {
        if (!IsPublicType(type))
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

    /// <summary>
    /// Checks whether a type is public (visible outside the assembly).
    /// </summary>
    private static bool IsPublicType(TypeDefinition type)
    {
        return type.IsPublic || type.IsNestedPublic;
    }

    /// <summary>
    /// Checks whether a type is a WinRT type (has the WindowsRuntimeTypeAttribute).
    /// </summary>
    internal static bool IsWinRTType(TypeDefinition type)
    {
        return type.CustomAttributes.Any(
            attr => attr.Constructor?.DeclaringType?.Name?.Value == "WindowsRuntimeTypeAttribute");
    }

    /// <summary>
    /// Gets the assembly name from a WindowsRuntimeTypeAttribute on a type, if present.
    /// </summary>
    internal static string? GetAssemblyForWinRTType(TypeDefinition type)
    {
        foreach (CustomAttribute attr in type.CustomAttributes)
        {
            if (attr.Constructor?.DeclaringType?.Name?.Value == "WindowsRuntimeTypeAttribute" &&
                attr.Signature?.FixedArguments.Count > 0)
            {
                return attr.Signature.FixedArguments[0].Element as string;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the effective namespace of a type. For nested types, this walks up the
    /// declaring type chain since nested types have no namespace of their own in metadata.
    /// </summary>
    internal static string? GetEffectiveNamespace(TypeDefinition type)
    {
        if (type.Namespace is { Value.Length: > 0 })
        {
            return type.Namespace.Value;
        }

        // For nested types, walk up to the declaring type to find the namespace
        TypeDefinition? current = type.DeclaringType;
        while (current != null)
        {
            if (current.Namespace is { Value.Length: > 0 })
            {
                return current.Namespace.Value;
            }

            current = current.DeclaringType;
        }

        return null;
    }

    /// <summary>
    /// Gets the full qualified name of a type, including generic arity.
    /// For nested types, uses the effective namespace from the declaring type chain.
    /// </summary>
    internal static string GetQualifiedName(TypeDefinition type)
    {
        string name = type.Name!.Value;

        string? ns = GetEffectiveNamespace(type);
        return ns is { Length: > 0 } ? $"{ns}.{name}" : name;
    }

    /// <summary>
    /// Gets the full qualified name for an <see cref="ITypeDefOrRef"/>.
    /// For nested <see cref="TypeDefinition"/> types, uses the effective namespace from the declaring type chain.
    /// </summary>
    internal static string GetQualifiedName(ITypeDefOrRef type)
    {
        if (type is TypeDefinition td)
        {
            return GetQualifiedName(td);
        }

        string name = type.Name!.Value;
        string? ns = type.Namespace?.Value;

        return ns is { Length: > 0 } ? $"{ns}.{name}" : name;
    }
}
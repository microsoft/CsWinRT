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
            if (!IsPublicType(type))
            {
                continue;
            }

            // We include classes, interfaces, structs, enums, and delegates
            if (type.IsClass || type.IsInterface || type.IsValueType || type.IsEnum || IsDelegate(type))
            {
                publicTypes.Add(type);
            }
        }

        return publicTypes;
    }

    /// <summary>
    /// Checks whether a type is public (visible outside the assembly).
    /// </summary>
    private static bool IsPublicType(TypeDefinition type)
    {
        return type.IsPublic || type.IsNestedPublic;
    }

    /// <summary>
    /// Checks whether a type is a delegate (inherits from System.MulticastDelegate).
    /// </summary>
    internal static bool IsDelegate(TypeDefinition type)
    {
        return type.BaseType?.FullName == "System.MulticastDelegate";
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
    /// Gets the full qualified name of a type, including generic arity.
    /// </summary>
    internal static string GetQualifiedName(TypeDefinition type)
    {
        string name = type.Name!.Value;
        if (type.GenericParameters.Count > 0)
        {
            name += $"`{type.GenericParameters.Count}";
        }

        return type.Namespace is { Value: { Length: > 0 } ns }
            ? $"{ns}.{name}"
            : name;
    }

    /// <summary>
    /// Gets the full qualified name for an <see cref="ITypeDefOrRef"/>.
    /// </summary>
    internal static string GetQualifiedName(ITypeDefOrRef type)
    {
        string name = type.Name!.Value;
        if (type is TypeDefinition td && td.GenericParameters.Count > 0)
        {
            name += $"`{td.GenericParameters.Count}";
        }

        return type.Namespace is { Value: { Length: > 0 } ns }
            ? $"{ns}.{name}"
            : name;
    }
}

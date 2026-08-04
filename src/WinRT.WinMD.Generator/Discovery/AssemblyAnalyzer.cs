// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver;
using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Discovery;

/// <summary>
/// Analyzes a compiled assembly to discover its public Windows Runtime API surface.
/// </summary>
/// <remarks>
/// This type is responsible for scanning the input module and collecting all public top-level
/// types that should be represented in the output WinMD. It discovers public classes, interfaces,
/// structs, enums, and delegates. Nested types are intentionally ignored, since the Windows
/// Runtime type system does not support them.
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
    /// Discovers all public top-level types in the input assembly that should be included in the WinMD.
    /// This includes public classes, interfaces, structs, enums, and delegates. Nested types are ignored.
    /// </summary>
    /// <returns>A list of <see cref="TypeDefinition"/> instances representing the public API surface.</returns>
    public IReadOnlyList<TypeDefinition> DiscoverPublicTypes()
    {
        List<TypeDefinition> publicTypes = [];

        foreach (TypeDefinition type in _inputModule.TopLevelTypes)
        {
            if (!type.IsPublic)
            {
                continue;
            }

            // Skip ABI namespace types — these are source generator implementation details,
            // not WinRT types to be included in the .winmd.
            if (type.Namespace is Utf8String ns &&
                (ns.AsSpan().SequenceEqual("ABI"u8) || ns.AsSpan().StartsWith("ABI."u8)))
            {
                continue;
            }

            // Skip types implementing a Windows Runtime class declared in existing metadata. They provide the
            // implementation for a type that is already declared elsewhere, so declaring them here would emit a
            // second, conflicting definition of it.
            if (ImplementsExistingRuntimeClass(type))
            {
                continue;
            }

            // We include classes, interfaces, structs, enums, and delegates
            if (type.IsClass || type.IsInterface || type.IsValueType || type.IsEnum || type.IsDelegate)
            {
                publicTypes.Add(type);
            }
        }

        return publicTypes;
    }

    /// <summary>
    /// Checks whether a type implements a Windows Runtime class declared in existing metadata, which it does by
    /// deriving from one of the abstract base classes CsWinRT generates for that purpose.
    /// </summary>
    /// <param name="type">The <see cref="TypeDefinition"/> to inspect.</param>
    /// <returns>Whether <paramref name="type"/> implements a class declared in existing metadata.</returns>
    private bool ImplementsExistingRuntimeClass(TypeDefinition type)
    {
        RuntimeContext? runtimeContext = _inputModule.RuntimeContext;

        for (TypeDefinition? current = type.BaseType?.Resolve(runtimeContext); current is not null;)
        {
            foreach (CustomAttribute attribute in current.CustomAttributes)
            {
                if (attribute.Constructor?.DeclaringType?.FullName is
                    "WindowsRuntime.WindowsRuntimeImplementableClassAttribute" or
                    "WindowsRuntime.WindowsRuntimeImplementableClassFactoryAttribute")
                {
                    return true;
                }
            }

            current = current.BaseType?.Resolve(runtimeContext);
        }

        return false;
    }
}
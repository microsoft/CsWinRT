// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Reads the implementable bases that referenced reference projections built with <c>CsWinRTImplementWinMDTypes</c>
/// declare, from their <c>[WindowsRuntimeReferenceAssemblyMetadata]</c> entries.
/// </summary>
/// <remarks>
/// <para>
/// The bases carry no marker: each reference projection records the Windows Runtime classes it declares bases
/// for, and each base has a well-known name derived from its class (<c>ABI.&lt;Namespace&gt;.&lt;Class&gt;</c>
/// for the instance base and <c>ABI.&lt;Namespace&gt;.&lt;Class&gt;ActivationFactory</c> for the activation
/// factory base). A class may only have one of the two. The key and names must match the ones the projection
/// writer emits.
/// </para>
/// <para>
/// This type is shared by the source generator and the analyzers, so that both apply the same rules. It is safe
/// to use concurrently.
/// </para>
/// </remarks>
internal sealed class ImplementableClassLookup
{
    /// <summary>
    /// The fully qualified name of the key/value metadata attribute.
    /// </summary>
    private const string MetadataAttributeName = "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyMetadataAttribute";

    /// <summary>
    /// The key for a Windows Runtime class the reference projection declares generated bases for.
    /// </summary>
    private const string ImplementableClassKey = "CsWinRT.ImplementableClass.v1";

    /// <summary>
    /// The implementable bases declared by each assembly inspected so far.
    /// </summary>
    private readonly ConcurrentDictionary<IAssemblySymbol, AssemblyInfo> _assemblies = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Gets the Windows Runtime class a type is the generated instance base for.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The fully qualified Windows Runtime class name, or <see langword="null"/> if <paramref name="type"/> is not an instance base.</returns>
    public string? GetInstanceRuntimeClassName(INamedTypeSymbol type)
    {
        return TryGetFullName(type, out string? fullName) && GetAssemblyInfo(type.ContainingAssembly).InstanceBases.TryGetValue(fullName, out string? runtimeClassName)
            ? runtimeClassName
            : null;
    }

    /// <summary>
    /// Gets the Windows Runtime class a type is the generated activation factory base for.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The fully qualified Windows Runtime class name, or <see langword="null"/> if <paramref name="type"/> is not an activation factory base.</returns>
    public string? GetFactoryRuntimeClassName(INamedTypeSymbol type)
    {
        return TryGetFullName(type, out string? fullName) && GetAssemblyInfo(type.ContainingAssembly).FactoryBases.TryGetValue(fullName, out string? runtimeClassName)
            ? runtimeClassName
            : null;
    }

    /// <summary>
    /// Finds the nearest generated instance base that a type is or derives from.
    /// </summary>
    /// <param name="type">The type whose base classes to inspect.</param>
    /// <returns>The generated instance base, or <see langword="null"/> if there is none.</returns>
    public INamedTypeSymbol? GetInstanceBase(INamedTypeSymbol? type)
    {
        return GetImplementableBase(type, GetInstanceRuntimeClassName);
    }

    /// <summary>
    /// Finds the nearest generated activation factory base that a type is or derives from.
    /// </summary>
    /// <param name="type">The type whose base classes to inspect.</param>
    /// <returns>The generated activation factory base, or <see langword="null"/> if there is none.</returns>
    public INamedTypeSymbol? GetFactoryBase(INamedTypeSymbol? type)
    {
        return GetImplementableBase(type, GetFactoryRuntimeClassName);
    }

    /// <summary>
    /// Gets the generated activation factory base for the same Windows Runtime class as a given instance base.
    /// </summary>
    /// <param name="instanceBase">The generated instance base.</param>
    /// <returns>The generated activation factory base, or <see langword="null"/> if the class has none.</returns>
    public INamedTypeSymbol? GetFactoryBaseForInstanceBase(INamedTypeSymbol instanceBase)
    {
        // The factory base sits next to the class base it activates, in the same assembly, under a reserved name
        string factoryBaseName = $"{instanceBase.ContainingNamespace.ToDisplayString()}.{instanceBase.Name}ActivationFactory";

        return instanceBase.ContainingAssembly.GetTypeByMetadataName(factoryBaseName) is INamedTypeSymbol factoryBase &&
               GetFactoryRuntimeClassName(factoryBase) is not null
            ? factoryBase
            : null;
    }

    /// <summary>
    /// Gets whether a generated activation factory base leaves only <c>ActivateInstance()</c> to implement, i.e. its
    /// class has no constructors taking arguments and no statics, so CsWinRT can implement the factory itself.
    /// </summary>
    /// <param name="factoryBase">The activation factory base to inspect.</param>
    /// <returns>Whether <c>ActivateInstance()</c> is the only abstract member of <paramref name="factoryBase"/>.</returns>
    public bool HasDefaultActivationOnly(INamedTypeSymbol factoryBase)
    {
        return GetFactoryRuntimeClassName(factoryBase) is not null &&
               factoryBase.GetMembers().Where(static member => member.IsAbstract).ToArray() is
                   [IMethodSymbol { Name: "ActivateInstance", Parameters.IsEmpty: true }];
    }

    /// <summary>
    /// Finds the nearest generated abstract base class that a type is or derives from.
    /// </summary>
    /// <param name="type">The type whose base classes to inspect.</param>
    /// <param name="getRuntimeClassName">Gets the Windows Runtime class a type is a generated base of the wanted kind for, if any.</param>
    /// <returns>The generated base class, or <see langword="null"/> if there is none.</returns>
    private static INamedTypeSymbol? GetImplementableBase(INamedTypeSymbol? type, Func<INamedTypeSymbol, string?> getRuntimeClassName)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (getRuntimeClassName(current) is not null)
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the full metadata name of a type shaped like a generated base: an abstract, non-static, top-level class
    /// in a named namespace.
    /// </summary>
    private static bool TryGetFullName(INamedTypeSymbol type, [NotNullWhen(true)] out string? fullName)
    {
        if (type is not { TypeKind: TypeKind.Class, IsAbstract: true, IsSealed: false, IsStatic: false, ContainingType: null, ContainingAssembly: not null } ||
            type.ContainingNamespace is not { IsGlobalNamespace: false } containingNamespace)
        {
            fullName = null;

            return false;
        }

        fullName = $"{containingNamespace.ToDisplayString()}.{type.MetadataName}";

        return true;
    }

    /// <summary>
    /// Gets the implementable bases declared by an assembly, reading them on first use.
    /// </summary>
    private AssemblyInfo GetAssemblyInfo(IAssemblySymbol assembly)
    {
        return _assemblies.GetOrAdd(assembly, static assemblySymbol =>
        {
            AssemblyInfo info = new();

            foreach (AttributeData attribute in assemblySymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != MetadataAttributeName ||
                    attribute.ConstructorArguments is not [{ Value: string key }, { Value: string runtimeClassName }])
                {
                    continue;
                }

                // Only the type asked about is checked against these, so a base the class does not have is never matched
                if (key == ImplementableClassKey)
                {
                    info.InstanceBases[$"ABI.{runtimeClassName}"] = runtimeClassName;
                    info.FactoryBases[$"ABI.{runtimeClassName}ActivationFactory"] = runtimeClassName;
                }
            }

            return info;
        });
    }

    /// <summary>
    /// The implementable bases declared by an assembly.
    /// </summary>
    private sealed class AssemblyInfo
    {
        /// <summary>
        /// Gets the full names of the instance bases, mapped to the Windows Runtime class each stands for.
        /// </summary>
        public Dictionary<string, string> InstanceBases { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the full names of the activation factory bases, mapped to the Windows Runtime class each activates.
        /// </summary>
        public Dictionary<string, string> FactoryBases { get; } = new(StringComparer.Ordinal);
    }
}

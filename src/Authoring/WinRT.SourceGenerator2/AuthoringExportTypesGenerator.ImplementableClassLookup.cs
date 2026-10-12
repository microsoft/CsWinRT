// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <inheritdoc cref="AuthoringExportTypesGenerator"/>
public partial class AuthoringExportTypesGenerator
{
    /// <summary>
    /// Reads the implementable bases that referenced reference projections built with <c>CsWinRTImplementWinMDTypes</c>
    /// declare, from their <c>[WindowsRuntimeReferenceAssemblyMetadata]</c> entries.
    /// </summary>
    /// <remarks>
    /// The bases carry no marker: each reference projection records the Windows Runtime classes it declares bases
    /// for, and each base has a well-known name derived from its class (<c>ABI.&lt;Namespace&gt;.&lt;Class&gt;</c>
    /// for the instance base and <c>ABI.&lt;Namespace&gt;.&lt;Class&gt;ActivationFactory</c> for the activation
    /// factory base). A class may only have one of the two. The key and names must match the ones the projection
    /// writer emits.
    /// </remarks>
    private sealed class ImplementableClassLookup
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
        private readonly Dictionary<IAssemblySymbol, AssemblyInfo> _assemblies = new(SymbolEqualityComparer.Default);

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
            if (_assemblies.TryGetValue(assembly, out AssemblyInfo? info))
            {
                return info;
            }

            info = new AssemblyInfo();

            foreach (AttributeData attribute in assembly.GetAttributes())
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

            _assemblies.Add(assembly, info);

            return info;
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
}

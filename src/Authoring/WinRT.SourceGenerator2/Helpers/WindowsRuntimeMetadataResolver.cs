// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.References;
using WindowsRuntime.InteropGenerator.Helpers;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Validates the identity of types and reads Windows Runtime metadata from the consuming compilation, without loading implementation assemblies.
/// </summary>
/// <param name="compilation">The consuming compilation.</param>
/// <param name="windowsRuntimeAssembly">The assembly containing the intercepted marshalling API.</param>
/// <param name="token">The cancellation token.</param>
internal sealed class WindowsRuntimeMetadataResolver(
    Compilation compilation,
    IAssemblySymbol windowsRuntimeAssembly,
    CancellationToken token)
{
    /// <summary>
    /// The metadata shared by all the calls analyzed against the same compilation.
    /// </summary>
    private readonly WindowsRuntimeMetadataCache _cache = WindowsRuntimeMetadataCache.GetOrCreate(compilation);

    /// <summary>
    /// Gets the validated definition and metadata name for a supported type.
    /// </summary>
    /// <param name="type">The input type.</param>
    /// <param name="definition">Its original definition.</param>
    /// <param name="name">The definition's metadata name.</param>
    /// <returns>Whether the type has a supported Windows Runtime definition.</returns>
    public bool TryGetDefinition(
        ITypeSymbol type,
        [NotNullWhen(true)] out INamedTypeSymbol? definition,
        [NotNullWhen(true)] out string? name)
    {
        definition = null;
        name = null;

        // Only closed, concrete, named types can be Windows Runtime types. Unbound generics, static classes, native integers, tuples
        // (which are just 'ValueTuple<...>' with no Windows Runtime equivalent) and unresolved error types have no valid IID.
        if (type is not INamedTypeSymbol
            {
                IsUnboundGenericType: false,
                IsStatic: false,
                IsNativeIntegerType: false,
                IsTupleType: false,
                TypeKind: not TypeKind.Error
            } named)
        {
            return false;
        }

        definition = named.OriginalDefinition;
        name = definition.GetFullyQualifiedMetadataName();

        return IsKnownDefinition(definition, name);
    }

    /// <summary>
    /// Checks a definition against the types provided by the runtime assembly or the framework, rather than a same-named type.
    /// </summary>
    /// <param name="type">The definition to check.</param>
    /// <param name="name">Its metadata name.</param>
    /// <returns>Whether this is a runtime or framework definition.</returns>
    public bool IsRuntimeOrFrameworkDefinition(INamedTypeSymbol type, string name)
    {
        return SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, windowsRuntimeAssembly) ||
            IsFrameworkDefinition(type, name);
    }

    /// <summary>
    /// Resolves definition IIDs for mapped types or compiler-visible projected metadata.
    /// </summary>
    /// <param name="type">The validated definition.</param>
    /// <param name="name">Its metadata name.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="iid">The IID.</param>
    /// <returns>Whether a definition IID is available.</returns>
    public bool TryGetDefinitionIid(
        INamedTypeSymbol type,
        string name,
        bool useWindowsUIXamlProjections,
        out Guid iid)
    {
        // Well-known types (e.g. custom-mapped and manually projected ones) have their IIDs in a shared table
        if (WellKnownWindowsRuntimeIids.TryGetIid(name, useWindowsUIXamlProjections, out iid))
        {
            return true;
        }

        // Projected types declare their IID with a '[Guid]' attribute. The attribute type is resolved through the
        // framework facades, so that a same-named attribute from another assembly can't supply an IID.
        if (GetFrameworkDefinition("System.Runtime.InteropServices.GuidAttribute") is not INamedTypeSymbol guidAttribute ||
            !type.TryGetAttributeWithType(guidAttribute, out AttributeData? attribute))
        {
            iid = default;

            return false;
        }

        // The IID is the single string argument of the attribute
        if (attribute.ConstructorArguments is not [{ Value: string text }])
        {
            iid = default;

            return false;
        }

        return Guid.TryParse(text, out iid);
    }

    /// <summary>
    /// Decodes a reference projection's default-interface metadata at the input boundary.
    /// </summary>
    /// <param name="type">The runtime class definition.</param>
    /// <param name="name">Its metadata name.</param>
    /// <param name="defaultInterface">The recorded default interface.</param>
    /// <returns>Whether a supported default interface was recorded.</returns>
    public bool TryGetDefaultInterface(
        INamedTypeSymbol type,
        string name,
        [NotNullWhen(true)] out INamedTypeSymbol? defaultInterface)
    {
        // Resolves the recorded default interface of a runtime class (cached below, as this involves parsing a type name)
        static INamedTypeSymbol? ResolveDefaultInterface(
            INamedTypeSymbol type,
            (WindowsRuntimeMetadataResolver Resolver, Compilation Compilation, CancellationToken Token, string Name) state)
        {
            // Look up the recorded name for this class
            if (!state.Resolver.GetDefaultInterfaceNames(type.ContainingAssembly).TryGetValue(state.Name, out string? interfaceName))
            {
                return null;
            }

            // Resolve the recorded name to a type
            if (!WindowsRuntimeTypeNameParser.TryParse(interfaceName, state.Compilation, type.ContainingAssembly, state.Token, out INamedTypeSymbol? resolved))
            {
                return null;
            }

            // Make sure it's actually an interface (not a class or struct)
            if (resolved.TypeKind != TypeKind.Interface)
            {
                return null;
            }

            return resolved;
        }

        // Resolving the default interface involves parsing a type name, so cache the result (including misses) per class
        defaultInterface = _cache.DefaultInterfaces.GetOrAdd(type, ResolveDefaultInterface, (Resolver: this, Compilation: compilation, Token: token, Name: name));

        return defaultInterface is not null;
    }

    /// <summary>
    /// Checks identity before applying name-based mappings or trusting projected metadata.
    /// </summary>
    /// <param name="type">The type definition.</param>
    /// <param name="name">Its metadata name.</param>
    /// <returns>Whether this is a supported framework or projected definition.</returns>
    private bool IsKnownDefinition(INamedTypeSymbol type, string name)
    {
        INamedTypeSymbol? apiContractAttribute = windowsRuntimeAssembly.GetTypeByMetadataName("Windows.Foundation.Metadata.ApiContractAttribute");

        // Types from the current assembly can't be trusted (they could just be authored to look like Windows Runtime types), nested
        // types are not valid Windows Runtime types, and API contracts are metadata-only types that are never marshalled as values.
        if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) ||
            type.ContainingType is not null ||
            (apiContractAttribute is not null && type.HasAttributeWithType(apiContractAttribute)))
        {
            return false;
        }

        // Custom-mapped .NET types (and 'Guid') are only valid if they're the actual framework definition, not a same-named type.
        // The two attribute types are excluded because they map to Windows Runtime metadata attributes, which are not marshalled.
        if (name.StartsWith("System.", StringComparison.Ordinal) &&
            (TypeMapping.TryFindMappedTypeName(name, false, out _) || name == "System.Guid"))
        {
            return name is not ("System.AttributeTargets" or "System.AttributeUsageAttribute") &&
                IsFrameworkDefinition(type, name);
        }

        // Attribute types are metadata-only and never marshalled as values, so they're excluded from the checks below
        INamedTypeSymbol? attributeType = GetFrameworkDefinition("System.Attribute");
        bool isAttribute = attributeType is not null && type.InheritsFromType(attributeType);

        // Manually projected types in the runtime assembly must carry the Windows Runtime metadata markers, which excludes
        // runtime-only helper types. Attributes are excluded too, as they're metadata-only and never marshalled as values.
        if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, windowsRuntimeAssembly))
        {
            INamedTypeSymbol? contractVersionAttribute = windowsRuntimeAssembly.GetTypeByMetadataName("Windows.Foundation.Metadata.ContractVersionAttribute");
            INamedTypeSymbol? windowsRuntimeTypeAttribute = windowsRuntimeAssembly.GetTypeByMetadataName("WindowsRuntime.WindowsRuntimeTypeAttribute");

            return ((contractVersionAttribute is not null && type.HasAttributeWithType(contractVersionAttribute)) ||
                    (windowsRuntimeTypeAttribute is not null && type.HasAttributeWithType(windowsRuntimeTypeAttribute))) &&
                !isAttribute;
        }

        WindowsRuntimeMetadataCache.ProjectionInfo metadata = GetProjectionInfo(type.ContainingAssembly);

        // Everything else needs to come from a reference projection assembly. Components are excluded because their types are
        // authored in managed code and not projected, and attributes are excluded for the same reason as above.
        return metadata.IsProjection && !metadata.IsComponent && !isAttribute;
    }

    /// <summary>
    /// Checks a definition against the framework type resolved by the compiler.
    /// </summary>
    /// <param name="type">The definition to check.</param>
    /// <param name="name">Its metadata name.</param>
    /// <returns>Whether this is the framework definition rather than a same-named type.</returns>
    private bool IsFrameworkDefinition(INamedTypeSymbol type, string name)
    {
        return SymbolEqualityComparer.Default.Equals(type, GetFrameworkDefinition(name));
    }

    /// <summary>
    /// Resolves framework definitions through their trusted facades, not namespace or signing-key similarity.
    /// </summary>
    /// <param name="name">The definition's metadata name.</param>
    /// <returns>The framework definition, if available.</returns>
    private INamedTypeSymbol? GetFrameworkDefinition(string name)
    {
        // Looks up the definition in the core library first, and then in the trusted facade assemblies that forward to it
        static INamedTypeSymbol? ResolveFrameworkDefinition(string name, (Compilation Compilation, CancellationToken Token) state)
        {
            // Most framework types live directly in the core library, so check there first
            IAssemblySymbol coreLibrary = state.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;

            INamedTypeSymbol? type = coreLibrary.GetTypeByMetadataName(name);

            if (type is not null)
            {
                return type;
            }

            // Reference assemblies split types across facade assemblies, which forward to the actual definitions. If the type
            // wasn't found in the core library, look through the referenced facades that we know and trust.
            foreach (IAssemblySymbol assembly in state.Compilation.SourceModule.ReferencedAssemblySymbols)
            {
                state.Token.ThrowIfCancellationRequested();

                // Only consider well-known framework assemblies, so that a same-named assembly can't provide the type
                byte[]? publicKeyToken = assembly.Identity.Name switch
                {
                    "System.Runtime" => WellKnownPublicKeyTokens.SystemRuntime,
                    "System.ObjectModel" => WellKnownPublicKeyTokens.SystemObjectModel,
                    "System.ComponentModel" => WellKnownPublicKeyTokens.SystemComponentModel,
                    "System.Numerics.Vectors" => WellKnownPublicKeyTokens.SystemNumericsVectors,
                    "System.Runtime.InteropServices" => WellKnownPublicKeyTokens.SystemRuntimeInteropServices,
                    _ => null
                };

                // The assembly also needs to be signed with the expected key, and then the type is either defined there or forwarded
                if (publicKeyToken is null ||
                    !assembly.Identity.PublicKeyToken.AsSpan().SequenceEqual(publicKeyToken))
                {
                    continue;
                }

                type = assembly.GetTypeByMetadataName(name) ?? assembly.ResolveForwardedType(name);

                if (type is not null)
                {
                    return type;
                }
            }

            return null;
        }

        // Framework definitions are resolved repeatedly for the same names, so cache the results (including misses)
        return _cache.FrameworkDefinitions.GetOrAdd(name, ResolveFrameworkDefinition, (Compilation: compilation, Token: token));
    }

    /// <summary>
    /// Gets whether an assembly is a reference projection or an authored component.
    /// </summary>
    /// <param name="assembly">The input assembly.</param>
    /// <returns>The kind of the assembly.</returns>
    private WindowsRuntimeMetadataCache.ProjectionInfo GetProjectionInfo(IAssemblySymbol assembly)
    {
        // Checks the marker attributes from the runtime assembly, to tell whether this is a reference projection or an authored component
        static WindowsRuntimeMetadataCache.ProjectionInfo ComputeProjectionInfo(IAssemblySymbol assembly, IAssemblySymbol windowsRuntimeAssembly)
        {
            INamedTypeSymbol? referenceAssemblyAttribute = windowsRuntimeAssembly.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute");
            INamedTypeSymbol? componentAssemblyAttribute = windowsRuntimeAssembly.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeComponentAssemblyAttribute");

            bool isProjection = referenceAssemblyAttribute is not null && assembly.HasAttributeWithType(referenceAssemblyAttribute);
            bool isComponent = componentAssemblyAttribute is not null && assembly.HasAttributeWithType(componentAssemblyAttribute);

            return new WindowsRuntimeMetadataCache.ProjectionInfo(isProjection, isComponent);
        }

        // The same assembly is queried for every type it defines, so cache the result
        return _cache.ProjectionInfos.GetOrAdd(assembly, ComputeProjectionInfo, windowsRuntimeAssembly);
    }

    /// <summary>
    /// Indexes the default interface names recorded by a reference projection.
    /// </summary>
    /// <param name="assembly">The input assembly.</param>
    /// <returns>The recorded default interface names, by runtime class name.</returns>
    /// <remarks>
    /// This is separate from <see cref="GetProjectionInfo"/> because a projection can record thousands of entries, which
    /// are only needed to resolve runtime classes. Calls that only deal with interfaces and delegates never pay for them.
    /// </remarks>
    private IReadOnlyDictionary<string, string> GetDefaultInterfaceNames(IAssemblySymbol assembly)
    {
        // Reads the default interface entries recorded by the reference projection, which can be thousands of them
        static IReadOnlyDictionary<string, string> IndexDefaultInterfaceNames(
            IAssemblySymbol assembly,
            (IAssemblySymbol WindowsRuntimeAssembly, CancellationToken Token) state)
        {
            INamedTypeSymbol? referenceMetadataAttribute = state.WindowsRuntimeAssembly.GetTypeByMetadataName(WindowsRuntimeReferenceAssemblyMetadata.AttributeTypeName);
            Dictionary<string, string> names = new(StringComparer.Ordinal);

            if (referenceMetadataAttribute is null)
            {
                return names;
            }

            // Reference projections record the default interface of each runtime class as a metadata entry. Each entry is a key
            // with a known prefix followed by the class name, and the value is the name of its default interface.
            foreach (AttributeData attribute in assembly.GetAttributes(referenceMetadataAttribute))
            {
                state.Token.ThrowIfCancellationRequested();

                if (attribute.ConstructorArguments is [{ Value: string key }, { Value: string value }] &&
                    key.StartsWith(WindowsRuntimeReferenceAssemblyMetadata.DefaultInterfacePrefix, StringComparison.Ordinal))
                {
                    names[key[WindowsRuntimeReferenceAssemblyMetadata.DefaultInterfacePrefix.Length..]] = value;
                }
            }

            return names;
        }

        // The same assembly is queried for every runtime class it defines, so cache the index
        return _cache.DefaultInterfaceNames.GetOrAdd(assembly, IndexDefaultInterfaceNames, (WindowsRuntimeAssembly: windowsRuntimeAssembly, Token: token));
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.References;
using WindowsRuntime.InteropGenerator.Helpers;

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
    /// Projection metadata shared by both XAML modes within one semantic transform.
    /// </summary>
    private readonly Dictionary<IAssemblySymbol, ProjectionInfo> _projectionMetadata = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Framework definitions resolved within the consuming compilation, including type forwards.
    /// </summary>
    private readonly Dictionary<string, INamedTypeSymbol?> _frameworkDefinitions = new(StringComparer.Ordinal);

    /// <summary>
    /// Default interfaces decoded only for classes used by the current call.
    /// </summary>
    private readonly Dictionary<INamedTypeSymbol, INamedTypeSymbol?> _defaultInterfaces = new(SymbolEqualityComparer.Default);

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

        if (type is not INamedTypeSymbol { IsUnboundGenericType: false, IsStatic: false, IsNativeIntegerType: false } named ||
            named.TypeKind == TypeKind.Error ||
            named.IsTupleType)
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
        if (!_defaultInterfaces.TryGetValue(type, out defaultInterface))
        {
            // Look up the recorded name, then resolve it and make sure it's actually an interface (not a class or struct)
            if (GetProjectionInfo(type.ContainingAssembly).DefaultInterfaces.TryGetValue(name, out string? interfaceName) &&
                WindowsRuntimeTypeNameParser.TryParse(interfaceName, compilation, type.ContainingAssembly, token, out INamedTypeSymbol? resolved) &&
                resolved.TypeKind == TypeKind.Interface)
            {
                defaultInterface = resolved;
            }

            _defaultInterfaces.Add(type, defaultInterface);
        }

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
        if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) ||
            type.ContainingType is not null ||
            HasWindowsRuntimeAttribute(type, "Windows.Foundation.Metadata.ApiContractAttribute"))
        {
            return false;
        }

        if (name.StartsWith("System.", StringComparison.Ordinal) &&
            (TypeMapping.TryFindMappedTypeName(name, false, out _) || name == "System.Guid"))
        {
            return name is not ("System.AttributeTargets" or "System.AttributeUsageAttribute") &&
                IsFrameworkDefinition(type, name);
        }

        if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, windowsRuntimeAssembly))
        {
            return (HasWindowsRuntimeAttribute(type, "Windows.Foundation.Metadata.ContractVersionAttribute") ||
                    HasWindowsRuntimeAttribute(type, "WindowsRuntime.WindowsRuntimeTypeAttribute")) &&
                !InheritsAttribute(type);
        }

        ProjectionInfo metadata = GetProjectionInfo(type.ContainingAssembly);

        return metadata.IsProjection && !metadata.IsComponent && !InheritsAttribute(type);
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
        if (_frameworkDefinitions.TryGetValue(name, out INamedTypeSymbol? type))
        {
            return type;
        }

        IAssemblySymbol coreLibrary = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;

        type = coreLibrary.GetTypeByMetadataName(name);

        if (type is null)
        {
            foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                token.ThrowIfCancellationRequested();

                byte[]? publicKeyToken = assembly.Identity.Name switch
                {
                    "System.Runtime" => WellKnownPublicKeyTokens.SystemRuntime,
                    "System.ObjectModel" => WellKnownPublicKeyTokens.SystemObjectModel,
                    "System.ComponentModel" => WellKnownPublicKeyTokens.SystemComponentModel,
                    "System.Numerics.Vectors" => WellKnownPublicKeyTokens.SystemNumericsVectors,
                    "System.Runtime.InteropServices" => WellKnownPublicKeyTokens.SystemRuntimeInteropServices,
                    _ => null
                };

                if (publicKeyToken is not null &&
                    assembly.Identity.PublicKeyToken.AsSpan().SequenceEqual(publicKeyToken))
                {
                    type = assembly.GetTypeByMetadataName(name) ?? assembly.ResolveForwardedType(name);

                    if (type is not null)
                    {
                        break;
                    }
                }
            }
        }

        _frameworkDefinitions.Add(name, type);

        return type;
    }

    /// <summary>
    /// Checks an attribute against its definition in the referenced runtime assembly.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="name">The attribute's metadata name.</param>
    /// <returns>Whether the type carries that runtime attribute.</returns>
    private bool HasWindowsRuntimeAttribute(INamedTypeSymbol type, string name)
    {
        return windowsRuntimeAssembly.GetTypeByMetadataName(name) is INamedTypeSymbol attributeType &&
            type.HasAttributeWithType(attributeType);
    }

    /// <summary>
    /// Rejects metadata-only Windows Runtime attributes.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Whether this is an attribute type.</returns>
    private bool InheritsAttribute(INamedTypeSymbol type)
    {
        INamedTypeSymbol? attributeType = GetFrameworkDefinition("System.Attribute");

        for (INamedTypeSymbol? parent = type.BaseType; parent is not null; parent = parent.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(parent, attributeType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Indexes reference-only projection metadata without retaining symbols beyond this semantic transform.
    /// </summary>
    /// <param name="assembly">The input assembly.</param>
    /// <returns>The indexed projection metadata.</returns>
    private ProjectionInfo GetProjectionInfo(IAssemblySymbol assembly)
    {
        if (_projectionMetadata.TryGetValue(assembly, out ProjectionInfo? info))
        {
            return info;
        }

        INamedTypeSymbol? referenceAssemblyAttribute = windowsRuntimeAssembly.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute");
        INamedTypeSymbol? componentAssemblyAttribute = windowsRuntimeAssembly.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeComponentAssemblyAttribute");
        INamedTypeSymbol? referenceMetadataAttribute = windowsRuntimeAssembly.GetTypeByMetadataName(WindowsRuntimeReferenceAssemblyMetadata.AttributeTypeName);

        bool isProjection = false;
        bool isComponent = false;
        Dictionary<string, string> defaultInterfaces = new(StringComparer.Ordinal);

        foreach (AttributeData attribute in assembly.GetAttributes())
        {
            token.ThrowIfCancellationRequested();

            if (attribute.AttributeClass is not INamedTypeSymbol attributeType)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(attributeType, referenceAssemblyAttribute))
            {
                isProjection = true;
            }
            else if (SymbolEqualityComparer.Default.Equals(attributeType, componentAssemblyAttribute))
            {
                isComponent = true;
            }
            else if (SymbolEqualityComparer.Default.Equals(attributeType, referenceMetadataAttribute) &&
                     attribute.ConstructorArguments is [{ Value: string key }, { Value: string value }] &&
                     key.StartsWith(WindowsRuntimeReferenceAssemblyMetadata.DefaultInterfacePrefix, StringComparison.Ordinal))
            {
                defaultInterfaces[key[WindowsRuntimeReferenceAssemblyMetadata.DefaultInterfacePrefix.Length..]] = value;
            }
        }

        info = new(isProjection, isComponent, defaultInterfaces);

        _projectionMetadata.Add(assembly, info);

        return info;
    }

    /// <summary>
    /// Reference-only metadata for one assembly.
    /// </summary>
    /// <param name="IsProjection">Whether the assembly contains reference projections.</param>
    /// <param name="IsComponent">Whether the assembly is an authored component.</param>
    /// <param name="DefaultInterfaces">The runtime classes' recorded default interface names.</param>
    private sealed record ProjectionInfo(
        bool IsProjection,
        bool IsComponent,
        IReadOnlyDictionary<string, string> DefaultInterfaces);
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.References;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.ProjectionWriter.Helpers;

#pragma warning disable IDE0072

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Resolves Windows Runtime identities using compiler-visible metadata, without loading implementation assemblies.
/// </summary>
internal static class WindowsRuntimeIidResolver
{
    /// <summary>
    /// Tries to compute the IID represented by a closed type.
    /// </summary>
    /// <param name="type">The call's type argument.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <param name="useWindowsUIXaml">Whether to use UWP XAML mappings.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="iid">The computed IID.</param>
    /// <returns>Whether the type has a statically known Windows Runtime IID.</returns>
    public static bool TryGetIid(ITypeSymbol type, Compilation compilation, bool useWindowsUIXaml, CancellationToken token, out Guid iid)
    {
        Resolver resolver = new(compilation, useWindowsUIXaml, token);

        if (!resolver.TryGetSignature(type, out string? signature))
        {
            iid = default;
            return false;
        }

        if (type.SpecialType == SpecialType.System_Object)
        {
            iid = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");
            return true;
        }

        if (type is IArrayTypeSymbol or INamedTypeSymbol { IsGenericType: true })
        {
            iid = GuidGenerator.Generate(signature);
            return true;
        }

        if (type.IsValueType || type.SpecialType == SpecialType.System_String)
        {
            iid = default;
            return false;
        }

        if (type is INamedTypeSymbol named)
        {
            string name = GetMetadataName(named);

            if (name == "System.EventHandler")
            {
                iid = default;
                return false;
            }

            if (TypeMapping.TryFindMappedTypeIID(name, useWindowsUIXaml, out iid))
            {
                return true;
            }

            if (signature.StartsWith("delegate(", StringComparison.Ordinal))
            {
                return Guid.TryParse(signature.AsSpan(9, signature.Length - 10), out iid);
            }

            if (signature.StartsWith("rc(", StringComparison.Ordinal))
            {
                string defaultInterface = signature[(signature.IndexOf(';') + 1)..^1];
                if (defaultInterface.StartsWith("pinterface(", StringComparison.Ordinal) && defaultInterface.EndsWith(')'))
                {
                    iid = GuidGenerator.Generate(defaultInterface);
                    return true;
                }

                return Guid.TryParse(defaultInterface, out iid);
            }

            return Guid.TryParse(signature, out iid);
        }

        iid = default;
        return false;
    }

    /// <summary>
    /// Gets the metadata name used by the shared mapped-type tables.
    /// </summary>
    /// <param name="type">The type definition.</param>
    /// <returns>The fully qualified metadata name.</returns>
    private static string GetMetadataName(INamedTypeSymbol type)
    {
        return type.ContainingType is { } parent
            ? $"{GetMetadataName(parent)}+{type.MetadataName}"
            : type.ContainingNamespace.IsGlobalNamespace
                ? type.MetadataName
                : $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";
    }

    /// <summary>
    /// Reference-only projection metadata.
    /// </summary>
    private sealed class ProjectionInfo
    {
        /// <summary>
        /// Creates a cached index for an assembly.
        /// </summary>
        /// <param name="assembly">The input assembly.</param>
        public ProjectionInfo(IAssemblySymbol assembly)
        {
            Dictionary<string, string> interfaces = new(StringComparer.Ordinal);

            foreach (AttributeData attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ContainingAssembly.Identity.Name != "WinRT.Runtime")
                {
                    continue;
                }

                switch (attribute.AttributeClass.ToDisplayString())
                {
                    case "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute":
                        IsProjection = true;
                        break;
                    case "WindowsRuntime.InteropServices.WindowsRuntimeComponentAssemblyAttribute":
                        IsComponent = true;
                        break;
                    case WindowsRuntimeReferenceAssemblyMetadata.AttributeTypeName when
                        attribute.ConstructorArguments is [{ Value: string key }, { Value: string value }] &&
                        key.StartsWith(WindowsRuntimeReferenceAssemblyMetadata.DefaultInterfaceSignaturePrefix, StringComparison.Ordinal):
                        interfaces[key[WindowsRuntimeReferenceAssemblyMetadata.DefaultInterfaceSignaturePrefix.Length..]] = value;
                        break;
                    default:
                        break;
                }
            }

            DefaultInterfaces = interfaces.ToFrozenDictionary(StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets whether the assembly contains reference projections.
        /// </summary>
        public bool IsProjection { get; }

        /// <summary>
        /// Gets whether the assembly is an authored component.
        /// </summary>
        public bool IsComponent { get; }

        /// <summary>
        /// Gets the default-interface signatures.
        /// </summary>
        public FrozenDictionary<string, string> DefaultInterfaces { get; }
    }

    /// <summary>
    /// Per-call signature resolution state. No compiler objects escape the semantic transform.
    /// </summary>
    /// <param name="compilation">The consuming compilation.</param>
    /// <param name="useWindowsUIXaml">Whether to use UWP XAML mappings.</param>
    /// <param name="token">The cancellation token.</param>
    private sealed class Resolver(Compilation compilation, bool useWindowsUIXaml, CancellationToken token)
    {
        /// <summary>
        /// Reference metadata indexed only for this signature resolution.
        /// </summary>
        private readonly Dictionary<IAssemblySymbol, ProjectionInfo> _projectionMetadata = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Guards against malformed recursive value metadata.
        /// </summary>
        private readonly HashSet<ITypeSymbol> _activeTypes = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Computes a supported Windows Runtime parameterized signature.
        /// </summary>
        /// <param name="type">The input type.</param>
        /// <param name="signature">The resulting signature.</param>
        /// <returns>Whether sufficient, valid metadata is available.</returns>
        public bool TryGetSignature(ITypeSymbol type, [NotNullWhen(true)] out string? signature)
        {
            token.ThrowIfCancellationRequested();
            signature = null;

            if (!_activeTypes.Add(type))
            {
                return false;
            }

            bool success = TryGetSignatureCore(type, out signature);
            _ = _activeTypes.Remove(type);
            return success;
        }

        /// <inheritdoc cref="TryGetSignature"/>
        private bool TryGetSignatureCore(ITypeSymbol type, [NotNullWhen(true)] out string? signature)
        {
            if (type.TypeKind == TypeKind.Dynamic)
            {
                signature = null;
                return false;
            }

            signature = type.SpecialType switch
            {
                SpecialType.System_Boolean => "b1",
                SpecialType.System_Byte => "u1",
                SpecialType.System_Char => "c2",
                SpecialType.System_Int16 => "i2",
                SpecialType.System_UInt16 => "u2",
                SpecialType.System_Int32 => "i4",
                SpecialType.System_UInt32 => "u4",
                SpecialType.System_Int64 => "i8",
                SpecialType.System_UInt64 => "u8",
                SpecialType.System_Single => "f4",
                SpecialType.System_Double => "f8",
                SpecialType.System_String => "string",
                SpecialType.System_Object => "cinterface(IInspectable)",
                _ => null
            };

            if (signature is not null)
            {
                return true;
            }

            if (type is IArrayTypeSymbol { IsSZArray: true, ElementType: not IArrayTypeSymbol } array &&
                TryGetSignature(array.ElementType, out string? elementSignature))
            {
                signature = $"pinterface({{61c17707-2d65-11e0-9ae8-d48564015472}};{elementSignature})";
                return true;
            }

            if (type is not INamedTypeSymbol { IsUnboundGenericType: false, IsStatic: false, IsNativeIntegerType: false } named ||
                named.TypeKind == TypeKind.Error ||
                named.IsTupleType ||
                !IsKnownDefinition(named.OriginalDefinition))
            {
                return false;
            }

            string name = GetMetadataName(named.OriginalDefinition);

            if (name == "System.Guid")
            {
                signature = "g16";
                return true;
            }

            if (TypeMapping.TryFindMappedTypeSignature(name, useWindowsUIXaml, out signature))
            {
                return true;
            }

            if (named.IsGenericType)
            {
                if ((named.ContainingAssembly.Identity.Name != "WinRT.Runtime" && !name.StartsWith("System.", StringComparison.Ordinal)) ||
                    !TryGetDefinitionIid(named.OriginalDefinition, name, out Guid definitionIid) ||
                    named.TypeArguments.Any(static argument => argument is IArrayTypeSymbol) ||
                    (name == "System.Nullable`1" && named.TypeArguments[0] is INamedTypeSymbol nullableArgument &&
                     GetMetadataName(nullableArgument.OriginalDefinition) == "System.Collections.Generic.KeyValuePair`2"))
                {
                    return false;
                }

                List<string> arguments = [];
                foreach (ITypeSymbol argument in named.TypeArguments)
                {
                    if (!TryGetSignature(argument, out string? argumentSignature))
                    {
                        return false;
                    }

                    arguments.Add(argumentSignature);
                }

                signature = $"pinterface({{{definitionIid}}};{string.Join(";", arguments)})";
                return true;
            }

            if (TryGetDefinitionIid(named, name, out Guid iid))
            {
                signature = named.TypeKind == TypeKind.Delegate ? $"delegate({{{iid}}})" : $"{{{iid}}}";
                return true;
            }

            // Class default interfaces may be exclusive (and therefore absent from reference projections)
            if (named.TypeKind == TypeKind.Class)
            {
                if (GetProjectionInfo(named.ContainingAssembly)
                    .DefaultInterfaces.TryGetValue(name, out string? defaultInterface))
                {
                    signature = $"rc({name};{defaultInterface})";
                    return true;
                }

                return false;
            }

            if (named.TypeKind == TypeKind.Enum)
            {
                signature = $"enum({name};{(named.EnumUnderlyingType?.SpecialType == SpecialType.System_UInt32 ? "u4" : "i4")})";
                return true;
            }

            if (named.TypeKind == TypeKind.Struct)
            {
                List<string> fields = [];
                foreach (IFieldSymbol field in named.GetMembers().OfType<IFieldSymbol>().Where(static field => !field.IsStatic))
                {
                    if (!TryGetSignature(field.Type, out string? fieldSignature))
                    {
                        return false;
                    }

                    fields.Add(fieldSignature);
                }

                if (fields.Count > 0)
                {
                    signature = $"struct({name};{string.Join(";", fields)})";
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks identity before using name-based WinRT mappings or trusting a GUID attribute.
        /// </summary>
        /// <param name="type">The type definition.</param>
        /// <returns>Whether this is a supported framework or projected definition.</returns>
        private bool IsKnownDefinition(INamedTypeSymbol type)
        {
            if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) ||
                type.ContainingType is not null ||
                type.GetAttributes().Any(static attribute =>
                    attribute.AttributeClass?.ToDisplayString() == "Windows.Foundation.Metadata.ApiContractAttribute"))
            {
                return false;
            }

            string name = GetMetadataName(type);
            bool isFramework = name.StartsWith("System.", StringComparison.Ordinal) &&
                type.ContainingAssembly.Identity.PublicKeyToken.AsSpan().SequenceEqual(
                    compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity.PublicKeyToken.AsSpan());

            isFramework |= name.StartsWith("System.", StringComparison.Ordinal) &&
                type.ContainingAssembly.Identity.Name.StartsWith("System.", StringComparison.Ordinal) &&
                type.ContainingAssembly.Identity.PublicKeyToken.AsSpan().SequenceEqual(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a });

            if (isFramework)
            {
                return name is not ("System.AttributeTargets" or "System.AttributeUsageAttribute") &&
                    (TypeMapping.TryFindMappedTypeName(name, useWindowsUIXaml, out _) ||
                     name is "System.Guid" or "System.EventHandler");
            }

            ProjectionInfo metadata = GetProjectionInfo(type.ContainingAssembly);
            bool isProjection = type.ContainingAssembly.Identity.Name == "WinRT.Runtime"
                ? type.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name is "ContractVersionAttribute" or "WindowsRuntimeTypeAttribute")
                : metadata.IsProjection && !metadata.IsComponent;

            return isProjection && !InheritsAttribute(type);
        }

        /// <summary>
        /// Gets an assembly's metadata without caching compiler symbols beyond this resolution.
        /// </summary>
        /// <param name="assembly">The input assembly.</param>
        /// <returns>The indexed projection metadata.</returns>
        private ProjectionInfo GetProjectionInfo(IAssemblySymbol assembly)
        {
            if (!_projectionMetadata.TryGetValue(assembly, out ProjectionInfo? info))
            {
                info = new(assembly);
                _projectionMetadata.Add(assembly, info);
            }

            return info;
        }

        /// <summary>
        /// Rejects metadata-only Windows Runtime attributes.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>Whether this is an attribute type.</returns>
        private static bool InheritsAttribute(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol? parent = type.BaseType; parent is not null; parent = parent.BaseType)
            {
                if (parent.ToDisplayString() == "System.Attribute")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves definition IIDs for mapped types or compiler-visible projected metadata.
        /// </summary>
        /// <param name="type">The validated definition.</param>
        /// <param name="name">Its metadata name.</param>
        /// <param name="iid">The IID.</param>
        /// <returns>Whether a definition IID is available.</returns>
        private bool TryGetDefinitionIid(INamedTypeSymbol type, string name, out Guid iid)
        {
            if (WellKnownWindowsRuntimeIids.TryGetIid(name, useWindowsUIXaml, out iid))
            {
                return true;
            }

            foreach (AttributeData attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == "System.Runtime.InteropServices.GuidAttribute" &&
                    attribute.ConstructorArguments is [{ Value: string text }])
                {
                    return Guid.TryParse(text, out iid);
                }
            }

            iid = default;
            return false;
        }
    }
}

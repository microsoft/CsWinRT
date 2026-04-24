// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;

#pragma warning disable IDE0046

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Extension methods for <see cref="TypeDefinition"/>.
/// </summary>
internal static class TypeDefinitionExtensions
{
    extension(TypeDefinition type)
    {
        /// <summary>
        /// Checks whether the type is a Windows Runtime type (has the <c>[WindowsRuntimeMetadata]</c> attribute).
        /// </summary>
        /// <remarks>
        /// Types marked with <c>[WindowsRuntimeMetadata]</c> are projected Windows Runtime types that come
        /// from CsWinRT-generated projection assemblies. This attribute indicates the type has a
        /// corresponding Windows Runtime definition and carries metadata about its contract assembly.
        /// </remarks>
        public bool IsWindowsRuntimeType => type.FindCustomAttributes("WindowsRuntime", "WindowsRuntimeMetadataAttribute").Any();

        /// <summary>
        /// Checks whether the type is a Windows Runtime API contract (has the <c>[ApiContract]</c> attribute).
        /// </summary>
        /// <remarks>
        /// In C#, API contracts are projected as enums with <c>[ApiContract]</c>, but in WinMD metadata
        /// they are represented as empty structs per the Windows Runtime type system spec.
        /// </remarks>
        public bool IsApiContract => type.FindCustomAttributes("Windows.Foundation.Metadata", "ApiContractAttribute").Any();

        /// <summary>
        /// Checks whether the type already has the <c>[Version]</c> attribute.
        /// </summary>
        public bool HasVersionAttribute => type.FindCustomAttributes("Windows.Foundation.Metadata", "VersionAttribute").Any();

        /// <summary>
        /// Checks whether the type already has the <c>[ContractVersion]</c> attribute.
        /// </summary>
        public bool HasContractVersionAttribute => type.FindCustomAttributes("Windows.Foundation.Metadata", "ContractVersionAttribute").Any();

        /// <summary>
        /// Gets the version number from the type's <c>[Version]</c> attribute, if present.
        /// </summary>
        /// <returns>
        /// The version number as an integer, or <see langword="null"/> if the type does not have a <c>[Version]</c> attribute.
        /// </returns>
        public int? VersionAttributeValue
        {
            get
            {
                if (type.FindCustomAttributes("Windows.Foundation.Metadata", "VersionAttribute").FirstOrDefault() is not CustomAttribute attribute)
                {
                    return null;
                }

                if (attribute.Signature is { FixedArguments: [{ Element: uint version }] })
                {
                    return (int)version;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the Windows Runtime contract assembly name from <c>[WindowsRuntimeMetadata]</c> attribute on the type, if present.
        /// </summary>
        /// <returns>
        /// The Windows Runtime contract assembly name (e.g. <c>"Microsoft.UI.Xaml"</c>), or <see langword="null"/>
        /// if the type does not have a <c>[WindowsRuntimeMetadata]</c> attribute.
        /// </returns>
        /// <remarks>
        /// For types from projection assemblies (e.g. <c>Microsoft.WinUI</c>), this returns the original
        /// Windows Runtime contract assembly name so the WinMD can reference types correctly.
        /// </remarks>
        public string? WindowsRuntimeAssemblyName
        {
            get
            {
                // If the type doesn't have the '[WindowsRuntimeMetadata]' attribute, stop here
                if (type.FindCustomAttributes("WindowsRuntime", "WindowsRuntimeMetadataAttribute").FirstOrDefault() is not CustomAttribute attribute)
                {
                    return null;
                }

                // Extract the assembly name from the attribute signature, if possible
                if (attribute.Signature is { FixedArguments: [{ Element: object assemblyName }] })
                {
                    return assemblyName.ToString();
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the effective namespace of the type. For nested types, this walks up the
        /// declaring type chain since nested types have no namespace of their own in metadata.
        /// </summary>
        /// <returns>
        /// The namespace of the type or its nearest declaring type with a namespace,
        /// or <see langword="null"/> if no namespace can be found.
        /// </returns>
        public string? EffectiveNamespace
        {
            get
            {
                if (type.Namespace is { Value.Length: > 0 })
                {
                    return type.Namespace.Value;
                }

                // For nested types, walk up to the declaring type to find the namespace
                TypeDefinition? current = type.DeclaringType;

                while (current is not null)
                {
                    if (current.Namespace is { Value.Length: > 0 })
                    {
                        return current.Namespace.Value;
                    }

                    current = current.DeclaringType;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the fully qualified name of the type, including generic arity.
        /// For nested types, uses the effective namespace from the declaring type chain.
        /// </summary>
        public string QualifiedName
        {
            get
            {
                string name = type.Name!.Value;
                string? @namespace = type.EffectiveNamespace;

                return @namespace is { Length: > 0 } ? $"{@namespace}.{name}" : name;
            }
        }
    }
}

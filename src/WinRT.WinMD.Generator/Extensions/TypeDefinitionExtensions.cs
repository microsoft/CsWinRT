// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;

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
        public bool IsWinRTType
        {
            get => type.CustomAttributes.Any(
                attribute => attribute.Constructor?.DeclaringType?.Name?.Value == "WindowsRuntimeMetadataAttribute");
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
        public string? WinRTAssemblyName
        {
            get
            {
                foreach (CustomAttribute attribute in type.CustomAttributes)
                {
                    if (attribute.Constructor?.DeclaringType?.Name?.Value == "WindowsRuntimeMetadataAttribute"
                        && attribute.Signature?.FixedArguments.Count > 0)
                    {
                        return attribute.Signature.FixedArguments[0].Element?.ToString();
                    }
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

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Extension methods for <see cref="ITypeDefOrRef"/>.
/// </summary>
internal static class TypeDefOrRefExtensions
{
    extension(ITypeDefOrRef type)
    {
        /// <summary>
        /// Gets the fully qualified name for the type. For nested <see cref="TypeDefinition"/>
        /// types, uses the effective namespace from the declaring type chain.
        /// </summary>
        public string QualifiedName
        {
            get
            {
                // For TypeDefinition, use the enhanced qualified name that handles nested types
                if (type is TypeDefinition td)
                {
                    return td.QualifiedName;
                }

                string name = type.Name!.Value;
                string? ns = type.Namespace?.Value;

                return ns is { Length: > 0 } ? $"{ns}.{name}" : name;
            }
        }
    }
}

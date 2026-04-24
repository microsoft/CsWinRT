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
        /// Gets the fully qualified name for the type.
        /// </summary>
        public string QualifiedName
        {
            get
            {
                string name = type.Name!.Value;
                string? @namespace = type.Namespace?.Value;

                return @namespace is { Length: > 0 } ? $"{@namespace}.{name}" : name;
            }
        }
    }
}

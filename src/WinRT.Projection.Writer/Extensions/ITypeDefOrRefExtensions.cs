// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="ITypeDefOrRef"/>.
/// </summary>
internal static class ITypeDefOrRefExtensions
{
    extension(ITypeDefOrRef type)
    {
        /// <summary>
        /// Returns the namespace and name of the type as a tuple, with both fields
        /// guaranteed to be non-<see langword="null"/>: a missing namespace becomes <see cref="string.Empty"/>
        /// and a missing name becomes <see cref="string.Empty"/>.
        /// </summary>
        /// <returns>A tuple of (namespace, name) with both fields non-<see langword="null"/>.</returns>
        public (string Namespace, string Name) Names()
        {
            return (type.Namespace?.Value ?? string.Empty, type.Name?.Value ?? string.Empty);
        }

        /// <summary>
        /// Attempts to resolve <paramref name="type"/> against <paramref name="context"/>, returning
        /// <see langword="null"/> when the type cannot be resolved (missing assembly, invalid reference,
        /// missing type, etc.). This is the safe alternative to <c>ITypeDescriptor.Resolve(RuntimeContext)</c>
        /// (which throws on failure) for best-effort cross-assembly resolution paths in the writer.
        /// </summary>
        /// <param name="context">The runtime context used to locate the type's assembly.</param>
        /// <returns>The resolved <see cref="TypeDefinition"/>, or <see langword="null"/> when the
        /// reference cannot be resolved.</returns>
        public TypeDefinition? TryResolve(RuntimeContext context)
        {
            return type.TryResolve(context, out TypeDefinition? definition) ? definition : null;
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="ITypeDefOrRef"/>.
/// </summary>
internal static class ITypeDefOrRefExtensions
{
    extension(ITypeDefOrRef type)
    {
        /// <summary>
        /// Returns the type's metadata name with any generic-arity backtick suffix stripped
        /// (e.g. <c>"IList`1"</c> becomes <c>"IList"</c>). When the type has no name, returns
        /// <see cref="string.Empty"/>.
        /// </summary>
        /// <returns>The type's stripped name.</returns>
        public string GetStrippedName()
        {
            return IdentifierEscaping.StripBackticks(type.GetRawName());
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

        /// <summary>
        /// Resolves <paramref name="type"/> to a <see cref="TypeDefinition"/>, handling the three
        /// shapes that can appear as an interface implementation's <see cref="InterfaceImplementation.Interface"/>:
        /// <list type="bullet">
        ///   <item>If it is already a <see cref="TypeDefinition"/>, returns it directly.</item>
        ///   <item>If it is a <see cref="TypeSpecification"/> whose signature is a generic instance,
        ///         recurses on the generic's open form (<see cref="GenericInstanceTypeSignature.GenericType"/>).</item>
        ///   <item>If it is a <see cref="TypeReference"/>, looks it up via
        ///         <see cref="MetadataCache.Find(string)"/> on its qualified name.</item>
        /// </list>
        /// Returns <see langword="null"/> when the type cannot be resolved.
        /// </summary>
        /// <param name="cache">The metadata cache used for cross-module type-reference resolution.</param>
        /// <returns>The resolved <see cref="TypeDefinition"/>, or <see langword="null"/> on failure.</returns>
        public TypeDefinition? ResolveAsTypeDefinition(MetadataCache cache)
        {
            if (type is TypeDefinition td)
            {
                return td;
            }

            if (type is TypeSpecification { Signature: GenericInstanceTypeSignature gi })
            {
                return gi.GenericType.ResolveAsTypeDefinition(cache);
            }

            if (type is TypeReference tr)
            {
                (string ns, string nm) = tr.Names();

                return cache.Find(ns, nm);
            }

            return null;
        }

        /// <summary>
        /// Attempts to extract the <see cref="GenericInstanceTypeSignature"/> from
        /// <paramref name="type"/> when it is a <see cref="TypeSpecification"/> whose signature
        /// is a generic instance. Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <param name="genericInstance">The extracted signature when this returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
        public bool TryGetGenericInstance([NotNullWhen(true)] out GenericInstanceTypeSignature? genericInstance)
        {
            genericInstance = (type as TypeSpecification)?.Signature as GenericInstanceTypeSignature;

            return genericInstance is not null;
        }
    }
}
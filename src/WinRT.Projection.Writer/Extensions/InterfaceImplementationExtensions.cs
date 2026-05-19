// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Metadata;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="InterfaceImplementation"/>.
/// </summary>
internal static class InterfaceImplementationExtensions
{
    extension(InterfaceImplementation impl)
    {
        /// <summary>
        /// Returns whether the implemented interface is the runtime class's <c>[Default]</c> interface
        /// (i.e. the one whose vtable backs the class's <c>IInspectable</c> identity).
        /// </summary>
        /// <returns><see langword="true"/> if the interface is the default interface; otherwise <see langword="false"/>.</returns>
        public bool IsDefaultInterface()
            => impl.HasWindowsFoundationMetadataAttribute(DefaultAttribute);

        /// <summary>
        /// Returns whether the implemented interface is marked <c>[Overridable]</c> (i.e. derived
        /// classes are allowed to override its members).
        /// </summary>
        /// <returns><see langword="true"/> if the interface is overridable; otherwise <see langword="false"/>.</returns>
        public bool IsOverridable()
            => impl.HasWindowsFoundationMetadataAttribute(OverridableAttribute);

        /// <summary>
        /// Attempts to resolve the implemented interface to a <see cref="TypeDefinition"/>, handling
        /// the common loop-body pattern of <c>if (impl.Interface is null) continue;</c> followed by
        /// <see cref="ITypeDefOrRefExtensions.ResolveAsTypeDefinition(ITypeDefOrRef, MetadataCache)"/>
        /// in a single call.
        /// </summary>
        /// <param name="cache">The metadata cache used for cross-module type-reference resolution.</param>
        /// <param name="definition">The resolved interface <see cref="TypeDefinition"/> when this returns
        /// <see langword="true"/>; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the interface reference resolved successfully; <see langword="false"/>
        /// when <see cref="InterfaceImplementation.Interface"/> is <see langword="null"/> or the reference
        /// could not be resolved.</returns>
        public bool TryResolveTypeDef(MetadataCache cache, [NotNullWhen(true)] out TypeDefinition? definition)
        {
            if (impl.Interface is null)
            {
                definition = null;
                return false;
            }

            definition = impl.Interface.ResolveAsTypeDefinition(cache);
            return definition is not null;
        }
    }
}
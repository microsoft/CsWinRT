// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="PropertyDefinition"/>.
/// </summary>
internal static class PropertyDefinitionExtensions
{
    extension(PropertyDefinition property)
    {
        /// <summary>
        /// Returns whether the property carries the <c>[NoExceptionAttribute]</c>.
        /// </summary>
        /// <returns><see langword="true"/> if the property is documented to never throw; otherwise <see langword="false"/>.</returns>
        public bool IsNoExcept()
            => property.HasAttribute(WindowsFoundationMetadata, NoExceptionAttribute);

        /// <summary>
        /// Returns the (getter, setter) accessor pair of the property.
        /// </summary>
        /// <returns>A tuple of (Getter, Setter) accessor methods, either of which may be <see langword="null"/>.</returns>
        public (MethodDefinition? Getter, MethodDefinition? Setter) GetPropertyMethods()
            => (property.GetMethod, property.SetMethod);
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

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
        public bool IsNoExcept => property.HasWindowsFoundationMetadataAttribute(NoExceptionAttribute);

        /// <summary>
        /// Returns the (getter, setter) accessor pair of the property.
        /// </summary>
        /// <returns>A tuple of (Getter, Setter) accessor methods, either of which may be <see langword="null"/>.</returns>
        public (MethodDefinition? Getter, MethodDefinition? Setter) GetMethods()
            => (property.GetMethod, property.SetMethod);
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="FieldDefinition"/>.
/// </summary>
internal static class FieldDefinitionExtensions
{
    extension(FieldDefinition field)
    {
        /// <summary>
        /// Returns the field's raw metadata name, falling back to <see cref="string.Empty"/> when
        /// the metadata name is <see langword="null"/>. Convenience for the
        /// <c>field.Name?.Value ?? string.Empty</c> pattern that appears at many sites.
        /// </summary>
        public string GetRawName()
        {
            return field.Name?.Value ?? string.Empty;
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="EventDefinition"/>.
/// </summary>
internal static class EventDefinitionExtensions
{
    extension(EventDefinition evt)
    {
        /// <summary>
        /// Returns the event's raw metadata name, falling back to <see cref="string.Empty"/> when
        /// the metadata name is <see langword="null"/>. Convenience for the
        /// <c>evt.Name?.Value ?? string.Empty</c> pattern that appears at many sites.
        /// </summary>
        public string GetRawName()
        {
            return evt.Name?.Value ?? string.Empty;
        }
    }
}

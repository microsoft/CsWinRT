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
        /// Returns the (add, remove) accessor pair of the event.
        /// </summary>
        /// <returns>A tuple of (Add, Remove) accessor methods, either of which may be <see langword="null"/>.</returns>
        public (MethodDefinition? Add, MethodDefinition? Remove) GetEventMethods()
            => (evt.AddMethod, evt.RemoveMethod);
    }
}
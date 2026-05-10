// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="EventDefinition"/>.
/// </summary>
internal static class EventDefinitionExtensions
{
    /// <summary>
    /// Returns the (add, remove) accessor pair of <paramref name="evt"/>.
    /// </summary>
    /// <param name="evt">The event definition.</param>
    /// <returns>A tuple of (Add, Remove) accessor methods, either of which may be <see langword="null"/>.</returns>
    public static (MethodDefinition? Add, MethodDefinition? Remove) GetEventMethods(this EventDefinition evt)
        => (evt.AddMethod, evt.RemoveMethod);
}
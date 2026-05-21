// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Static lookup for the (namespace, type-name) pairs that have an associated namespace-additions
/// resource. Used by the projected-struct emitter to decide whether the struct should be projected
/// as <c>partial</c> (so the addition file's content can extend it).
/// </summary>
internal static class AdditionTypes
{
    private static readonly FrozenDictionary<string, FrozenSet<string>> Table = new Dictionary<string, FrozenSet<string>>(StringComparer.Ordinal)
    {
        ["Microsoft.UI.Xaml"] = FrozenSet.Create(StringComparer.Ordinal, "Thickness"),
        ["Microsoft.UI.Xaml.Controls.Primitives"] = FrozenSet.Create(StringComparer.Ordinal, "GeneratorPosition"),
        ["Microsoft.UI.Xaml.Media"] = FrozenSet.Create(StringComparer.Ordinal, "Matrix"),
        ["Microsoft.UI.Xaml.Media.Animation"] = FrozenSet.Create(StringComparer.Ordinal, "KeyTime"),
        ["Windows.UI"] = FrozenSet.Create(StringComparer.Ordinal, "Color"),
        ["Windows.UI.Xaml"] = FrozenSet.Create(StringComparer.Ordinal, "Thickness"),
        ["Windows.UI.Xaml.Controls.Primitives"] = FrozenSet.Create(StringComparer.Ordinal, "GeneratorPosition"),
        ["Windows.UI.Xaml.Media"] = FrozenSet.Create(StringComparer.Ordinal, "Matrix"),
        ["Windows.UI.Xaml.Media.Animation"] = FrozenSet.Create(StringComparer.Ordinal, "KeyTime"),
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Returns whether the type identified by (<paramref name="typeNamespace"/>, <paramref name="typeName"/>)
    /// has an associated namespace-additions resource.
    /// </summary>
    /// <param name="typeNamespace">The type's namespace.</param>
    /// <param name="typeName">The type's short name (without generic arity).</param>
    /// <returns><see langword="true"/> if an addition exists; otherwise <see langword="false"/>.</returns>
    public static bool HasAdditionToType(string typeNamespace, string typeName)
    {
        return Table.TryGetValue(typeNamespace, out FrozenSet<string>? names) && names.Contains(typeName);
    }
}

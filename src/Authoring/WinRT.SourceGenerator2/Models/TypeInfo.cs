// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from ComputeSharp.
// See: https://github.com/Sergio0694/ComputeSharp/blob/main/src/ComputeSharp.SourceGeneration/Models/TypeInfo.cs.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// A model describing a type info in a type hierarchy.
/// </summary>
/// <param name="QualifiedName">The qualified name for the type.</param>
/// <param name="Kind">The type of the type in the hierarchy.</param>
/// <param name="IsRecord">Whether the type is a record type.</param>
internal sealed record TypeInfo(string QualifiedName, TypeKind Kind, bool IsRecord)
{
    /// <summary>
    /// Gets the keyword for the current type kind.
    /// </summary>
    /// <returns>The keyword for the current type kind.</returns>
    [SuppressMessage("Style", "IDE0072", Justification = "These are the only relevant cases for type hierarchies.")]
    public string GetTypeKeyword()
    {
        return Kind switch
        {
            TypeKind.Struct when IsRecord => "record struct",
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            TypeKind.Class when IsRecord => "record",
            _ => "class"
        };
    }
}
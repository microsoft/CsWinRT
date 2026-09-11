// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// A model representing a specific <c>ICustomProperty</c> to generate code for.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="FullyQualifiedTypeName">The fully qualified type name of the property.</param>
/// <param name="FullyQualifiedTypeNameForTypeOf">The fully qualified property type name without nullable reference annotations, for use in <c>typeof</c> expressions.</param>
/// <param name="FullyQualifiedIndexerTypeName">The fully qualified type name of the indexer parameter, if applicable.</param>
/// <param name="FullyQualifiedIndexerTypeNameForTypeOf">The fully qualified indexer parameter type name without nullable reference annotations, for use in <c>typeof</c> expressions, if applicable.</param>
/// <param name="CanRead">Whether the property can be read.</param>
/// <param name="CanWrite">Whether the property can be written to.</param>
/// <param name="IsStatic">Whether the property is static.</param>
internal sealed record CustomPropertyInfo(
    string Name,
    string FullyQualifiedTypeName,
    string FullyQualifiedTypeNameForTypeOf,
    string? FullyQualifiedIndexerTypeName,
    string? FullyQualifiedIndexerTypeNameForTypeOf,
    bool CanRead,
    bool CanWrite,
    bool IsStatic)
{
    /// <summary>
    /// Gets whether the current property is an indexer property.
    /// </summary>
    [MemberNotNullWhen(true, nameof(FullyQualifiedIndexerTypeName))]
    public bool IsIndexer => FullyQualifiedIndexerTypeName is not null;
}
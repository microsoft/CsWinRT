// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// A model representing a specific <c>ICustomProperty</c> to generate code for.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="FullyQualifiedTypeName">The fully qualified type name of the property.</param>
/// <param name="FullyQualifiedIndexerTypeName">The fully qualified type name of the indexer parameter, if applicable.</param>
/// <param name="CanRead">Whether the property can be read.</param>
/// <param name="CanWrite">Whether the property can be written to.</param>
/// <param name="IsStatic">Whether the property is static.</param>
internal sealed record CustomPropertyInfo(
    string Name,
    string FullyQualifiedTypeName,
    string? FullyQualifiedIndexerTypeName,
    bool CanRead,
    bool CanWrite,
    bool IsStatic);

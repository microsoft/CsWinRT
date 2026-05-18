// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="MetadataAttributeFactory.WriteWinRTMetadataAttribute(IndentedTextWriter, TypeDefinition, MetadataCache)"/>
/// </summary>
internal readonly struct WriteWinRTMetadataAttributeCallback(
    TypeDefinition type,
    MetadataCache cache) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MetadataAttributeFactory.WriteWinRTMetadataAttributeBody(writer, type, cache);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="MetadataAttributeFactory.WriteWinRTMetadataTypeNameAttribute(IndentedTextWriter, ProjectionEmitContext, TypeDefinition)"/>
/// </summary>
internal readonly struct WriteWinRTMetadataTypeNameAttributeCallback(
    ProjectionEmitContext context,
    TypeDefinition type) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MetadataAttributeFactory.WriteWinRTMetadataTypeNameAttributeBody(writer, context, type);
    }
}

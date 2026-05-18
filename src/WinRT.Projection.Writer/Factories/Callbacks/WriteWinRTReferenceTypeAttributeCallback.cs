// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="MetadataAttributeFactory.WriteWinRTReferenceTypeAttribute(IndentedTextWriter, ProjectionEmitContext, TypeDefinition)"/>
/// </summary>
internal readonly struct WriteWinRTReferenceTypeAttributeCallback(
    ProjectionEmitContext context,
    TypeDefinition type) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MetadataAttributeFactory.WriteWinRTReferenceTypeAttributeBody(writer, context, type);
    }
}

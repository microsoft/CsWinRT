// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="IidExpressionGenerator.WriteIidReferenceGuidPropertyName(ProjectionEmitContext, TypeDefinition)"/>
internal readonly struct WriteIidReferenceGuidPropertyNameCallback(
    ProjectionEmitContext context,
    TypeDefinition type) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        IidExpressionGenerator.WriteIidReferenceGuidPropertyName(writer, context, type);
    }
}

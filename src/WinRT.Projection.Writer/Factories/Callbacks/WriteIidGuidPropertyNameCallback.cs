// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="IidExpressionGenerator.WriteIidGuidPropertyName(ProjectionEmitContext, TypeDefinition)"/>
internal readonly struct WriteIidGuidPropertyNameCallback(
    ProjectionEmitContext context,
    TypeDefinition type) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        IidExpressionGenerator.WriteIidGuidPropertyName(writer, context, type);
    }
}

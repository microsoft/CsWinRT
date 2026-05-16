// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="AbiTypeWriter.WriteAbiType(ProjectionEmitContext, TypeSemantics)"/>
internal readonly struct WriteAbiTypeCallback(
    ProjectionEmitContext context,
    TypeSemantics semantics) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        AbiTypeWriter.WriteAbiType(writer, context, semantics);
    }
}

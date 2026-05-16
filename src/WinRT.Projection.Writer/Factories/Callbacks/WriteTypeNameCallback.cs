// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="TypedefNameWriter.WriteTypeName(ProjectionEmitContext, TypeSemantics, TypedefNameType, bool)"/>
internal readonly struct WriteTypeNameCallback(
    ProjectionEmitContext context,
    TypeSemantics semantics,
    TypedefNameType nameType,
    bool forceWriteNamespace) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        TypedefNameWriter.WriteTypeName(writer, context, semantics, nameType, forceWriteNamespace);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="TypedefNameWriter.WriteTypedefName(ProjectionEmitContext, TypeDefinition, TypedefNameType, bool)"/>
internal readonly struct WriteTypedefNameCallback(
    ProjectionEmitContext context,
    TypeDefinition type,
    TypedefNameType nameType,
    bool forceWriteNamespace) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        TypedefNameWriter.WriteTypedefName(writer, context, type, nameType, forceWriteNamespace);
    }
}


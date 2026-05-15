// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="AbiTypeHelpers.WriteIidGuidReference(ProjectionEmitContext, TypeDefinition)"/>
internal readonly struct WriteIidGuidReferenceCallback(ProjectionEmitContext context, TypeDefinition type) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        AbiTypeHelpers.WriteIidGuidReference(writer, context, type);
    }
}

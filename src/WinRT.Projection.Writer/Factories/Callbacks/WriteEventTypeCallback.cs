// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="TypedefNameWriter.WriteEventType(ProjectionEmitContext, EventDefinition, GenericInstanceTypeSignature?)"/>
internal readonly struct WriteEventTypeCallback(
    ProjectionEmitContext context,
    EventDefinition evt,
    GenericInstanceTypeSignature? currentInstance) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        TypedefNameWriter.WriteEventType(writer, context, evt, currentInstance);
    }
}

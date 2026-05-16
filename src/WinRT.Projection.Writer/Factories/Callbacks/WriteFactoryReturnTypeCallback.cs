// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="ComponentFactory.WriteFactoryReturnType(IndentedTextWriter, ProjectionEmitContext, MethodDefinition)"/>
/// </summary>
internal readonly struct WriteFactoryReturnTypeCallback(
    ProjectionEmitContext context,
    MethodDefinition method) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        ComponentFactory.WriteFactoryReturnType(writer, context, method);
    }
}

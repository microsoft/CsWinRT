// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="ComponentFactory.WriteFactoryMethodParameters(IndentedTextWriter, ProjectionEmitContext, MethodDefinition, bool)"/>
/// </summary>
internal readonly struct WriteFactoryMethodParametersCallback(
    ProjectionEmitContext context,
    MethodDefinition method,
    bool includeTypes) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        ComponentFactory.WriteFactoryMethodParameters(writer, context, method, includeTypes);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="MethodFactory.WriteProjectionParameter(IndentedTextWriter, ProjectionEmitContext, ParameterInfo)"/>
/// </summary>
internal readonly struct WriteProjectionParameterCallback(
    ProjectionEmitContext context,
    ParameterInfo parameter) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MethodFactory.WriteProjectionParameter(writer, context, parameter);
    }
}

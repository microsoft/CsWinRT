// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="MethodFactory.WriteProjectionParameterType(IndentedTextWriter, ProjectionEmitContext, ParameterInfo)"/>
/// </summary>
internal readonly struct WriteProjectionParameterTypeCallback(
    ProjectionEmitContext context,
    ParameterInfo parameter) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MethodFactory.WriteProjectionParameterType(writer, context, parameter);
    }
}

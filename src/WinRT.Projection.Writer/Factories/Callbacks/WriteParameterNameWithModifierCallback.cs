// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="ClassMembersFactory.WriteParameterNameWithModifier(IndentedTextWriter, ProjectionEmitContext, ParameterInfo)"/>
/// </summary>
internal readonly struct WriteParameterNameWithModifierCallback(
    ProjectionEmitContext context,
    ParameterInfo parameter) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        ClassMembersFactory.WriteParameterNameWithModifier(writer, context, parameter);
    }
}

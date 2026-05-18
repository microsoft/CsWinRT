// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="MethodFactory.WriteCallArguments(IndentedTextWriter, ProjectionEmitContext, MethodSignatureInfo, bool)"/>
/// </summary>
internal readonly struct WriteCallArgumentsCallback(
    ProjectionEmitContext context,
    MethodSignatureInfo sig,
    bool leadingComma) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MethodFactory.WriteCallArguments(writer, context, sig, leadingComma);
    }
}

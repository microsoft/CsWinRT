// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="MethodFactory.WriteProjectionReturnType(ProjectionEmitContext, MethodSignatureInfo)"/>
internal readonly struct WriteProjectionReturnTypeCallback(
    ProjectionEmitContext context,
    MethodSignatureInfo sig) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MethodFactory.WriteProjectionReturnType(writer, context, sig);
    }
}

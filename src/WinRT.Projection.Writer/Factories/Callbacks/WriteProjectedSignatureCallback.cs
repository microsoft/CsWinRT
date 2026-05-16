// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="MethodFactory.WriteProjectedSignature(ProjectionEmitContext, TypeSignature, bool)"/>
internal readonly struct WriteProjectedSignatureCallback(
    ProjectionEmitContext context,
    TypeSignature typeSig,
    bool isParameter) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        MethodFactory.WriteProjectedSignature(writer, context, typeSig, isParameter);
    }
}

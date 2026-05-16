// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="ObjRefNameGenerator.WriteIidExpression(ProjectionEmitContext, ITypeDefOrRef)"/>
internal readonly struct WriteIidExpressionCallback(
    ProjectionEmitContext context,
    ITypeDefOrRef ifaceType) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        ObjRefNameGenerator.WriteIidExpression(writer, context, ifaceType);
    }
}

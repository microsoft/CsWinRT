// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="ObjRefNameGenerator.WriteIidReferenceExpression(TypeDefinition)"/>
internal readonly struct WriteIidReferenceExpressionCallback(TypeDefinition type) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        ObjRefNameGenerator.WriteIidReferenceExpression(writer, type);
    }
}

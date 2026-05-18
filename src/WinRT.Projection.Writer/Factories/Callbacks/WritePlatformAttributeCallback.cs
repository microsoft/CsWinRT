// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="CustomAttributeFactory.WritePlatformAttribute(IndentedTextWriter, ProjectionEmitContext, IHasCustomAttribute)"/>
/// </summary>
internal readonly struct WritePlatformAttributeCallback(
    ProjectionEmitContext context,
    IHasCustomAttribute member) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        CustomAttributeFactory.WritePlatformAttributeBody(writer, context, member);
    }
}

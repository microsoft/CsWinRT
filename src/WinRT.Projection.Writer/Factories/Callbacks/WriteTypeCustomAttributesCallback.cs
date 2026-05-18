// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="CustomAttributeFactory.WriteTypeCustomAttributes(IndentedTextWriter, ProjectionEmitContext, TypeDefinition, bool)"/>
/// </summary>
internal readonly struct WriteTypeCustomAttributesCallback(
    ProjectionEmitContext context,
    TypeDefinition type,
    bool enablePlatformAttrib) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        CustomAttributeFactory.WriteTypeCustomAttributesBody(writer, context, type, enablePlatformAttrib);
    }
}

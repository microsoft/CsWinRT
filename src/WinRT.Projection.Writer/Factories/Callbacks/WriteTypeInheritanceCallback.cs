// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="InterfaceFactory.WriteTypeInheritance(ProjectionEmitContext, TypeDefinition, bool, bool)"/>
internal readonly struct WriteTypeInheritanceCallback(
    ProjectionEmitContext context,
    TypeDefinition type,
    bool includeExclusiveInterface,
    bool includeWindowsRuntimeObject) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        InterfaceFactory.WriteTypeInheritance(writer, context, type, includeExclusiveInterface, includeWindowsRuntimeObject);
    }
}

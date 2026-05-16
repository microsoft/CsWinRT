// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="ClassMembersFactory.WriteInterfaceTypeNameForCcw(ProjectionEmitContext, ITypeDefOrRef)"/>
internal readonly struct WriteInterfaceTypeNameForCcwCallback(
    ProjectionEmitContext context,
    ITypeDefOrRef ifaceType) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        ClassMembersFactory.WriteInterfaceTypeNameForCcw(writer, context, ifaceType);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="InterfaceFactory.WriteGuidAttribute(IndentedTextWriter, TypeDefinition)"/>
/// </summary>
internal readonly struct WriteGuidAttributeCallback(TypeDefinition type) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        InterfaceFactory.WriteGuidAttribute(writer, type);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="AbiInterfaceFactory.WriteAbiParameterTypesPointer(IndentedTextWriter, ProjectionEmitContext, MethodSignatureInfo, bool)"/>
/// </summary>
internal readonly struct WriteAbiParameterTypesPointerCallback(
    ProjectionEmitContext context,
    MethodSignatureInfo sig,
    bool includeParamNames) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        AbiInterfaceFactory.WriteAbiParameterTypesPointer(writer, context, sig, includeParamNames);
    }
}

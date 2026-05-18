// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="AbiMethodBodyFactory.EmitMarshallerConvertToUnmanaged(IndentedTextWriter, ProjectionEmitContext, TypeSignature, string)"/>
/// </summary>
internal readonly struct EmitMarshallerConvertToUnmanagedCallback(
    ProjectionEmitContext context,
    TypeSignature sig,
    string argName) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        AbiMethodBodyFactory.EmitMarshallerConvertToUnmanaged(writer, context, sig, argName);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <summary>
/// <see cref="AbiMethodBodyFactory.EmitMarshallerConvertToManaged(IndentedTextWriter, ProjectionEmitContext, TypeSignature, string)"/>
/// </summary>
internal readonly struct EmitMarshallerConvertToManagedCallback(
    ProjectionEmitContext context,
    TypeSignature sig,
    string argName) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        AbiMethodBodyFactory.EmitMarshallerConvertToManaged(writer, context, sig, argName);
    }
}

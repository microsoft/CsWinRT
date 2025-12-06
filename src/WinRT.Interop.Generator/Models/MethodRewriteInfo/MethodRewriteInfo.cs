// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains generic info for a target method for two-pass IL generation.
/// </summary>
internal abstract class MethodRewriteInfo
{
    /// <summary>
    /// The type of the value that needs to be marshalled.
    /// </summary>
    public required TypeSignature Type { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='method']/node()"/></summary>
    public required MethodDefinition Method { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='marker']/node()"/></summary>
    public required CilInstruction Marker { get; init; }
}

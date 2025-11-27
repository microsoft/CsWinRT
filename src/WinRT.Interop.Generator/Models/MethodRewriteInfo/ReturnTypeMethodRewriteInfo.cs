// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains info for a target method for two-pass IL generation.
/// </summary>
/// <see cref="Factories.InteropMethodRewriteFactory.Return"/>
internal sealed class ReturnTypeMethodRewriteInfo
{
    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.Return" path="/param[@name='returnType']/node()"/></summary>
    public required TypeSignature ReturnType { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.Return" path="/param[@name='method']/node()"/></summary>
    public required MethodDefinition Method { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.Return" path="/param[@name='marker']/node()"/></summary>
    public required CilInstruction Marker { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.Return" path="/param[@name='source']/node()"/></summary>
    public required CilLocalVariable Source { get; init; }
}

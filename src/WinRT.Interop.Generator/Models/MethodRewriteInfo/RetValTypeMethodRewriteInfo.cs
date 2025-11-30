// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains info for a target method for two-pass IL generation, for an unmanaged <c>[retval]</c> value.
/// </summary>
/// <see cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod"/>
internal sealed class RetValTypeMethodRewriteInfo
{
    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.RetVal.RewriteMethod" path="/param[@name='retValType']/node()"/></summary>
    public required TypeSignature ReturnType { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.RetVal.RewriteMethod" path="/param[@name='method']/node()"/></summary>
    public required MethodDefinition Method { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.RetVal.RewriteMethod" path="/param[@name='marker']/node()"/></summary>
    public required CilInstruction Marker { get; init; }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Code.Cil;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains info for a target method for two-pass IL generation, for a managed return value.
/// </summary>
/// <see cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod"/>
internal sealed class ReturnTypeMethodRewriteInfo : MethodRewriteInfo
{
    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='source']/node()"/></summary>
    public required CilLocalVariable Source { get; init; }
}

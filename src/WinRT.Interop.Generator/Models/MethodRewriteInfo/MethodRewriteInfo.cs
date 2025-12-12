// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Helpers;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains generic info for a target method for two-pass IL generation.
/// </summary>
internal abstract class MethodRewriteInfo : IComparable<MethodRewriteInfo>
{
    /// <summary>
    /// The type of the value that needs to be marshalled.
    /// </summary>
    public required TypeSignature Type { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='method']/node()"/></summary>
    public required MethodDefinition Method { get; init; }

    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='marker']/node()"/></summary>
    public required CilInstruction Marker { get; init; }

    /// <inheritdoc/>
    public virtual int CompareTo(MethodRewriteInfo? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        int result = TypeDescriptorComparer.Instance.Compare(Type, other.Type);

        // Match by marshalling type first
        if (result != 0)
        {
            return result;
        }

        result = MemberDefinitionComparer.Instance.Compare(Method, other.Method);

        // Match by target method next
        if (result != 0)
        {
            return result;
        }

        int leftIndex = Method.CilMethodBody?.Instructions.IndexOf(Marker) ?? -1;
        int rightIndex = other.Method.CilMethodBody?.Instructions.IndexOf(other.Marker) ?? -1;

        // Lastly, compare by order of instructions within the target method.
        // There's no concern about stable sorting with respect to objects
        // where the instructions are missing, as 'cswinrtgen' will fail.
        return leftIndex.CompareTo(rightIndex);
    }
}

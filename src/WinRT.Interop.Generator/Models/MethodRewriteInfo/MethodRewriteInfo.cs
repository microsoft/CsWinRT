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
internal abstract partial class MethodRewriteInfo : IComparable<MethodRewriteInfo>
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
    public abstract int CompareTo(MethodRewriteInfo? other);

    /// <summary>
    /// Compares the current instance just based on the state from <see cref="MethodRewriteInfo"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="MethodRewriteInfo"/> type currently in use.</typeparam>
    /// <param name="other">The input <see cref="MethodRewriteInfo"/> instance.</param>
    /// <returns>The comparison result.</returns>
    protected int CompareByMethodRewriteInfo<T>(MethodRewriteInfo? other)
        where T : MethodRewriteInfo
    {
        if (other is null)
        {
            return 1;
        }

        // If the input object is of a different type, just sort alphabetically based on the type name
        if (other is not T)
        {
            return typeof(T).FullName!.CompareTo(other.GetType().FullName!);
        }

        int result = MemberDefinitionComparer.Instance.Compare(Method, other.Method);

        // First, sort by target method
        if (result != 0)
        {
            return result;
        }

        // Next, compare by order of instructions within the target method.
        // There's no concern about stable sorting with respect to objects
        // where the instructions are missing, as 'cswinrtgen' will fail.
        int leftIndex = Method.CilMethodBody?.Instructions.ReferenceIndexOf(Marker) ?? -1;
        int rightIndex = other.Method.CilMethodBody?.Instructions.ReferenceIndexOf(other.Marker) ?? -1;

        result = leftIndex.CompareTo(rightIndex);

        if (result != 0)
        {
            return result;
        }

        // Lastly, compare by target type (this shouldn't be reached for valid objects)
        return TypeDescriptorComparer.Instance.Compare(Type, other.Type);
    }
}

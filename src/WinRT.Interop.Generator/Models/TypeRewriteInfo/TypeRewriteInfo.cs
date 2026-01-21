// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Helpers;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains generic info for a target type for two-pass IL generation.
/// </summary>
internal abstract partial class TypeRewriteInfo : IComparable<TypeRewriteInfo>
{
    /// <summary>
    /// The mapped type for <see cref="GeneratedType"/>.
    /// </summary>
    public required TypeSignature MappedType { get; init; }

    /// <summary>
    /// The type of the value that needs to be marshalled.
    /// </summary>
    public required TypeDefinition GeneratedType { get; init; }

    /// <inheritdoc/>
    public abstract int CompareTo(TypeRewriteInfo? other);

    /// <summary>
    /// Compares the current instance just based on the state from <see cref="TypeRewriteInfo"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="TypeRewriteInfo"/> type currently in use.</typeparam>
    /// <param name="other">The input <see cref="TypeRewriteInfo"/> instance.</param>
    /// <returns>The comparison result.</returns>
    protected int CompareByTypeRewriteInfo<T>(TypeRewriteInfo? other)
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

        int result = TypeDescriptorComparer.Instance.Compare(MappedType, other.MappedType);

        // First, sort by mapped type
        if (result != 0)
        {
            return result;
        }

        // Otherwise, compare by generated type (this shouldn't be reached for valid objects)
        return TypeDescriptorComparer.Instance.Compare(GeneratedType, other.GeneratedType);
    }
}

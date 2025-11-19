// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions to create <see cref="TypeSignature"/> instances without resolving types.
/// </summary>
internal static class MakeTypeSignatureExtensions
{
    /// <inheritdoc cref="TypeDefinition.ToTypeSignature(bool)"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a value type.</remarks>
    public static TypeSignature ToValueTypeSignature(this TypeDefinition typeDefinition)
    {
        return typeDefinition.ToTypeSignature(isValueType: true);
    }

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a value type.</remarks>
    public static TypeSignature ToValueTypeSignature(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: true);
    }

    /// <inheritdoc cref="TypeDescriptorExtensions.MakeGenericInstanceType(ITypeDescriptor, bool, TypeSignature[])"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a value type.</remarks>
    public static GenericInstanceTypeSignature MakeGenericValueType(this ITypeDescriptor typeDescriptor, params TypeSignature[] typeArguments)
    {
        return typeDescriptor.MakeGenericInstanceType(isValueType: true, typeArguments);
    }

    /// <inheritdoc cref="TypeDefinition.ToTypeSignature(bool)"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a reference type.</remarks>
    public static TypeSignature ToReferenceTypeSignature(this TypeDefinition typeDefinition)
    {
        return typeDefinition.ToTypeSignature(isValueType: false);
    }

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a reference type.</remarks>
    public static TypeSignature ToReferenceTypeSignature(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: false);
    }

    /// <inheritdoc cref="TypeDescriptorExtensions.MakeGenericInstanceType(ITypeDescriptor, bool, TypeSignature[])"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a reference type.</remarks>
    public static GenericInstanceTypeSignature MakeGenericReferenceType(this ITypeDescriptor typeDescriptor, params TypeSignature[] typeArguments)
    {
        return typeDescriptor.MakeGenericInstanceType(isValueType: false, typeArguments);
    }
}
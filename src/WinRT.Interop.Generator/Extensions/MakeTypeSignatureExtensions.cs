// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
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

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="PointerTypeSignature"/> for a value type.</remarks>
    public static PointerTypeSignature MakeValueTypePointerType(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: true).MakePointerType();
    }

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="PointerTypeSignature"/> for a reference type.</remarks>
    public static PointerTypeSignature MakeReferenceTypePointerType(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: false).MakePointerType();
    }

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="ByReferenceTypeSignature"/> for a value type.</remarks>
    public static ByReferenceTypeSignature MakeValueTypeByReferenceType(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: true).MakeByReferenceType();
    }

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="ByReferenceTypeSignature"/> for a reference type.</remarks>
    public static ByReferenceTypeSignature MakeReferenceTypeByReferenceType(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: false).MakeByReferenceType();
    }

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="SzArrayTypeSignature"/> for a value type.</remarks>
    public static SzArrayTypeSignature MakeValueTypeSzArrayType(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: true).MakeSzArrayType();
    }

    /// <inheritdoc cref="ITypeDefOrRef.ToTypeSignature"/>
    /// <remarks>This method always returns a <see cref="SzArrayTypeSignature"/> for a reference type.</remarks>
    public static SzArrayTypeSignature MakeReferenceTypeSzArrayType(this ITypeDefOrRef typeDefOrRef)
    {
        return typeDefOrRef.ToTypeSignature(isValueType: false).MakeSzArrayType();
    }

    /// <inheritdoc cref="TypeDescriptorExtensions.MakeGenericInstanceType(ITypeDescriptor, bool, IEnumerable{TypeSignature})"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a value type.</remarks>
    public static GenericInstanceTypeSignature MakeGenericValueType(this ITypeDescriptor typeDescriptor, IEnumerable<TypeSignature> typeArguments)
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

    /// <inheritdoc cref="TypeDescriptorExtensions.MakeGenericInstanceType(ITypeDescriptor, bool, IEnumerable{TypeSignature})"/>
    /// <remarks>This method always returns a <see cref="TypeSignature"/> for a reference type.</remarks>
    public static GenericInstanceTypeSignature MakeGenericReferenceType(this ITypeDescriptor typeDescriptor, IEnumerable<TypeSignature> typeArguments)
    {
        return typeDescriptor.MakeGenericInstanceType(isValueType: false, typeArguments);
    }
}
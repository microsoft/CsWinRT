// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known type signatures.
/// </summary>
internal static class WellKnownTypeSignatureFactory
{
    /// <summary>
    /// Creates a type signature for the <c>QueryInterface</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static FunctionPointerTypeSignature QueryInterfaceImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT>'
        return new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                referenceImporter.ImportType(typeof(Guid)).MakePointerType(),
                corLibTypeFactory.Void.MakePointerType().MakePointerType()]).MakeFunctionPointerType();
    }

    /// <summary>
    /// Creates a type signature for the <c>AddRef</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static FunctionPointerTypeSignature AddRefImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.UInt32),
            parameterTypes: [corLibTypeFactory.Void.MakePointerType()]).MakeFunctionPointerType();
    }

    /// <summary>
    /// Creates a type signature for the <c>Release</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static FunctionPointerTypeSignature ReleaseImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.UInt32),
            parameterTypes: [corLibTypeFactory.Void.MakePointerType()]).MakeFunctionPointerType();
    }
}

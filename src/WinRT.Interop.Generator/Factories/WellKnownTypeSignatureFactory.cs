// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    public static MethodSignature QueryInterfaceImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                referenceImporter.ImportType(typeof(Guid)).MakePointerType(),
                corLibTypeFactory.Void.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>AddRef</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature AddRefImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.UInt32),
            parameterTypes: [corLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Release</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature ReleaseImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.UInt32),
            parameterTypes: [corLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetIids</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetIidsImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.UInt32.MakePointerType(),
                referenceImporter.ImportType(typeof(Guid)).MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetRuntimeClassName</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetRuntimeClassNameImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Void.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetTrustLevel</c> vtable entry.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetTrustLevelImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, TrustLevel*, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Int32.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Invoke</c> vtable entry for a delegate, taking objects for both parameters.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature InvokeImpl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void*, void*, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for <c>in Guid</c> values.
    /// </summary>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="CustomModifierTypeSignature"/> instance.</returns>
    public static CustomModifierTypeSignature InGuid(ReferenceImporter referenceImporter)
    {
        // Signature for 'Guid& modreq(InAttribute)'
        return referenceImporter
            .ImportType(typeof(Guid))
            .MakeByReferenceType()
            .MakeModifierType(referenceImporter.ImportType(typeof(InAttribute)), isRequired: true);
    }
}

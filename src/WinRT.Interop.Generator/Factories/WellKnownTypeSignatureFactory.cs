// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known type signatures.
/// </summary>
internal static class WellKnownTypeSignatureFactory
{
    /// <summary>
    /// Creates a type signature for the <c>QueryInterface</c> vtable entry.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature QueryInterfaceImpl(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: wellKnownInteropReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: wellKnownInteropReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType(),
                wellKnownInteropReferences.Guid.MakePointerType(),
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>AddRef</c> vtable entry.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature AddRefImpl(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: wellKnownInteropReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: wellKnownInteropReferences.CorLibTypeFactory.UInt32),
            parameterTypes: [wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Release</c> vtable entry.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature ReleaseImpl(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: wellKnownInteropReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: wellKnownInteropReferences.CorLibTypeFactory.UInt32),
            parameterTypes: [wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetIids</c> vtable entry.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetIidsImpl(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: wellKnownInteropReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: wellKnownInteropReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType(),
                wellKnownInteropReferences.CorLibTypeFactory.UInt32.MakePointerType(),
                wellKnownInteropReferences.Guid.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetRuntimeClassName</c> vtable entry.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetRuntimeClassNameImpl(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: wellKnownInteropReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: wellKnownInteropReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType(),
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetTrustLevel</c> vtable entry.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetTrustLevelImpl(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, TrustLevel*, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: wellKnownInteropReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: wellKnownInteropReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType(),
                wellKnownInteropReferences.CorLibTypeFactory.Int32.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Invoke</c> vtable entry for a delegate, taking objects for both parameters.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature InvokeImpl(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void*, void*, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: wellKnownInteropReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: wellKnownInteropReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType(),
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType(),
                wellKnownInteropReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for <c>in Guid</c> values.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="CustomModifierTypeSignature"/> instance.</returns>
    public static CustomModifierTypeSignature InGuid(WellKnownInteropReferences wellKnownInteropReferences)
    {
        // Signature for 'Guid& modreq(InAttribute)'
        return
            wellKnownInteropReferences.Guid
            .MakeByReferenceType()
            .MakeModifierType(wellKnownInteropReferences.InAttribute, isRequired: true);
    }
}

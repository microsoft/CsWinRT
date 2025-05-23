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
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature QueryInterfaceImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.Guid.MakePointerType(),
                interopReferences.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>AddRef</c> vtable entry.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature AddRefImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.UInt32),
            parameterTypes: [interopReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Release</c> vtable entry.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature ReleaseImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.UInt32),
            parameterTypes: [interopReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetIids</c> vtable entry.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetIidsImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32.MakePointerType(),
                interopReferences.Guid.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetRuntimeClassName</c> vtable entry.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetRuntimeClassNameImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetTrustLevel</c> vtable entry.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature GetTrustLevelImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, TrustLevel*, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.Int32.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Invoke</c> vtable entry for a delegate, taking objects for both parameters.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature InvokeImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void*, void*, int>'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Current</c> vtable entry for an enumerator.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IEnumerator1CurrentImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_Current'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>HasCurrent</c> vtable entry for an enumerator.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IEnumerator1HasCurrentImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_HasCurrent'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.Boolean.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetMany</c> vtable entry for an enumerator.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IEnumerator1GetManyImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, void*, uint*, HRESULT> GetMany'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32,
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for <c>in Guid</c> values.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="CustomModifierTypeSignature"/> instance.</returns>
    public static CustomModifierTypeSignature InGuid(InteropReferences interopReferences)
    {
        // Signature for 'Guid& modreq(InAttribute)'
        return
            interopReferences.Guid
            .MakeByReferenceType()
            .MakeModifierType(interopReferences.InAttribute, isRequired: true);
    }
}

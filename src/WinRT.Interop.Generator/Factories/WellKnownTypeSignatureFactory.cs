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
    /// Creates a type signature for the <c>First</c> vtable entry for an enumerable.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IEnumerable1FirstImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> First'
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
    /// Creates a type signature for the <c>GetAt</c> vtable entry for a vector view.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector view.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IReadOnlyList1GetAtImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, <ELEMENT_TYPE>*, HRESULT> GetAt'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32,
                elementType.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>get_Size</c> vtable entry for a vector view.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IReadOnlyList1get_SizeImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>IndexOf</c> vtable entry for a vector view.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector view.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IReadOnlyList1IndexOfImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, <ELEMENT_TYPE>, uint*, HRESULT> IndexOf'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                elementType,
                interopReferences.CorLibTypeFactory.UInt32.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetMany</c> vtable entry for a vector view.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector view.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IReadOnlyList1GetManyImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, uint, <ELEMENT_TYPE>*, uint*, HRESULT> GetMany'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32,
                interopReferences.CorLibTypeFactory.UInt32,
                elementType.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetAt</c> vtable entry for a vector.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1GetAtImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // The signature is identical to 'IVectorView<T>.GetAt'
        return IReadOnlyList1GetAtImpl(elementType, interopReferences);
    }

    /// <summary>
    /// Creates a type signature for the <c>get_Size</c> vtable entry for a vector.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1get_SizeImpl(InteropReferences interopReferences)
    {
        // The signature is identical to 'IVectorView<T>.get_Size'
        return IReadOnlyList1get_SizeImpl(interopReferences);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetView</c> vtable entry for a vector.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1GetViewImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetView'
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
    /// Creates a type signature for the <c>IndexOf</c> vtable entry for a vector.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1IndexOfImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, <ELEMENT_TYPE>, uint*, bool*, HRESULT> IndexOf'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                elementType,
                interopReferences.CorLibTypeFactory.UInt32.MakePointerType(),
                interopReferences.CorLibTypeFactory.Boolean.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>SetAt</c> vtable entry for a vector.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1SetAtImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, <ELEMENT_TYPE>, HRESULT> SetAt'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32,
                elementType]);
    }

    /// <summary>
    /// Creates a type signature for the <c>InsertAt</c> vtable entry for a vector.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1InsertAtImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, <ELEMENT_TYPE>, HRESULT> InsertAt'.
        // This is identical to the signature of 'SetAt', so we can just reuse that method here as well.
        return IList1SetAtImpl(elementType, interopReferences);
    }

    /// <summary>
    /// Creates a type signature for the <c>RemoveAt</c> vtable entry for a vector.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1RemoveAtImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, HRESULT> RemoveAt'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Append</c> vtable entry for a vector.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1AppendImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, <ELEMENT_TYPE>, HRESULT> Append'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                elementType]);
    }

    /// <summary>
    /// Creates a type signature for the <c>RemoveAtEnd</c> vtable entry for a vector.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1RemoveAtEndImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, HRESULT> RemoveAtEnd'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [interopReferences.CorLibTypeFactory.Void.MakePointerType()]);
    }

    /// <summary>
    /// Creates a type signature for the <c>Clear</c> vtable entry for a vector.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1ClearImpl(InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, HRESULT> Clear'.
        // This is identical to the signature of 'RemoveAtEnd', so we can reuse that.
        return IList1RemoveAtEndImpl(interopReferences);
    }

    /// <summary>
    /// Creates a type signature for the <c>GetMany</c> vtable entry for a vector.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1GetManyImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, int, <ELEMENT_TYPE>*, uint*, HRESULT> GetMany'.
        // This is the same as 'IVectorView<T>.GetMany', so we can reuse that one here (like the methods above).
        return IReadOnlyList1GetManyImpl(elementType, interopReferences);
    }

    /// <summary>
    /// Creates a type signature for the <c>ReplaceAll</c> vtable entry for a vector.
    /// </summary>
    /// <param name="elementType">The type of elements in the vector.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The resulting <see cref="FunctionPointerTypeSignature"/> instance.</returns>
    public static MethodSignature IList1ReplaceAllImpl(TypeSignature elementType, InteropReferences interopReferences)
    {
        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint, <ELEMENT_TYPE>*, HRESULT> ReplaceAll'
        return new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: interopReferences.CorLibTypeFactory.Int32),
            parameterTypes: [
                interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                interopReferences.CorLibTypeFactory.UInt32,
                elementType.MakePointerType()]);
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

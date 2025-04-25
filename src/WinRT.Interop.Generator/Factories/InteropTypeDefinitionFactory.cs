// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WinRT.Interop.Generator.Factories;

/// <summary>
/// A factory for interop type definitions.
/// </summary>
internal static class InteropTypeDefinitionFactory
{
    /// <summary>
    /// Creates a new type definition for the vtable of an 'IDelegate' interface.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateVftblType(
        TypeSignature typeSignature,
        CorLibTypeFactory corLibTypeFactory,
        ReferenceImporter referenceImporter)
    {
        // We're declaring an 'internal struct' type
        TypeDefinition vftblType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature),
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: referenceImporter.ImportType(typeof(ValueType)));

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT>'
        FunctionPointerTypeSignature queryInterfaceType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                referenceImporter.ImportType(typeof(Guid)).MakePointerType(),
                corLibTypeFactory.Void.MakePointerType().MakePointerType()]).MakeFunctionPointerType();

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        FunctionPointerTypeSignature addRefType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.UInt32),
            parameterTypes: [corLibTypeFactory.Void.MakePointerType()]).MakeFunctionPointerType();

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint>'
        FunctionPointerTypeSignature releaseType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.UInt32),
            parameterTypes: [corLibTypeFactory.Void.MakePointerType()]).MakeFunctionPointerType();

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void*, void*, int>'
        FunctionPointerTypeSignature invokeType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Void.MakePointerType()]).MakeFunctionPointerType();

        // The vtable layout for 'IDelegate' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, void*, void*, int> Invoke;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType));
        vftblType.Fields.Add(new FieldDefinition("Invoke"u8, FieldAttributes.Public, invokeType));

        return vftblType;
    }
}

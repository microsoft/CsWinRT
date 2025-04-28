// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known type definitions.
/// </summary>
internal static class WellKnownTypeDefinitionFactory
{
    /// <summary>
    /// Creates an <c>IUnknownVftbl</c> type.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <c>IUnknownVftbl</c> type.</returns>
    public static TypeDefinition IUnknownVftbl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // We're declaring an 'internal struct' type
        TypeDefinition vftblType = new(
            ns: null,
            name: "<IUnknownVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: referenceImporter.ImportType(typeof(ValueType)));

        // Get the 'IUnknown' signatures
        FunctionPointerTypeSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(corLibTypeFactory, referenceImporter);

        // The vtable layout for 'IUnknown' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType));

        return vftblType;
    }

    /// <summary>
    /// Creates an <c>IUnknownVftbl</c> type.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <c>IUnknownVftbl</c> type.</returns>
    public static TypeDefinition IInspectableVftbl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // We're declaring an 'internal struct' type
        TypeDefinition vftblType = new(
            ns: null,
            name: "<IInspectableVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: referenceImporter.ImportType(typeof(ValueType)));

        // Get the 'IUnknown' signatures
        FunctionPointerTypeSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(corLibTypeFactory, referenceImporter);

        // Get the 'IInspectable' signatures
        FunctionPointerTypeSignature getIidsType = WellKnownTypeSignatureFactory.GetIidsImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature getRuntimeClassNameType = WellKnownTypeSignatureFactory.GetRuntimeClassNameImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature getTrustLevelType = WellKnownTypeSignatureFactory.GetTrustLevelImpl(corLibTypeFactory, referenceImporter);

        // The vtable layout for 'IInspectable' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of a <see langword="delegate"/> type.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    /// <remarks>This method always assumes the <see langword="delegate"/> type will take two objects as input parameters.</remarks>
    public static TypeDefinition DelegateVftbl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        // We're declaring an 'internal struct' type
        TypeDefinition vftblType = new(
            ns: null,
            name: "<DelegateVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: referenceImporter.ImportType(typeof(ValueType)));

        // Get the 'IUnknown' signatures
        FunctionPointerTypeSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(corLibTypeFactory, referenceImporter);

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

    /// <summary>
    /// Creates a new type definition for the vtable of an 'IReference`1&lt;T&gt;' instantiation for some <see langword="delegate"/> type.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateReferenceVftbl(CorLibTypeFactory corLibTypeFactory, ReferenceImporter referenceImporter)
    {
        TypeDefinition vftblType = new(
            ns: null,
            name: "<DelegateReferenceVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: referenceImporter.ImportType(typeof(ValueType)));

        // Get the 'IUnknown' signatures
        FunctionPointerTypeSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(corLibTypeFactory, referenceImporter);

        // Get the 'IInspectable' signatures
        FunctionPointerTypeSignature getIidsType = WellKnownTypeSignatureFactory.GetIidsImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature getRuntimeClassNameType = WellKnownTypeSignatureFactory.GetRuntimeClassNameImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature getTrustLevelType = WellKnownTypeSignatureFactory.GetTrustLevelImpl(corLibTypeFactory, referenceImporter);

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, int>'
        FunctionPointerTypeSignature valueType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Void.MakePointerType().MakePointerType()]).MakeFunctionPointerType();

        // The vtable layout for 'IReference`1<T>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> Value;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType));
        vftblType.Fields.Add(new FieldDefinition("Value"u8, FieldAttributes.Public, valueType));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for COM interface entries for a <see langword="delegate"/> type.
    /// </summary>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateInterfaceEntriesType(ReferenceImporter referenceImporter)
    {
        TypeDefinition interfaceEntriesType = new(
            ns: null,
            name: "<DelegateInterfaceEntries>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: referenceImporter.ImportType(typeof(ValueType)));

        // Get the signature for the 'ComInterfaceEntry' type (this is a bit involved, so cache it)
        TypeSignature comInterfaceEntryType = referenceImporter.ImportType(typeof(ComWrappers.ComInterfaceEntry)).ToTypeSignature(isValueType: true);

        // The type layout looks like this:
        //
        // public ComInterfaceEntry Delegate;
        // public ComInterfaceEntry DelegateReference;
        // public ComInterfaceEntry IStringable;
        // public ComInterfaceEntry IWeakReferenceSource;
        // public ComInterfaceEntry IMarshal;
        // public ComInterfaceEntry IAgileObject;
        // public ComInterfaceEntry IInspectable;
        // public ComInterfaceEntry IUnknown;
        interfaceEntriesType.Fields.Add(new FieldDefinition("Delegate"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("DelegateReference"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IStringable"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IWeakReferenceSource"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IMarshal"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IAgileObject"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IInspectable"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IUnknown"u8, FieldAttributes.Public, comInterfaceEntryType));

        return interfaceEntriesType;
    }

    /// <summary>
    /// Creates types to use to declare RVA fields.
    /// </summary>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The <see cref="TypeDefinition"/> to use to contain all RVA fields.</returns>
    /// <remarks>
    /// The returned type will have exactly one nested type, for RVA fields of size 16 (ie. <see cref="Guid"/>).
    /// </remarks>
    public static TypeDefinition RvaFields(ReferenceImporter referenceImporter)
    {
        // Define the special '<RvaFields>' type, to contain all RVA fields
        TypeDefinition rvaFieldsType = new(
            ns: null,
            name: "<RvaFields>"u8,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // Define the data type for IID data
        TypeDefinition iidRvaDataType = new(
            ns: null,
            name: "IIDRvaDataSize=16",
            attributes: TypeAttributes.NestedAssembly | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            baseType: referenceImporter.ImportType(typeof(ValueType)))
        {
            ClassLayout = new ClassLayout(packingSize: 1, classSize: 16)
        };

        // The IID RVA type is nested under the '<RvaFields>' type
        rvaFieldsType.NestedTypes.Add(iidRvaDataType);

        return rvaFieldsType;
    }
}

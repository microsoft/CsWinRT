// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Factories;

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
            name: InteropUtf8NameFactory.TypeName(typeSignature, "Vftbl"),
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
    /// Creates a new type definition for the vtable of an 'IReference`1&lt;T&gt;' instantiation for some 'IDelegate' type.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateReferenceVftblType(
        TypeSignature typeSignature,
        CorLibTypeFactory corLibTypeFactory,
        ReferenceImporter referenceImporter)
    {
        TypeDefinition vftblType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "ReferenceVftbl"),
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: referenceImporter.ImportType(typeof(ValueType)));

        // Get the 'IUnknown' signatures
        FunctionPointerTypeSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(corLibTypeFactory, referenceImporter);
        FunctionPointerTypeSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(corLibTypeFactory, referenceImporter);

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>'
        FunctionPointerTypeSignature getIidsType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.UInt32.MakePointerType(),
                referenceImporter.ImportType(typeof(Guid)).MakePointerType().MakePointerType()]).MakeFunctionPointerType();

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, int>'
        FunctionPointerTypeSignature getRuntimeClassNameType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Void.MakePointerType().MakePointerType()]).MakeFunctionPointerType();

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, TrustLevel*, int>'
        FunctionPointerTypeSignature getTrustLevelType = new MethodSignature(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: referenceImporter.ImportType(typeof(CallConvMemberFunction)),
                isRequired: false,
                baseType: corLibTypeFactory.Int32),
            parameterTypes: [
                corLibTypeFactory.Void.MakePointerType(),
                corLibTypeFactory.Int32.MakePointerType()]).MakeFunctionPointerType();

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
    /// Creates a new type definition for COM interface entries for an 'IDelegate' interface.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateInterfaceEntriesType(TypeSignature typeSignature, ReferenceImporter referenceImporter)
    {
        TypeDefinition interfaceEntriesType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "InterfaceEntries"),
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
    /// Creates a new type definition for the implementation of the COM interface entries for an 'IDelegate' interface.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="delegateInterfaceEntriesType">The <see cref="TypeDefinition"/> instance returned by <see cref="DelegateInterfaceEntriesType"/>.</param>
    /// <param name="delegateImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="DelegateImplType"/>.</param>
    /// <param name="owningModule">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateInterfaceEntriesImplType(
        TypeSignature typeSignature,
        TypeDefinition delegateInterfaceEntriesType,
        TypeDefinition delegateImplType,
        ModuleDefinition owningModule)
    {
        // We're declaring an 'internal static class' type
        TypeDefinition implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "InterfaceEntriesImpl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // Get the signature for the 'ComInterfaceEntry' type (this is a bit involved, so cache it)
        TypeDefinition fixedAddressValueTypeAttributeType = owningModule.DefaultImporter.ImportType(typeof(FixedAddressValueTypeAttribute)).Resolve()!;

        // The interface entries field looks like this:
        //
        // [FixedAddressValueType]
        // public static readonly <DELEGATE_TYPE_InterfaceEntries Entries;
        //
        // The '[FixedAddressValueType]' attribute allows ILC to pre-initialize the entire vtable (in .rdata).
        FieldDefinition entriesField = new("Entries"u8, FieldAttributes.Public, delegateInterfaceEntriesType.ToTypeSignature(isValueType: true))
        {
            CustomAttributes = { }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the interface entries
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(owningModule);

        // We need to create a new method body bound to this constructor
        cctor.CilMethodBody = new CilMethodBody(cctor);

        // Resolve 'ComInterfaceEntry', so we can set its fields:
        //   - [0]: Guid IID
        //   - [1]: nint Vtable
        TypeDefinition comInterfaceEntryType = owningModule.DefaultImporter.ImportType(typeof(ComWrappers.ComInterfaceEntry)).Resolve()!;

        CilInstructionCollection instructions = cctor.CilMethodBody.Instructions;

        // Initialize the COM interface entries, doing this for each entry:
        //
        // Entries.<FIELD#i>.IID = <INTERFACE>Impl.IID;
        // Entries.<FIELD#i>.Vtable = <INTERFACE>Impl.Vtable;
        //
        // For the two generated 'Impl' types, we just directly store the address of the RVA vtable data.
        _ = instructions.Add(CilOpCodes.Ldsflda, entriesField);
        _ = instructions.Add(CilOpCodes.Ldflda, delegateInterfaceEntriesType.Fields[0]);
        _ = instructions.Add(CilOpCodes.Ldsflda, delegateImplType.Fields[0]);
        _ = instructions.Add(CilOpCodes.Stfld, comInterfaceEntryType.Fields[1]);

        return implType;
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the vtable for an 'IDelegate' interface.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="delegateVfbtlType">The <see cref="TypeDefinition"/> instance returned by <see cref="DelegateVftblType"/>.</param>
    /// <param name="owningModule">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateImplType(
        TypeSignature typeSignature,
        TypeDefinition delegateVfbtlType,
        ModuleDefinition owningModule)
    {
        // We're declaring an 'internal static class' type
        TypeDefinition implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "Impl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // Get the signature for the 'ComInterfaceEntry' type (this is a bit involved, so cache it)
        // TypeSignature comInterfaceEntryType = referenceImporter.ImportType(typeof(FixedAddressValueTypeAttribute));

        // The vtable field looks like this:
        //
        // [FixedAddressValueType]
        // public static readonly <DELEGATE_TYPE>Vftbl Vftbl;
        //
        // The '[FixedAddressValueType]' attribute allows ILC to pre-initialize the entire vtable (in .rdata).
        FieldDefinition entriesField = new("Vftbl"u8, FieldAttributes.Public, delegateVfbtlType.ToTypeSignature())
        {
            CustomAttributes = { }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(owningModule);

        // We need to create a new method body bound to this constructor
        cctor.CilMethodBody = new CilMethodBody(cctor);

        CilInstructionCollection instructions = cctor.CilMethodBody.Instructions;

        // Initialize the COM interface entries:
        //
        // 
        //_ = instructions.Add(CilOpCodes.Ldsflda, implType.Fields[0]);
        //_ = instructions.Add(CilOpCodes.Ldflda, delegateInterfaceEntriesType.Fields[0]);


        return implType;
    }
}

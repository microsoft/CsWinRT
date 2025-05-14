// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known type definitions.
/// </summary>
internal static class WellKnownTypeDefinitionFactory
{
    /// <summary>
    /// Creates an <c>IUnknownVftbl</c> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <c>IUnknownVftbl</c> type.</returns>
    public static TypeDefinition IUnknownVftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        // We're declaring an 'internal struct' type
        TypeDefinition vftblType = new(
            ns: null,
            name: "<IUnknownVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.ValueType.Import(module));

        // Get the 'IUnknown' signatures
        MethodSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(interopReferences);
        MethodSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(interopReferences);
        MethodSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(interopReferences);

        // The vtable layout for 'IUnknown' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates an <c>IUnknownVftbl</c> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <c>IUnknownVftbl</c> type.</returns>
    public static TypeDefinition IInspectableVftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        // We're declaring an 'internal struct' type
        TypeDefinition vftblType = new(
            ns: null,
            name: "<IInspectableVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.ValueType.Import(module));

        // Get the 'IUnknown' signatures
        MethodSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(interopReferences);
        MethodSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(interopReferences);
        MethodSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(interopReferences);

        // Get the 'IInspectable' signatures
        MethodSignature getIidsType = WellKnownTypeSignatureFactory.GetIidsImpl(interopReferences);
        MethodSignature getRuntimeClassNameType = WellKnownTypeSignatureFactory.GetRuntimeClassNameImpl(interopReferences);
        MethodSignature getTrustLevelType = WellKnownTypeSignatureFactory.GetTrustLevelImpl(interopReferences);

        // The vtable layout for 'IInspectable' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of a <see cref="Delegate"/> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    /// <remarks>This method always assumes the <see cref="Delegate"/> type will take two objects as input parameters.</remarks>
    public static TypeDefinition DelegateVftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        // We're declaring an 'internal struct' type
        TypeDefinition vftblType = new(
            ns: null,
            name: "<DelegateVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.ValueType.Import(module));

        // Get the 'IUnknown' signatures
        MethodSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(interopReferences);
        MethodSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(interopReferences);
        MethodSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(interopReferences);

        // Also get the 'Invoke' signature
        MethodSignature invokeType = WellKnownTypeSignatureFactory.InvokeImpl(interopReferences);

        // The vtable layout for 'IDelegate' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, void*, void*, int> Invoke;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Invoke"u8, FieldAttributes.Public, invokeType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an 'IReference`1&lt;T&gt;' instantiation for some <see cref="Delegate"/> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateReferenceVftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: null,
            name: "<DelegateReferenceVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.ValueType.Import(module));

        // Get the 'IUnknown' signatures
        MethodSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(interopReferences);
        MethodSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(interopReferences);
        MethodSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(interopReferences);

        // Get the 'IInspectable' signatures
        MethodSignature getIidsType = WellKnownTypeSignatureFactory.GetIidsImpl(interopReferences);
        MethodSignature getRuntimeClassNameType = WellKnownTypeSignatureFactory.GetRuntimeClassNameImpl(interopReferences);
        MethodSignature getTrustLevelType = WellKnownTypeSignatureFactory.GetTrustLevelImpl(interopReferences);

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, int>'
        MethodSignature valueType = new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: module.CorLibTypeFactory.Int32),
            parameterTypes: [
                module.CorLibTypeFactory.Void.MakePointerType(),
                module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]);

        // The vtable layout for 'IReference`1<T>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Value;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Value"u8, FieldAttributes.Public, valueType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for COM interface entries for a <see cref="Delegate"/> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateInterfaceEntriesType(InteropReferences interopReferences, ModuleDefinition module)
    {
        TypeDefinition interfaceEntriesType = new(
            ns: null,
            name: "<DelegateInterfaceEntries>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.ValueType.Import(module));

        // Get the signature for the 'ComInterfaceEntry' type (this is a bit involved, so cache it)
        TypeSignature comInterfaceEntryType = interopReferences.ComInterfaceEntry.Import(module).ToTypeSignature(isValueType: true);

        // The type layout looks like this:
        //
        // public ComInterfaceEntry Delegate;
        // public ComInterfaceEntry DelegateReference;
        // public ComInterfaceEntry IPropertyValue;
        // public ComInterfaceEntry IStringable;
        // public ComInterfaceEntry IWeakReferenceSource;
        // public ComInterfaceEntry IMarshal;
        // public ComInterfaceEntry IAgileObject;
        // public ComInterfaceEntry IInspectable;
        // public ComInterfaceEntry IUnknown;
        interfaceEntriesType.Fields.Add(new FieldDefinition("Delegate"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("DelegateReference"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IPropertyValue"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IStringable"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IWeakReferenceSource"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IMarshal"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IAgileObject"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IInspectable"u8, FieldAttributes.Public, comInterfaceEntryType));
        interfaceEntriesType.Fields.Add(new FieldDefinition("IUnknown"u8, FieldAttributes.Public, comInterfaceEntryType));

        return interfaceEntriesType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an 'IKeyValuePair`2&lt;TKey, TValue&gt;' instantiation for some <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IKeyValuePairVftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: null,
            name: "<IKeyValuePairVftbl>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.ValueType.Import(module));

        // Get the 'IUnknown' signatures
        MethodSignature queryInterfaceType = WellKnownTypeSignatureFactory.QueryInterfaceImpl(interopReferences);
        MethodSignature addRefType = WellKnownTypeSignatureFactory.AddRefImpl(interopReferences);
        MethodSignature releaseType = WellKnownTypeSignatureFactory.ReleaseImpl(interopReferences);

        // Get the 'IInspectable' signatures
        MethodSignature getIidsType = WellKnownTypeSignatureFactory.GetIidsImpl(interopReferences);
        MethodSignature getRuntimeClassNameType = WellKnownTypeSignatureFactory.GetRuntimeClassNameImpl(interopReferences);
        MethodSignature getTrustLevelType = WellKnownTypeSignatureFactory.GetTrustLevelImpl(interopReferences);

        // Signature for 'delegate* unmanaged[MemberFunction]<void*, void**, int>'
        MethodSignature valueType = new(
            attributes: CallingConventionAttributes.Unmanaged,
            returnType: new CustomModifierTypeSignature(
                modifierType: interopReferences.CallConvMemberFunction,
                isRequired: false,
                baseType: module.CorLibTypeFactory.Int32),
            parameterTypes: [
                module.CorLibTypeFactory.Void.MakePointerType(),
                module.CorLibTypeFactory.Void.MakePointerType()]);

        // The vtable layout for 'IReference`1<T>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_Key;
        // public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_Value;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Key"u8, FieldAttributes.Public, valueType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Value"u8, FieldAttributes.Public, valueType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for COM interface entries for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IKeyValuePairInterfaceEntriesType(InteropReferences interopReferences, ModuleDefinition module)
    {
        TypeDefinition interfaceEntriesType = new(
            ns: null,
            name: "<IKeyValuePairInterfaceEntries>"u8,
            attributes: TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.ValueType.Import(module));

        // Get the signature for the 'ComInterfaceEntry' type (this is a bit involved, so cache it)
        TypeSignature comInterfaceEntryType = interopReferences.ComInterfaceEntry.Import(module).ToTypeSignature(isValueType: true);

        // The type layout looks like this:
        //
        // public ComInterfaceEntry IKeyValuePair;
        // public ComInterfaceEntry IStringable;
        // public ComInterfaceEntry IWeakReferenceSource;
        // public ComInterfaceEntry IMarshal;
        // public ComInterfaceEntry IAgileObject;
        // public ComInterfaceEntry IInspectable;
        // public ComInterfaceEntry IUnknown;
        interfaceEntriesType.Fields.Add(new FieldDefinition("IKeyValuePair"u8, FieldAttributes.Public, comInterfaceEntryType));
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
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The <see cref="TypeDefinition"/> to use to contain all RVA fields.</returns>
    /// <remarks>
    /// The returned type will have exactly one nested type, for RVA fields of size 16 (ie. <see cref="Guid"/>).
    /// </remarks>
    public static TypeDefinition RvaFields(InteropReferences interopReferences, ModuleDefinition module)
    {
        // Define the special '<RvaFields>' type, to contain all RVA fields
        TypeDefinition rvaFieldsType = new(
            ns: null,
            name: "<RvaFields>"u8,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        // Define the data type for IID data
        TypeDefinition iidRvaDataType = new(
            ns: null,
            name: "IIDRvaData(Size=16|Align=4)",
            attributes: TypeAttributes.NestedAssembly | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            baseType: interopReferences.ValueType.Import(module))
        {
            ClassLayout = new ClassLayout(packingSize: 4, classSize: 16)
        };

        // The IID RVA type is nested under the '<RvaFields>' type
        rvaFieldsType.NestedTypes.Add(iidRvaDataType);

        return rvaFieldsType;
    }

    /// <summary>
    /// Creates a type to hold implementation detail helpers.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The <see cref="TypeDefinition"/> to hold implementation detail helpers.</returns>
    public static TypeDefinition InteropImplementationDetails(InteropReferences interopReferences, ModuleDefinition module)
    {
        // Define the special '<InteropImplementationDetails>' type, with all internal helpers
        TypeDefinition interopImplementationDetailsType = new(
            ns: null,
            name: "<InteropImplementationDetails>"u8,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        interopImplementationDetailsType.Methods.Add(WellKnownMemberDefinitionFactory.ComputeReadOnlySpanHash(interopReferences, module));

        return interopImplementationDetailsType;
    }
}

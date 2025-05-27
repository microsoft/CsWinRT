// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known type definitions.
/// </summary>
internal static partial class WellKnownTypeDefinitionFactory
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
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IEnumerator{T}"/> instantiation.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IEnumerator1Vftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: null,
            name: "<IEnumerator1Vftbl>"u8,
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

        // Get the 'IIterator`1' signatures
        MethodSignature get_CurrentType = WellKnownTypeSignatureFactory.IEnumerator1CurrentImpl(interopReferences);
        MethodSignature get_HasCurrentType = WellKnownTypeSignatureFactory.IEnumerator1HasCurrentImpl(interopReferences);
        MethodSignature moveNextType = get_HasCurrentType;
        MethodSignature getManyType = WellKnownTypeSignatureFactory.IEnumerator1GetManyImpl(interopReferences);

        // The vtable layout for 'IEnumerator<T>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_Current;
        // public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_HasCurrent;
        // public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> MoveNext;
        // public delegate* unmanaged[MemberFunction]<void*, uint, void*, uint*, HRESULT> GetMany;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Current"u8, FieldAttributes.Public, get_CurrentType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_HasCurrent"u8, FieldAttributes.Public, get_HasCurrentType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("MoveNext"u8, FieldAttributes.Public, moveNextType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetMany"u8, FieldAttributes.Public, getManyType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IEnumerable{T}"/> instantiation.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IEnumerable1Vftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: null,
            name: "<IEnumerable1Vftbl>"u8,
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

        // Get the 'IIterable`1' signatures
        MethodSignature firstType = WellKnownTypeSignatureFactory.IEnumerable1FirstImpl(interopReferences);

        // The vtable layout for 'IEnumerable<T>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> First;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("First"u8, FieldAttributes.Public, firstType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IReadOnlyList{T}"/> instantiation.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    /// <remarks>
    /// Unlike <see cref="IReadOnlyList1Vftbl(Utf8String?, Utf8String, TypeSignature, InteropReferences, ModuleDefinition)"/>,
    /// this overload just uses <see cref="void"/><c>*</c> as element type, so it can be shared across reference types.
    /// </remarks>
    public static TypeDefinition IReadOnlyList1Vftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        return IReadOnlyList1Vftbl(
            ns: null,
            name: "<IReadOnlyList1Vftbl>"u8,
            elementType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IReadOnlyList{T}"/> instantiation.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="elementType">The element type for the vtable type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IReadOnlyList1Vftbl(
        Utf8String? ns,
        Utf8String name,
        TypeSignature elementType,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: ns,
            name: name,
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

        // Get the 'IVectorView`1' signatures
        MethodSignature getAtType = WellKnownTypeSignatureFactory.IReadOnlyList1GetAtImpl(elementType, interopReferences);
        MethodSignature get_SizeType = WellKnownTypeSignatureFactory.IReadOnlyList1get_SizeImpl(interopReferences);
        MethodSignature indexOfType = WellKnownTypeSignatureFactory.IReadOnlyList1IndexOfImpl(elementType, interopReferences);
        MethodSignature getManyType = WellKnownTypeSignatureFactory.IReadOnlyList1GetManyImpl(elementType, interopReferences);

        // The vtable layout for 'IReadOnlyList<T>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, uint, <ELEMENT_TYPE>*, HRESULT> GetAt;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;
        // public delegate* unmanaged[MemberFunction]<void*, <ELEMENT_TYPE>, uint*, HRESULT> IndexOf;
        // public delegate* unmanaged[MemberFunction]<void*, uint, <ELEMENT_TYPE>*, uint*, HRESULT> GetMany;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetAt"u8, FieldAttributes.Public, getAtType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Size"u8, FieldAttributes.Public, get_SizeType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("IndexOf"u8, FieldAttributes.Public, indexOfType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetMany"u8, FieldAttributes.Public, getManyType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IList{T}"/> instantiation.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    /// <remarks>
    /// Unlike <see cref="IList1Vftbl(Utf8String?, Utf8String, TypeSignature, InteropReferences, ModuleDefinition)"/>,
    /// this overload just uses <see cref="void"/><c>*</c> as element type, so it can be shared across reference types.
    /// </remarks>
    public static TypeDefinition IList1Vftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        return IList1Vftbl(
            ns: null,
            name: "<IList1Vftbl>"u8,
            elementType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IList{T}"/> instantiation.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="elementType">The element type for the vtable type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IList1Vftbl(
        Utf8String? ns,
        Utf8String name,
        TypeSignature elementType,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: ns,
            name: name,
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

        // Get the 'IVector`1' signatures
        MethodSignature getAtType = WellKnownTypeSignatureFactory.IList1GetAtImpl(elementType, interopReferences);
        MethodSignature get_SizeType = WellKnownTypeSignatureFactory.IList1get_SizeImpl(interopReferences);
        MethodSignature getViewType = WellKnownTypeSignatureFactory.IList1GetViewImpl(interopReferences);
        MethodSignature indexOfType = WellKnownTypeSignatureFactory.IList1IndexOfImpl(elementType, interopReferences);
        MethodSignature setAtType = WellKnownTypeSignatureFactory.IList1SetAtImpl(elementType, interopReferences);
        MethodSignature insertAtType = WellKnownTypeSignatureFactory.IList1InsertAtImpl(elementType, interopReferences);
        MethodSignature removeAtType = WellKnownTypeSignatureFactory.IList1RemoveAtImpl(interopReferences);
        MethodSignature appendType = WellKnownTypeSignatureFactory.IList1AppendImpl(elementType, interopReferences);
        MethodSignature removeAtEndType = WellKnownTypeSignatureFactory.IList1RemoveAtEndImpl(interopReferences);
        MethodSignature clearType = WellKnownTypeSignatureFactory.IList1ClearImpl(interopReferences);
        MethodSignature getManyType = WellKnownTypeSignatureFactory.IList1GetManyImpl(elementType, interopReferences);
        MethodSignature replaceAllType = WellKnownTypeSignatureFactory.IList1ReplaceAllImpl(elementType, interopReferences);


        // The vtable layout for 'IList<T>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> GetAt;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;
        // public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetView;
        // public delegate* unmanaged[MemberFunction]<void*, void*, uint*, bool*, HRESULT> IndexOf;
        // public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> SetAt;
        // public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> InsertAt;
        // public delegate* unmanaged[MemberFunction]<void*, uint, HRESULT> RemoveAt;
        // public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> Append;
        // public delegate* unmanaged[MemberFunction]<void*, HRESULT> RemoveAtEnd;
        // public delegate* unmanaged[MemberFunction]<void*, HRESULT> Clear;
        // public delegate* unmanaged[MemberFunction]<void*, uint, uint, void*, uint*, HRESULT> GetMany;
        // public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> ReplaceAll;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetAt"u8, FieldAttributes.Public, getAtType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Size"u8, FieldAttributes.Public, get_SizeType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetView"u8, FieldAttributes.Public, getViewType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("IndexOf"u8, FieldAttributes.Public, indexOfType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("SetAt"u8, FieldAttributes.Public, setAtType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("InsertAt"u8, FieldAttributes.Public, insertAtType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("RemoveAt"u8, FieldAttributes.Public, removeAtType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Append"u8, FieldAttributes.Public, appendType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("RemoveAtEnd"u8, FieldAttributes.Public, removeAtEndType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Clear"u8, FieldAttributes.Public, clearType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetMany"u8, FieldAttributes.Public, getManyType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("ReplaceAll"u8, FieldAttributes.Public, replaceAllType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> instantiation.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    /// <remarks>
    /// Unlike <see cref="IReadOnlyDictionary2Vftbl(Utf8String?, Utf8String, TypeSignature, TypeSignature, InteropReferences, ModuleDefinition)"/>,
    /// this overload just uses <see cref="void"/><c>*</c> as key and value types, so it can be shared across reference types (for both types).
    /// </remarks>
    public static TypeDefinition IReadOnlyDictionary2Vftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        return IReadOnlyDictionary2Vftbl(
            ns: null,
            name: "<IReadOnlyDictionary2Vftbl>"u8,
            keyType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
            valueType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> instantiation.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="keyType">The key type for the vtable type.</param>
    /// <param name="valueType">The value type for the vtable type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IReadOnlyDictionary2Vftbl(
        Utf8String? ns,
        Utf8String name,
        TypeSignature keyType,
        TypeSignature valueType,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: ns,
            name: name,
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

        // Get the 'IMapView`1' signatures
        MethodSignature lookupType = WellKnownTypeSignatureFactory.IReadOnlyDictionary2LookupImpl(keyType, valueType, interopReferences);
        MethodSignature get_SizeType = WellKnownTypeSignatureFactory.IReadOnlyDictionary2get_SizeImpl(interopReferences);
        MethodSignature hasKeyType = WellKnownTypeSignatureFactory.IReadOnlyDictionary2HasKeyImpl(keyType, interopReferences);
        MethodSignature splitType = WellKnownTypeSignatureFactory.IReadOnlyDictionary2SplitImpl(interopReferences);

        // The vtable layout for 'IReadOnlyDictionary<TKey, TValue>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING, HSTRING*, HRESULT> Lookup;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING, bool*, HRESULT> HasKey;
        // public delegate* unmanaged[MemberFunction]<void*, void**, void**, HRESULT> Split;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Lookup"u8, FieldAttributes.Public, lookupType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Size"u8, FieldAttributes.Public, get_SizeType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("HasKey"u8, FieldAttributes.Public, hasKeyType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Split"u8, FieldAttributes.Public, splitType.Import(module).MakeFunctionPointerType()));

        return vftblType;
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> instantiation.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    /// <remarks>
    /// Unlike <see cref="IDictionary2Vftbl(Utf8String?, Utf8String, TypeSignature, TypeSignature, InteropReferences, ModuleDefinition)"/>,
    /// this overload just uses <see cref="void"/><c>*</c> as key and value types, so it can be shared across reference types (for both types).
    /// </remarks>
    public static TypeDefinition IDictionary2Vftbl(InteropReferences interopReferences, ModuleDefinition module)
    {
        return IDictionary2Vftbl(
            ns: null,
            name: "<IDictionary2Vftbl>"u8,
            keyType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
            valueType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new type definition for the vtable of an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> instantiation.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="keyType">The key type for the vtable type.</param>
    /// <param name="valueType">The value type for the vtable type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition IDictionary2Vftbl(
        Utf8String? ns,
        Utf8String name,
        TypeSignature keyType,
        TypeSignature valueType,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        TypeDefinition vftblType = new(
            ns: ns,
            name: name,
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

        // Get the 'IMap`2' signatures
        MethodSignature lookupType = WellKnownTypeSignatureFactory.IDictionary2LookupImpl(keyType, valueType, interopReferences);
        MethodSignature get_SizeType = WellKnownTypeSignatureFactory.IDictionary2get_SizeImpl(interopReferences);
        MethodSignature hasKeyType = WellKnownTypeSignatureFactory.IDictionary2HasKeyImpl(keyType, interopReferences);
        MethodSignature getViewType = WellKnownTypeSignatureFactory.IDictionary2GetViewImpl(interopReferences);
        MethodSignature insertType = WellKnownTypeSignatureFactory.IDictionary2InsertImpl(keyType, valueType, interopReferences);
        MethodSignature removeType = WellKnownTypeSignatureFactory.IDictionary2RemoveImpl(keyType, interopReferences);
        MethodSignature clearType = WellKnownTypeSignatureFactory.IDictionary2ClearImpl(interopReferences);

        // The vtable layout for 'IDictionary<TKey, TValue>' looks like this:
        //
        // public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
        // public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        // public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
        // public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING, HSTRING*, HRESULT> Lookup;
        // public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING, bool*, HRESULT> HasKey;
        // public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetView;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING, HSTRING, bool*, HRESULT> Insert;
        // public delegate* unmanaged[MemberFunction]<void*, HSTRING, HRESULT> Remove;
        // public delegate* unmanaged[MemberFunction]<void*, HRESULT> Clear;
        vftblType.Fields.Add(new FieldDefinition("QueryInterface"u8, FieldAttributes.Public, queryInterfaceType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("AddRef"u8, FieldAttributes.Public, addRefType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Release"u8, FieldAttributes.Public, releaseType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetIids"u8, FieldAttributes.Public, getIidsType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetRuntimeClassName"u8, FieldAttributes.Public, getRuntimeClassNameType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetTrustLevel"u8, FieldAttributes.Public, getTrustLevelType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Lookup"u8, FieldAttributes.Public, lookupType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Size"u8, FieldAttributes.Public, get_SizeType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("HasKey"u8, FieldAttributes.Public, hasKeyType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("GetView"u8, FieldAttributes.Public, getViewType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Insert"u8, FieldAttributes.Public, insertType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Remove"u8, FieldAttributes.Public, removeType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("Clear"u8, FieldAttributes.Public, clearType.Import(module).MakeFunctionPointerType()));

        return vftblType;
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
        MethodSignature get_KeyOrValueType = new(
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
        vftblType.Fields.Add(new FieldDefinition("get_Key"u8, FieldAttributes.Public, get_KeyOrValueType.Import(module).MakeFunctionPointerType()));
        vftblType.Fields.Add(new FieldDefinition("get_Value"u8, FieldAttributes.Public, get_KeyOrValueType.Import(module).MakeFunctionPointerType()));

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

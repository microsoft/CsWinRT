// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using AsmResolver;
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
    /// Creates a new type definition for the implementation of the COM interface entries for an 'IDelegate' interface.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="delegateInterfaceEntriesType">The <see cref="TypeDefinition"/> instance returned by <see cref="WellKnownTypeDefinitionFactory.DelegateInterfaceEntriesType"/>.</param>
    /// <param name="delegateImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="DelegateImplType"/>.</param>
    /// <param name="delegateReferenceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="DelegateReferenceImplType"/>.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateInterfaceEntriesImplType(
        TypeSignature delegateType,
        TypeDefinition delegateInterfaceEntriesType,
        TypeDefinition delegateImplType,
        TypeDefinition delegateReferenceImplType,
        ModuleDefinition module)
    {
        // We're declaring an 'internal static class' type
        TypeDefinition implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "InterfaceEntriesImpl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // The interface entries field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateInterfaceEntries> Entries;
        //
        // The '[FixedAddressValueType]' attribute allows ILC to pre-initialize the entire vtable (in .rdata).
        FieldDefinition entriesField = new("Entries"u8, FieldAttributes.Private, delegateInterfaceEntriesType.ToTypeSignature(isValueType: true))
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the interface entries
        InteropMethodBodyFactory.InterfaceEntriesImpl(
            cctor: implType.GetOrCreateStaticConstructor(module),
            entriesField: entriesField,
            entriesFieldType: delegateInterfaceEntriesType,
            module: module,
            implTypes: [
                delegateImplType,
                delegateReferenceImplType]);

        // The 'Vtables' property type has the signature being 'ComWrappers.ComInterfaceEntry*'
        PointerTypeSignature vtablesPropertyType = module.DefaultImporter
            .ImportType(typeof(ComWrappers.ComInterfaceEntry))
            .MakePointerType();

        // The 'Vtables' property doesn't have a special signature
        PropertySignature vtablePropertySignature = new(CallingConventionAttributes.Property, vtablesPropertyType, []);

        // Create the 'Vtables' property
        PropertyDefinition vtablesProperty = new("Vtables"u8, PropertyAttributes.None, vtablePropertySignature)
        {
            GetMethod = new MethodDefinition(
                name: "get_Vtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: new MethodSignature(
                    attributes: CallingConventionAttributes.Default,
                    returnType: vtablesPropertyType,
                    parameterTypes: []))
            { IsAggressiveInlining = true }
        };

        implType.Properties.Add(vtablesProperty);
        implType.Methods.Add(vtablesProperty.GetMethod!);

        // Create a method body for the 'Vtables' property
        CilInstructionCollection get_VtablesInstructions = vtablesProperty.GetMethod!.CreateAndBindCilMethodBody().Instructions;

        // The 'get_Vtables' method directly returns the 'Entries' field address
        _ = get_VtablesInstructions.Add(CilOpCodes.Ldsflda, entriesField);
        _ = get_VtablesInstructions.Add(CilOpCodes.Ret);

        return implType;
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the vtable for an 'IDelegate' interface.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="delegateVftblType">The <see cref="TypeDefinition"/> instance returned by <see cref="WellKnownTypeDefinitionFactory.DelegateVftbl"/>.</param>
    /// <param name="iidRvaDataType">The type to use for IID RVA fields.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateImplType(
        TypeSignature delegateType,
        TypeDefinition delegateVftblType,
        TypeDefinition iidRvaDataType,
        ModuleDefinition module,
        out FieldDefinition iidRvaField)
    {
        // We're declaring an 'internal static class' type
        TypeDefinition implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "Impl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // The vtable field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateVftbl> Vftbl;
        FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, delegateVftblType.ToTypeSignature())
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(vftblField);

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the delegate vtable
        // TODO
        _ = cctorInstructions.Add(CilOpCodes.Ret);

        // Create the field for the IID for the delegate type
        WellKnownMemberDefinitionFactory.IID(
            iidRvaFieldName: InteropUtf8NameFactory.TypeName(delegateType, "IID"),
            iidRvaDataType: iidRvaDataType,
            module: module,
            iid: Guid.NewGuid(),
            out iidRvaField,
            out PropertyDefinition iidProperty,
            out MethodDefinition get_iidMethod);

        implType.Properties.Add(iidProperty);
        implType.Methods.Add(get_iidMethod);

        // The 'Vtable' property has the signature being just 'nint'
        PropertySignature vtablePropertySignature = new(CallingConventionAttributes.Property, module.CorLibTypeFactory.IntPtr, []);

        // Create the 'Vtable' property
        PropertyDefinition vtableProperty = new("Vtable"u8, PropertyAttributes.None, vtablePropertySignature)
        {
            GetMethod = new MethodDefinition(
                name: "get_Vtable"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: new MethodSignature(
                    attributes: CallingConventionAttributes.Default,
                    returnType: module.CorLibTypeFactory.IntPtr,
                    parameterTypes: []))
            { IsAggressiveInlining = true }
        };

        implType.Properties.Add(vtableProperty);
        implType.Methods.Add(vtableProperty.GetMethod!);

        // Create a method body for the 'Vtable' property
        CilInstructionCollection get_VtableInstructions = vtableProperty.GetMethod!.CreateAndBindCilMethodBody().Instructions;

        // The 'get_Vtable' method directly returns the 'Vftbl' field address
        _ = get_VtableInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = get_VtableInstructions.Add(CilOpCodes.Ret);

        // Define the 'Invoke' methods as follows:
        //
        // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        // private static int Invoke(void* thisPtr, void* sender, void* e)
        MethodDefinition invokeMethod = new(
            name: "Invoke"u8,
            attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: module.CorLibTypeFactory.Int32,
                parameterTypes: [
                    module.CorLibTypeFactory.Void.MakePointerType(),
                    module.CorLibTypeFactory.Void.MakePointerType(),
                    module.CorLibTypeFactory.Void.MakePointerType()]))
        {
            CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(module) }
        };

        implType.Methods.Add(invokeMethod);

        // Create a method body for the 'Invoke' method
        CilInstructionCollection invokeInstructions = invokeMethod.CreateAndBindCilMethodBody().Instructions;

        _ = invokeInstructions.Add(CilOpCodes.Ldc_I4_0);
        _ = invokeInstructions.Add(CilOpCodes.Ret);

        return implType;
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the vtable for the 'IReference`1&lt;T&gt;' instantiation for some <see langword="delegate"/> type.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="delegateReferenceVftblType">The <see cref="TypeDefinition"/> instance returned by <see cref="WellKnownTypeDefinitionFactory.DelegateVftbl"/>.</param>
    /// <param name="iidRvaDataType">The type to use for IID RVA fields.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateReferenceImplType(
        TypeSignature delegateType,
        TypeDefinition delegateReferenceVftblType,
        TypeDefinition iidRvaDataType,
        ModuleDefinition module,
        out FieldDefinition iidRvaField)
    {
        // We're declaring an 'internal static class' type
        TypeDefinition implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "ReferenceImpl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // The vtable field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateReferenceVftbl> Vftbl;
        FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, delegateReferenceVftblType.ToTypeSignature())
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(vftblField);

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the delegate vtable
        // TODO
        _ = cctorInstructions.Add(CilOpCodes.Ret);

        // Create the field for the IID for the boxed delegate type
        WellKnownMemberDefinitionFactory.IID(
            iidRvaFieldName: InteropUtf8NameFactory.TypeName(delegateType, "ReferenceIID"),
            iidRvaDataType: iidRvaDataType,
            module: module,
            iid: Guid.NewGuid(),
            out iidRvaField,
            out PropertyDefinition iidProperty,
            out MethodDefinition get_iidMethod);

        implType.Properties.Add(iidProperty);
        implType.Methods.Add(get_iidMethod);

        // The 'Vtable' property has the signature being just 'nint'
        PropertySignature vtablePropertySignature = new(CallingConventionAttributes.Property, module.CorLibTypeFactory.IntPtr, []);

        // Create the 'Vtable' property
        PropertyDefinition vtableProperty = new("Vtable"u8, PropertyAttributes.None, vtablePropertySignature)
        {
            GetMethod = new MethodDefinition(
                name: "get_Vtable"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: new MethodSignature(
                    attributes: CallingConventionAttributes.Default,
                    returnType: module.CorLibTypeFactory.IntPtr,
                    parameterTypes: []))
            { IsAggressiveInlining = true }
        };

        implType.Properties.Add(vtableProperty);
        implType.Methods.Add(vtableProperty.GetMethod!);

        // Create a method body for the 'Vtable' property
        CilInstructionCollection get_VtableInstructions = vtableProperty.GetMethod!.CreateAndBindCilMethodBody().Instructions;

        // The 'get_Vtable' method directly returns the 'Vftbl' field address
        _ = get_VtableInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = get_VtableInstructions.Add(CilOpCodes.Ret);

        // Define the 'Value' methods as follows:
        //
        // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        // private static int Value(void* thisPtr, void** result)
        MethodDefinition valueMethod = new(
            name: "Value"u8,
            attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: module.CorLibTypeFactory.Int32,
                parameterTypes: [
                    module.CorLibTypeFactory.Void.MakePointerType(),
                    module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
        {
            CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(module) }
        };

        implType.Methods.Add(valueMethod);

        // Create a method body for the 'Value' method
        CilInstructionCollection invokeInstructions = valueMethod.CreateAndBindCilMethodBody().Instructions;

        _ = invokeInstructions.Add(CilOpCodes.Ldc_I4_0);
        _ = invokeInstructions.Add(CilOpCodes.Ret);

        return implType;
    }

    /// <summary>
    /// Creates types to use to declare RVA fields.
    /// </summary>
    /// <param name="referenceImporter">The <see cref="ReferenceImporter"/> instance to use.</param>
    /// <param name="rvaFieldsType">The containing type for all RVA fields.</param>
    /// <param name="iidRvaDataType">The type to use for IID RVA fields.</param>
    public static void RvaFieldsTypes(
        ReferenceImporter referenceImporter,
        out TypeDefinition rvaFieldsType,
        out TypeDefinition iidRvaDataType)
    {
        // Define the special '<RvaFields>' type, to contain all RVA fields
        rvaFieldsType = new(
            ns: null,
            name: "<RvaFields>"u8,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // Define the data type for IID data
        iidRvaDataType = new(
            ns: null,
            name: "IIDRvaDataSize=16",
            attributes: TypeAttributes.NestedAssembly | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            baseType: referenceImporter.ImportType(typeof(ValueType)))
        {
            ClassLayout = new ClassLayout(packingSize: 1, classSize: 16)
        };

        // The IID RVA type is nested under the '<RvaFields>' type
        rvaFieldsType.NestedTypes.Add(iidRvaDataType);
    }
}

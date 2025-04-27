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
    /// <param name="owningModule">The module that will contain the type being created.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateInterfaceEntriesImplType(
        TypeSignature delegateType,
        TypeDefinition delegateInterfaceEntriesType,
        TypeDefinition delegateImplType,
        ModuleDefinition owningModule)
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
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(owningModule) }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the interface entries
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(owningModule);

        // Resolve 'ComInterfaceEntry', so we can set its fields:
        //   - [0]: Guid IID
        //   - [1]: nint Vtable
        TypeDefinition comInterfaceEntryType = owningModule.MetadataResolver.ResolveType(owningModule.DefaultImporter.ImportType(typeof(ComWrappers.ComInterfaceEntry)))!;

        // Import the target fields (they have to be in the module, or the resulting assembly won't be valid)
        IFieldDescriptor comInterfaceEntryIIDField = owningModule.DefaultImporter.ImportField(comInterfaceEntryType.Fields[0]);
        IFieldDescriptor comInterfaceEntryVtableField = owningModule.DefaultImporter.ImportField(comInterfaceEntryType.Fields[1]);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection instructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the COM interface entries, doing this for each entry:
        //
        // Entries.<FIELD#i>.IID = <INTERFACE>Impl.IID;
        // Entries.<FIELD#i>.Vtable = <INTERFACE>Impl.Vtable;
        //
        // For the two generated 'Impl' types, we just directly store the address of the RVA vtable data.
        _ = instructions.Add(CilOpCodes.Ldsflda, entriesField);
        _ = instructions.Add(CilOpCodes.Ldflda, delegateInterfaceEntriesType.Fields[0]);
        _ = instructions.Add(CilOpCodes.Call, delegateImplType.Properties[0].GetMethod!);
        _ = instructions.Add(CilOpCodes.Ldobj, owningModule.DefaultImporter.ImportType(typeof(Guid)));
        _ = instructions.Add(CilOpCodes.Stfld, comInterfaceEntryIIDField);
        _ = instructions.Add(CilOpCodes.Ldsflda, entriesField);
        _ = instructions.Add(CilOpCodes.Ldflda, delegateInterfaceEntriesType.Fields[0]);
        _ = instructions.Add(CilOpCodes.Call, delegateImplType.Properties[1].GetMethod!);
        _ = instructions.Add(CilOpCodes.Stfld, comInterfaceEntryVtableField);
        _ = instructions.Add(CilOpCodes.Ret);

        // The 'Vtables' property type has the signature being 'ComWrappers.ComInterfaceEntry*'
        PointerTypeSignature vtablesPropertyType = owningModule.DefaultImporter
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
    /// <param name="delegateVfbtlType">The <see cref="TypeDefinition"/> instance returned by <see cref="WellKnownTypeDefinitionFactory.DelegateVftbl"/>.</param>
    /// <param name="iidRvaDataType">The type to use for IID RVA fields.</param>
    /// <param name="owningModule">The module that will contain the type being created.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public static TypeDefinition DelegateImplType(
        TypeSignature delegateType,
        TypeDefinition delegateVfbtlType,
        TypeDefinition iidRvaDataType,
        ModuleDefinition owningModule,
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
        //
        // The '[FixedAddressValueType]' attribute allows ILC to pre-initialize the entire vtable (in .rdata).
        FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, delegateVfbtlType.ToTypeSignature())
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(owningModule) }
        };

        implType.Fields.Add(vftblField);

        // Create the field for the IID for the delegate type
        iidRvaField = new FieldDefinition(
            name: InteropUtf8NameFactory.TypeName(delegateType, "IID"),
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: iidRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(Guid.NewGuid().ToByteArray())
        };

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(owningModule);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the delegate vtable
        // TODO
        _ = cctorInstructions.Add(CilOpCodes.Ret);

        // The 'IID' property type has the signature being 'Guid& modreq(InAttribute)'
        CustomModifierTypeSignature iidPropertyType = owningModule.DefaultImporter
            .ImportType(typeof(Guid))
            .MakeByReferenceType()
            .MakeModifierType(owningModule.DefaultImporter.ImportType(typeof(InAttribute)), isRequired: true);

        // The 'IID' property has the signature being 'Guid& modreq(InAttribute)'
        PropertySignature iidPropertySignature = new(CallingConventionAttributes.Property, iidPropertyType, []);

        // Create the 'IID' property
        PropertyDefinition iidProperty = new("IID"u8, PropertyAttributes.None, iidPropertySignature)
        {
            CustomAttributes = { InteropCustomAttributeFactory.IsReadOnly(owningModule) },
            GetMethod = new MethodDefinition(
                name: "get_IID"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: new MethodSignature(
                    attributes: CallingConventionAttributes.Default,
                    returnType: iidPropertyType,
                    parameterTypes: []))
            { IsAggressiveInlining = true }
        };

        implType.Properties.Add(iidProperty);
        implType.Methods.Add(iidProperty.GetMethod!);

        // Create a method body for the 'IID' property
        CilInstructionCollection get_IIDInstructions = iidProperty.GetMethod!.CreateAndBindCilMethodBody().Instructions;

        // The 'get_IID' method directly returns the IID RVA field address
        _ = get_IIDInstructions.Add(CilOpCodes.Ldsflda, iidRvaField);
        _ = get_IIDInstructions.Add(CilOpCodes.Ret);

        // The 'Vtable' property has the signature being just 'nint'
        PropertySignature vtablePropertySignature = new(CallingConventionAttributes.Property, owningModule.CorLibTypeFactory.IntPtr, []);

        // Create the 'Vtable' property
        PropertyDefinition vtableProperty = new("Vtable"u8, PropertyAttributes.None, vtablePropertySignature)
        {
            GetMethod = new MethodDefinition(
                name: "get_Vtable"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: new MethodSignature(
                    attributes: CallingConventionAttributes.Default,
                    returnType: owningModule.CorLibTypeFactory.IntPtr,
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
                returnType: owningModule.CorLibTypeFactory.Int32,
                parameterTypes: [
                    owningModule.CorLibTypeFactory.Void.MakePointerType(),
                    owningModule.CorLibTypeFactory.Void.MakePointerType(),
                    owningModule.CorLibTypeFactory.Void.MakePointerType()]))
        {
            CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(owningModule) }
        };

        implType.Methods.Add(invokeMethod);

        // Create a method body for the 'Invoke' method
        CilInstructionCollection invokeInstructions = invokeMethod.CreateAndBindCilMethodBody().Instructions;

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

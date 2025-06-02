// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for interop type definitions.
/// </summary>
internal static partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Creates an 'IID' property with the specified parameters.
    /// </summary>
    /// <param name="name">The property and field name.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iid">The <see cref="Guid"/> value to use for the RVA field.</param>
    /// <param name="get_IidMethod">The resulting 'get_IID' method.</param>
    private static void IID(
        Utf8String name,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module,
        in Guid iid,
        out MethodDefinition get_IidMethod)
    {
        WellKnownMemberDefinitionFactory.IID(
            propertyName: name,
            iidRvaFieldName: name,
            iidRvaDataType: interopDefinitions.IIDRvaDataSize_16,
            interopReferences: interopReferences,
            module: module,
            iid: in iid,
            out FieldDefinition iidRvaField,
            out get_IidMethod,
            out PropertyDefinition iidProperty);

        interopDefinitions.RvaFields.Fields.Add(iidRvaField);
        interopDefinitions.InterfaceIIDs.Methods.Add(get_IidMethod);
        interopDefinitions.InterfaceIIDs.Properties.Add(iidProperty);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the COM interface entries for a managed type.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="entriesFieldType">The <see cref="TypeDefinition"/> for the type of entries field.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="implTypes">The set of vtable accessors to use for each entry.</param>
    private static void InterfaceEntriesImplType(
        Utf8String ns,
        Utf8String name,
        TypeDefinition entriesFieldType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition implType,
        params ReadOnlySpan<(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable)> implTypes)
    {
        // We're declaring an 'internal static class' type
        implType = new TypeDefinition(
            ns: ns,
            name: name,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(implType);

        // The interface entries field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateInterfaceEntries> Entries;
        //
        // The '[FixedAddressValueType]' attribute allows ILC to pre-initialize the entire vtable (in .rdata).
        FieldDefinition entriesField = new("Entries"u8, FieldAttributes.Private, entriesFieldType.ToTypeSignature(isValueType: true))
        {
            CustomAttributes = { new CustomAttribute(interopReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the interface entries
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // Import the target fields (they have to be in the module, or the resulting assembly won't be valid):
        //   - [0]: Guid IID
        //   - [1]: nint Vtable
        IFieldDescriptor comInterfaceEntryIIDField = interopReferences.ComInterfaceEntryIID.Import(module);
        IFieldDescriptor comInterfaceEntryVtableField = interopReferences.ComInterfaceEntryVtable.Import(module);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the COM interface entries, doing this for each entry:
        //
        // Entries.<FIELD#i>.IID = <INTERFACE>Impl.IID;
        // Entries.<FIELD#i>.Vtable = <INTERFACE>Impl.Vtable;
        //
        // Each 'Impl' types is assumed to always have the 'IID' and 'Vtable' properties, in this order.
        for (int i = 0; i < implTypes.Length; i++)
        {
            _ = cctorInstructions.Add(Ldsflda, entriesField);
            _ = cctorInstructions.Add(Ldflda, entriesFieldType.Fields[i]);
            _ = cctorInstructions.Add(Call, implTypes[i].get_IID.Import(module));
            _ = cctorInstructions.Add(Ldobj, interopReferences.Guid.Import(module));
            _ = cctorInstructions.Add(Stfld, comInterfaceEntryIIDField);
            _ = cctorInstructions.Add(Ldsflda, entriesField);
            _ = cctorInstructions.Add(Ldflda, entriesFieldType.Fields[i]);
            _ = cctorInstructions.Add(Call, implTypes[i].get_Vtable.Import(module));
            _ = cctorInstructions.Add(Stfld, comInterfaceEntryVtableField);
        }

        _ = cctorInstructions.Add(Ret);

        // The 'Vtables' property type has the signature being 'ComWrappers.ComInterfaceEntry*'
        PointerTypeSignature vtablesPropertyType = interopReferences.ComInterfaceEntry.Import(module).MakePointerType();

        // Create the 'Vtables' property
        PropertyDefinition vtablesProperty = new(
            name: "Vtables"u8,
            attributes: PropertyAttributes.None,
            signature: PropertySignature.CreateStatic(vtablesPropertyType))
        {
            GetMethod = new MethodDefinition(
                name: "get_Vtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: vtablesPropertyType,
                    parameterTypes: []))
            { IsAggressiveInlining = true }
        };

        implType.Properties.Add(vtablesProperty);
        implType.Methods.Add(vtablesProperty.GetMethod!);

        // Create a method body for the 'Vtables' property (it directly returns the 'Entries' field address)
        vtablesProperty.GetMethod!.CilMethodBody = new CilMethodBody(vtablesProperty.GetMethod!)
        {
            Instructions =
            {
                { Ldsflda, entriesField },
                { Conv_U },
                { Ret }
            }
        };
    }

    /// <summary>
    /// Creates a new type definition for the proxy type of some managed type.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="runtimeClassName">The runtime class name for the managed type.</param>
    /// <param name="comWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance for the marshaller attribute type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="marshallerType">The resulting proxy type.</param>
    public static void ProxyType(
        Utf8String ns,
        Utf8String name,
        string runtimeClassName,
        TypeDefinition comWrappersMarshallerAttributeType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition marshallerType)
    {
        // We're declaring an 'internal static class' type
        marshallerType = new(
            ns: ns,
            name: name,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(marshallerType);

        // Get the constructor for '[WindowsRuntimeClassName]'
        MemberReference windowsRuntimeClassNameAttributeCtor = interopReferences.WindowsRuntimeClassNameAttribute
            .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
                returnType: module.CorLibTypeFactory.Void,
                parameterTypes: [module.CorLibTypeFactory.String]))
            .Import(module);

        // Add the attribute with the name of the runtime class
        marshallerType.CustomAttributes.Add(new CustomAttribute(
            constructor: windowsRuntimeClassNameAttributeCtor,
            signature: new CustomAttributeSignature(new CustomAttributeArgument(
                argumentType: module.CorLibTypeFactory.String,
                value: runtimeClassName))));

        // Add the generated marshaller attribute
        marshallerType.CustomAttributes.Add(new CustomAttribute(comWrappersMarshallerAttributeType.GetConstructor()!));
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using System;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropGenerator.Factories;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for interop type definitions.
/// </summary>
internal static partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Creates a new type definition for the implementation of the COM interface entries for a managed type.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="entriesFieldType">The <see cref="TypeDefinition"/> for the type of entries field.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="implTypes">The set of vtable accessors to use for each entry.</param>
    private static void InterfaceEntriesImplType(
        Utf8String ns,
        Utf8String name,
        TypeDefinition entriesFieldType,
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
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the interface entries
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // Resolve 'ComInterfaceEntry', so we can set its fields:
        //   - [0]: Guid IID
        //   - [1]: nint Vtable
        TypeDefinition comInterfaceEntryType = module.MetadataResolver.ResolveType(module.DefaultImporter.ImportType(typeof(ComWrappers.ComInterfaceEntry)))!;

        // Import the target fields (they have to be in the module, or the resulting assembly won't be valid)
        IFieldDescriptor comInterfaceEntryIIDField = module.DefaultImporter.ImportField(comInterfaceEntryType.Fields[0]);
        IFieldDescriptor comInterfaceEntryVtableField = module.DefaultImporter.ImportField(comInterfaceEntryType.Fields[1]);

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
            _ = cctorInstructions.Add(Call, (IMethodDefOrRef)implTypes[i].get_IID.ImportWith(module.DefaultImporter));
            _ = cctorInstructions.Add(Ldobj, module.DefaultImporter.ImportType(typeof(Guid)));
            _ = cctorInstructions.Add(Stfld, comInterfaceEntryIIDField);
            _ = cctorInstructions.Add(Ldsflda, entriesField);
            _ = cctorInstructions.Add(Ldflda, entriesFieldType.Fields[i]);
            _ = cctorInstructions.Add(Call, (IMethodDefOrRef)implTypes[i].get_Vtable.ImportWith(module.DefaultImporter));
            _ = cctorInstructions.Add(Stfld, comInterfaceEntryVtableField);
        }

        _ = cctorInstructions.Add(Ret);

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
}

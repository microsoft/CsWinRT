// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop method bodies.
/// </summary>
internal static class InteropMethodBodyFactory
{
    /// <summary>
    /// Creates the body of a static constructor that initializes the COM interface entries for a given type.
    /// </summary>
    /// <param name="cctor">The <see cref="MethodDefinition"/> for the static constructor.</param>
    /// <param name="entriesField">The <see cref="FieldDefinition"/> for the COM interface entries.</param>
    /// <param name="entriesFieldType">The <see cref="TypeDefinition"/> for the type of <paramref name="entriesField"/>.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implTypes">The types to use to extracr COM interface entry values.</param>
    public static void InterfaceEntriesImpl(
        MethodDefinition cctor,
        FieldDefinition entriesField,
        TypeDefinition entriesFieldType,
        ModuleDefinition module,
        params ReadOnlySpan<TypeDefinition> implTypes)
    {
        // Resolve 'ComInterfaceEntry', so we can set its fields:
        //   - [0]: Guid IID
        //   - [1]: nint Vtable
        TypeDefinition comInterfaceEntryType = module.MetadataResolver.ResolveType(module.DefaultImporter.ImportType(typeof(ComWrappers.ComInterfaceEntry)))!;

        // Import the target fields (they have to be in the module, or the resulting assembly won't be valid)
        IFieldDescriptor comInterfaceEntryIIDField = module.DefaultImporter.ImportField(comInterfaceEntryType.Fields[0]);
        IFieldDescriptor comInterfaceEntryVtableField = module.DefaultImporter.ImportField(comInterfaceEntryType.Fields[1]);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection instructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the COM interface entries, doing this for each entry:
        //
        // Entries.<FIELD#i>.IID = <INTERFACE>Impl.IID;
        // Entries.<FIELD#i>.Vtable = <INTERFACE>Impl.Vtable;
        //
        // Each 'Impl' types is assumed to always have the 'IID' and 'Vtable' properties, in this order.
        for (int i = 0; i < implTypes.Length; i++)
        {
            _ = instructions.Add(CilOpCodes.Ldsflda, entriesField);
            _ = instructions.Add(CilOpCodes.Ldflda, entriesFieldType.Fields[0]);
            _ = instructions.Add(CilOpCodes.Call, implTypes[i].Properties[0].GetMethod!.ImportWith(module.DefaultImporter));
            _ = instructions.Add(CilOpCodes.Ldobj, module.DefaultImporter.ImportType(typeof(Guid)));
            _ = instructions.Add(CilOpCodes.Stfld, comInterfaceEntryIIDField);
            _ = instructions.Add(CilOpCodes.Ldsflda, entriesField);
            _ = instructions.Add(CilOpCodes.Ldflda, entriesFieldType.Fields[0]);
            _ = instructions.Add(CilOpCodes.Call, implTypes[i].Properties[1].GetMethod!.ImportWith(module.DefaultImporter));
            _ = instructions.Add(CilOpCodes.Stfld, comInterfaceEntryVtableField);
        }

        _ = instructions.Add(CilOpCodes.Ret);
    }
}

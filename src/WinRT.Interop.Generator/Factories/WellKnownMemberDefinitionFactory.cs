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
/// A factory for well known member definitions.
/// </summary>
internal static class WellKnownMemberDefinitionFactory
{
    /// <summary>
    /// Creates the 'IID' property with the specified parameters.
    /// </summary>
    /// <param name="iidRvaFieldName">The name to use for <paramref name="iidRvaField"/>.</param>
    /// <param name="iidRvaDataType">The type to use for IID RVA fields.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iid">The <see cref="Guid"/> value to use for the RVA field.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    /// <param name="iidProperty">The resulting 'IID' property.</param>
    /// <param name="get_IidMethod">The resulting 'IID' getter method.</param>
    public static void IID(
        Utf8String iidRvaFieldName,
        TypeDefinition iidRvaDataType,
        ModuleDefinition module,
        in Guid iid,
        out FieldDefinition iidRvaField,
        out PropertyDefinition iidProperty,
        out MethodDefinition get_IidMethod)
    {
        // Create the field for the IID for the delegate type
        iidRvaField = new FieldDefinition(
            name: iidRvaFieldName,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: iidRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(iid.ToByteArray())
        };

        // The 'IID' property type has the signature being 'Guid& modreq(InAttribute)'
        CustomModifierTypeSignature iidPropertyType = WellKnownTypeSignatureFactory.InGuid(module.DefaultImporter);

        // The 'IID' property has the signature being 'Guid& modreq(InAttribute)'
        PropertySignature iidPropertySignature = new(CallingConventionAttributes.Property, iidPropertyType, []);

        // Create the 'get_IID' getter method
        get_IidMethod = new MethodDefinition(
            name: "get_IID"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: iidPropertyType,
                parameterTypes: []))
        { IsAggressiveInlining = true };

        // Create the 'IID' property
        iidProperty = new PropertyDefinition("IID"u8, PropertyAttributes.None, iidPropertySignature)
        {
            CustomAttributes = { InteropCustomAttributeFactory.IsReadOnly(module) },
            GetMethod = get_IidMethod
        };

        // Create a method body for the 'IID' property
        CilInstructionCollection get_IIDInstructions = get_IidMethod.CreateAndBindCilMethodBody().Instructions;

        // The 'get_IID' method directly returns the IID RVA field address
        _ = get_IIDInstructions.Add(CilOpCodes.Ldsflda, iidRvaField);
        _ = get_IIDInstructions.Add(CilOpCodes.Ret);
    }

    /// <summary>
    /// Creates the 'Vtable' property with the specified parameters.
    /// </summary>
    /// <param name="vftblField">The target vtable field to access.</param>
    /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
    /// <param name="vtableProperty">The resulting 'IID' property.</param>
    /// <param name="get_VtableMethod">The resulting 'IID' getter method.</param>
    public static void Vtable(
        FieldDefinition vftblField,
        CorLibTypeFactory corLibTypeFactory,
        out PropertyDefinition vtableProperty,
        out MethodDefinition get_VtableMethod)
    {
        // The 'Vtable' property has the signature being just 'nint'
        PropertySignature vtablePropertySignature = new(CallingConventionAttributes.Property, corLibTypeFactory.IntPtr, []);

        // Create the 'get_Vtable' getter method
        get_VtableMethod = new MethodDefinition(
            name: "get_Vtable"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: corLibTypeFactory.IntPtr,
                parameterTypes: []))
        { IsAggressiveInlining = true };

        // Create the 'Vtable' property
        vtableProperty = new PropertyDefinition("Vtable"u8, PropertyAttributes.None, vtablePropertySignature) { GetMethod = get_VtableMethod };

        // Create a method body for the 'Vtable' property
        CilInstructionCollection get_VtableInstructions = vtableProperty.GetMethod!.CreateAndBindCilMethodBody().Instructions;

        // The 'get_Vtable' method directly returns the 'Vftbl' field address
        _ = get_VtableInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = get_VtableInstructions.Add(CilOpCodes.Conv_U);
        _ = get_VtableInstructions.Add(CilOpCodes.Ret);
    }
}

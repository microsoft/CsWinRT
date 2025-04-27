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
    /// <param name="get_iidMethod">The resulting 'IID' getter method.</param>
    public static void IID(
        Utf8String iidRvaFieldName,
        TypeDefinition iidRvaDataType,
        ModuleDefinition module,
        in Guid iid,
        out FieldDefinition iidRvaField,
        out PropertyDefinition iidProperty,
        out MethodDefinition get_iidMethod)
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
        CustomModifierTypeSignature iidPropertyType = module.DefaultImporter
            .ImportType(typeof(Guid))
            .MakeByReferenceType()
            .MakeModifierType(module.DefaultImporter.ImportType(typeof(InAttribute)), isRequired: true);

        // The 'IID' property has the signature being 'Guid& modreq(InAttribute)'
        PropertySignature iidPropertySignature = new(CallingConventionAttributes.Property, iidPropertyType, []);

        // Create the 'get_IID' getter method
        get_iidMethod = new MethodDefinition(
            name: "get_IID"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: iidPropertyType,
                parameterTypes: []))
        { IsAggressiveInlining = true };

        // Create the 'IID' property
        iidProperty = new("IID"u8, PropertyAttributes.None, iidPropertySignature)
        {
            CustomAttributes = { InteropCustomAttributeFactory.IsReadOnly(module) },
            GetMethod = get_iidMethod
        };

        // Create a method body for the 'IID' property
        CilInstructionCollection get_IIDInstructions = get_iidMethod.CreateAndBindCilMethodBody().Instructions;

        // The 'get_IID' method directly returns the IID RVA field address
        _ = get_IIDInstructions.Add(CilOpCodes.Ldsflda, iidRvaField);
        _ = get_IIDInstructions.Add(CilOpCodes.Ret);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known member definitions.
/// </summary>
internal static class WellKnownMemberDefinitionFactory
{
    /// <summary>
    /// Creates an 'IID' property with the specified parameters (and optionally a specific name).
    /// </summary>
    /// <param name="propertyName">The name to use for <paramref name="iidProperty"/> (will default to 'IID' if <see langword="null"/>).</param>
    /// <param name="iidRvaFieldName">The name to use for <paramref name="iidRvaField"/>.</param>
    /// <param name="iidRvaDataType">The type to use for IID RVA fields.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iid">The <see cref="Guid"/> value to use for the RVA field.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    /// <param name="get_IidMethod">The resulting 'IID' getter method.</param>
    /// <param name="iidProperty">The resulting 'IID' property.</param>
    public static void IID(
        Utf8String? propertyName,
        Utf8String iidRvaFieldName,
        TypeDefinition iidRvaDataType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        in Guid iid,
        out FieldDefinition iidRvaField,
        out MethodDefinition get_IidMethod,
        out PropertyDefinition iidProperty)
    {
        // Create the field for the IID
        iidRvaField = new FieldDefinition(
            name: iidRvaFieldName,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: iidRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(iid.ToByteArray())
        };

        // The 'IID' property type has the signature being 'Guid& modreq(InAttribute)'
        TypeSignature iidPropertyType = WellKnownTypeSignatureFactory.InGuid(interopReferences).Import(module);

        // Select the property and accessor name based on 'propertyName'
        propertyName = propertyName is null
            ? "IID"u8
            : $"IID_{propertyName}";

        // Create the 'get_IID' getter method
        get_IidMethod = new MethodDefinition(
            name: "get_"u8 + propertyName,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(iidPropertyType))
        {
            IsAggressiveInlining = true,
            CilInstructions =
            {
                { Ldsflda, iidRvaField },
                { Ret }
            }
        };

        // Create the 'IID' property
        iidProperty = new PropertyDefinition(
            name: propertyName,
            attributes: PropertyAttributes.None,
            signature: PropertySignature.FromGetMethod(get_IidMethod))
        {
            CustomAttributes = { new CustomAttribute(interopReferences.IsReadOnlyAttribute_ctor.Import(module)) },
            GetMethod = get_IidMethod
        };
    }

    /// <summary>
    /// Creates the 'IID' property that forwards to another property.
    /// </summary>
    /// <param name="forwardedIidMethod">The <see cref="MethodDefinition"/> to forward calls to.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="get_IidMethod">The resulting 'IID' getter method.</param>
    /// <param name="iidProperty">The resulting 'IID' property.</param>
    public static void IID(
        MethodDefinition forwardedIidMethod,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out MethodDefinition get_IidMethod,
        out PropertyDefinition iidProperty)
    {
        // The 'IID' property type has the signature being 'Guid& modreq(InAttribute)'
        TypeSignature iidPropertyType = WellKnownTypeSignatureFactory.InGuid(interopReferences).Import(module);

        // Create the 'get_IID' getter method
        get_IidMethod = new MethodDefinition(
            name: "get_IID"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(iidPropertyType))
        {
            IsAggressiveInlining = true,
            CilInstructions =
            {
                { Call, forwardedIidMethod },
                { Ret }
            }
        };

        // Create the 'IID' property
        iidProperty = new PropertyDefinition(
            name: "IID"u8,
            attributes: PropertyAttributes.None,
            signature: PropertySignature.FromGetMethod(get_IidMethod))
        {
            CustomAttributes = { new CustomAttribute(interopReferences.IsReadOnlyAttribute_ctor.Import(module)) },
            GetMethod = get_IidMethod
        };
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
        // Create the 'get_Vtable' getter method
        get_VtableMethod = new MethodDefinition(
            name: "get_Vtable"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(corLibTypeFactory.IntPtr))
        {
            IsAggressiveInlining = true,
            CilInstructions =
            {
                { Ldsflda, vftblField },
                { Conv_U },
                { Ret }
            }
        };

        // Create the 'Vtable' property (the signature is just 'nint')
        vtableProperty = new PropertyDefinition(
            name: "Vtable"u8,
            attributes: PropertyAttributes.None,
            signature: PropertySignature.FromGetMethod(get_VtableMethod))
        { GetMethod = get_VtableMethod };
    }
}

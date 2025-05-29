// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
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
    /// Creates the 'IID' property with the specified parameters.
    /// </summary>
    /// <param name="iidRvaFieldName">The name to use for <paramref name="iidRvaField"/>.</param>
    /// <param name="iidRvaDataType">The type to use for IID RVA fields.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iid">The <see cref="Guid"/> value to use for the RVA field.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    /// <param name="iidProperty">The resulting 'IID' property.</param>
    /// <param name="get_IidMethod">The resulting 'IID' getter method.</param>
    public static void IID(
        Utf8String iidRvaFieldName,
        TypeDefinition iidRvaDataType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        in Guid iid,
        out FieldDefinition iidRvaField,
        out PropertyDefinition iidProperty,
        out MethodDefinition get_IidMethod)
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

        // The 'IID' property has the signature being 'Guid& modreq(InAttribute)'
        PropertySignature iidPropertySignature = new(CallingConventionAttributes.Property, iidPropertyType, []);

        // Create the 'get_IID' getter method
        get_IidMethod = new MethodDefinition(
            name: "get_IID"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(iidPropertyType))
        { IsAggressiveInlining = true };

        // Create the 'IID' property
        iidProperty = new PropertyDefinition("IID"u8, PropertyAttributes.None, iidPropertySignature)
        {
            CustomAttributes = { new CustomAttribute(interopReferences.IsReadOnlyAttribute_ctor.Import(module)) },
            GetMethod = get_IidMethod
        };

        // Create a method body for the 'IID' property
        get_IidMethod.CilMethodBody = new CilMethodBody(get_IidMethod)
        {
            Instructions =
            {
                { Ldsflda, iidRvaField },
                { Ret }
            }
        };
    }

    /// <summary>
    /// Creates the 'IID' property that forwards to another property.
    /// </summary>
    /// <param name="forwardedIidMethod">The <see cref="MethodDefinition"/> to forward calls to.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iidProperty">The resulting 'IID' property.</param>
    /// <param name="get_IidMethod">The resulting 'IID' getter method.</param>
    public static void IID(
        MethodDefinition forwardedIidMethod,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out PropertyDefinition iidProperty,
        out MethodDefinition get_IidMethod)
    {
        // The 'IID' property type has the signature being 'Guid& modreq(InAttribute)'
        TypeSignature iidPropertyType = WellKnownTypeSignatureFactory.InGuid(interopReferences).Import(module);

        // The 'IID' property has the signature being 'Guid& modreq(InAttribute)'
        PropertySignature iidPropertySignature = new(CallingConventionAttributes.Property, iidPropertyType, []);

        // Create the 'get_IID' getter method
        get_IidMethod = new MethodDefinition(
            name: "get_IID"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(iidPropertyType))
        { IsAggressiveInlining = true };

        // Create the 'IID' property
        iidProperty = new PropertyDefinition("IID"u8, PropertyAttributes.None, iidPropertySignature)
        {
            CustomAttributes = { new CustomAttribute(interopReferences.IsReadOnlyAttribute_ctor.Import(module)) },
            GetMethod = get_IidMethod
        };

        // Create a method body for the 'IID' property
        get_IidMethod.CilMethodBody = new CilMethodBody(get_IidMethod)
        {
            Instructions =
            {
                { Call, forwardedIidMethod },
                { Ret }
            }
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
        // The 'Vtable' property has the signature being just 'nint'
        PropertySignature vtablePropertySignature = new(CallingConventionAttributes.Property, corLibTypeFactory.IntPtr, []);

        // Create the 'get_Vtable' getter method
        get_VtableMethod = new MethodDefinition(
            name: "get_Vtable"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(corLibTypeFactory.IntPtr))
        { IsAggressiveInlining = true };

        // Create the 'Vtable' property
        vtableProperty = new PropertyDefinition("Vtable"u8, PropertyAttributes.None, vtablePropertySignature) { GetMethod = get_VtableMethod };

        // Create a method body for the 'Vtable' property
        vtableProperty.GetMethod.CilMethodBody = new CilMethodBody(vtableProperty.GetMethod)
        {
            Instructions =
            {
                { Ldsflda, vftblField },
                { Conv_U },
                { Ret }
            }
        };
    }

    /// <summary>
    /// Creates the 'ComputeReadOnlySpanHash' method.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    public static MethodDefinition ComputeReadOnlySpanHash(InteropReferences interopReferences, ModuleDefinition module)
    {
        // Create the 'ComputeReadOnlySpanHash' getter method
        MethodDefinition hashMethod = new(
            name: "ComputeReadOnlySpanHash"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(
                returnType: module.CorLibTypeFactory.Int32,
                parameterTypes: [interopReferences.ReadOnlySpanChar.Import(module).ToTypeSignature(isValueType: true)]));

        // Jump labels
        CilInstruction ldloc_1_rangeCheck = new(Ldloc_1);
        CilInstruction ldarga_S_0_loopStart = new(Ldarga_S, hashMethod.Parameters[0]);

        // Define the locals (hash value, and loop index)
        CilLocalVariable loc_0_hash = new(module.CorLibTypeFactory.UInt32);
        CilLocalVariable loc_1_index = new(module.CorLibTypeFactory.Int32);

        // Create a method body for the 'ComputeReadOnlySpanHash' method
        hashMethod.CilMethodBody = new CilMethodBody(hashMethod)
        {
            LocalVariables = { loc_0_hash, loc_1_index },
            Instructions =
            {
                // This method copies the simple hash implementation that Roslyn emits.
                // To verify that source, just inspect the code generated for a method
                // with at least a dozen 'string'-s in a big switch statement.
                { CilInstruction.CreateLdcI4(unchecked((int)2166136261u)) },
                { Stloc_0 },
                { Ldc_I4_0 },
                { Stloc_1 },
                { Br_S, ldloc_1_rangeCheck.CreateLabel() },

                // Loop
                { ldarga_S_0_loopStart },
                { Ldloc_1 },
                { Call, interopReferences.ReadOnlySpanCharget_Item.Import(module) },
                { Ldind_U2 },
                { Ldloc_0 },
                { Xor },
                { CilInstruction.CreateLdcI4(16777619) },
                { Mul },
                { Stloc_0 },
                { Ldloc_1 },
                { Ldc_I4_1 },
                { Add },
                { Stloc_1 },

                // Loop range check
                { ldloc_1_rangeCheck },
                { Ldarga_S, hashMethod.Parameters[0] },
                { Call, interopReferences.ReadOnlySpanCharget_Length.Import(module) },
                { Blt_S, ldarga_S_0_loopStart.CreateLabel() },

                // Return the hash
                { Ldloc_0 },
                { Ret }
            }
        };

        return hashMethod;
    }
}

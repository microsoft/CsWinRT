﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="WellKnownTypeDefinitionFactory"/>
internal partial class WellKnownTypeDefinitionFactory
{
    /// <summary>
    /// Creates the <c>IgnoreAccessChecksToAttribute</c> type.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <returns>The resulting <c>IgnoreAccessChecksToAttribute</c> type.</returns>
    public static TypeDefinition IgnoreAccessChecksToAttribute(InteropReferences interopReferences, ModuleDefinition module)
    {
        // We're declaring a 'public sealed class' type
        TypeDefinition ignoreAccessChecksToType = new(
            ns: "System.Runtime.CompilerServices"u8,
            name: "IgnoreAccessChecksToAttribute"u8,
            attributes: TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.Attribute.Import(module));

        // Add the '_assemblyName' field
        FieldDefinition assemblyNameField = new(
            name: "_assemblyName"u8,
            attributes: FieldAttributes.Private | FieldAttributes.InitOnly,
            fieldType: module.CorLibTypeFactory.String);

        ignoreAccessChecksToType.Fields.Add(assemblyNameField);

        // Define the constructor
        MethodDefinition ctor = MethodDefinition.CreateConstructor(module, module.CorLibTypeFactory.String);

        ignoreAccessChecksToType.Methods.Add(ctor);

        _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
        _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
        _ = ctor.CilMethodBody!.Instructions.Insert(2, Stfld, assemblyNameField);
        _ = ctor.CilMethodBody!.Instructions.Insert(3, Ldarg_0);
        _ = ctor.CilMethodBody!.Instructions.Insert(4, Call, interopReferences.Attribute_ctor.Import(module));

        // Create the 'get_AssemblyName' getter method
        MethodDefinition get_AssemblyNameMethod = new(
            name: "get_AssemblyName"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.String));

        // Create the 'AssemblyName' property
        PropertyDefinition assemblyNameProperty = new(
            name: "AssemblyName"u8,
            attributes: PropertyAttributes.None,
            signature: PropertySignature.FromGetMethod(get_AssemblyNameMethod))
        {
            GetMethod = get_AssemblyNameMethod
        };

        ignoreAccessChecksToType.Properties.Add(assemblyNameProperty);
        ignoreAccessChecksToType.Methods.Add(get_AssemblyNameMethod);

        // Create a method body for the 'AssemblyName' property
        get_AssemblyNameMethod.CilMethodBody = new CilMethodBody()
        {
            Instructions =
            {
                { Ldarg_0 },
                { Ldfld, assemblyNameField },
                { Ret }
            }
        };

        // Also emit '[AttributeUsage]' on the type
        ignoreAccessChecksToType.CustomAttributes.Add(InteropCustomAttributeFactory.AttributeUsage(
            attributeTargets: AttributeTargets.Assembly,
            allowMultiple: true,
            interopReferences: interopReferences,
            module: module));

        return ignoreAccessChecksToType;
    }
}

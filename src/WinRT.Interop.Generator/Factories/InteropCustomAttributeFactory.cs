// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop custom attributes.
/// </summary>
internal static class InteropCustomAttributeFactory
{
    /// <summary>
    /// Creates a new custom attribute value for <see cref="UnmanagedCallersOnlyAttribute"/> (and imports all metadata elements for it).
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    /// <remarks>The attribute will specify the <see cref="CallConvMemberFunction"/> calling convention.</remarks>
    public static CustomAttribute UnmanagedCallersOnly(InteropReferences interopReferences, ModuleDefinition module)
    {
        // Get the 'Type[]' signature and reuse it (we need it for both the argument and the element)
        TypeSignature typeArraySignature = interopReferences.Type.Import(module).MakeSzArrayType();

        // Create the following attribute:
        //
        // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        return new(interopReferences.UnmanagedCallersOnlyAttribute_ctor.Import(module), new CustomAttributeSignature(
            fixedArguments: [],
            namedArguments: [new CustomAttributeNamedArgument(
                memberType: CustomAttributeArgumentMemberType.Field,
                memberName: "CallConvs"u8,
                argumentType: typeArraySignature,
                argument: new CustomAttributeArgument(
                    argumentType: typeArraySignature,
                    elements: interopReferences.CallConvMemberFunction.Import(module).ToReferenceTypeSignature()))]));
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="AttributeUsageAttribute"/> (and imports all metadata elements for it).
    /// </summary>
    /// <param name="attributeTargets">The <see cref="AttributeTargets"/> value to use.</param>
    /// <param name="allowMultiple">Whether to allow multiple uses of the attribute.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    public static CustomAttribute AttributeUsage(
        AttributeTargets attributeTargets,
        bool allowMultiple,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Create the following attribute:
        //
        // [AttributeUsage(<attributeTargets>, AllowMultiple = <allowMultiple>)]
        return new(interopReferences.AttributeUsageAttribute_ctor_AttributeTargets.Import(module), new CustomAttributeSignature(
            fixedArguments: [new CustomAttributeArgument(
                argumentType: interopReferences.AttributeTargets.Import(module).ToValueTypeSignature(),
                value: (int)attributeTargets)],
            namedArguments: [new CustomAttributeNamedArgument(
                memberType: CustomAttributeArgumentMemberType.Property,
                memberName: "AllowMultiple"u8,
                argumentType: module.CorLibTypeFactory.Boolean,
                argument: new CustomAttributeArgument(
                    argumentType: module.CorLibTypeFactory.Boolean,
                    value: allowMultiple))]));
    }

    /// <summary>
    /// Creates a new custom attribute value for <c>IgnoresAccessChecksToAttribute</c>.
    /// </summary>
    /// <param name="assemblyName">The target assemby name.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    public static CustomAttribute IgnoresAccessChecksTo(
        string assemblyName,
        InteropDefinitions interopDefinitions,
        ModuleDefinition module)
    {
        // Get the constructor taking 'assemblyName' as a string argument
        MethodDefinition ctor = interopDefinitions.IgnoresAccessChecksToAttribute.GetConstructor(module.CorLibTypeFactory.String)!;

        // Create the following attribute:
        //
        // [IgnoresAccessChecksTo(<assemblyName>)]
        return new(ctor, new CustomAttributeSignature(new CustomAttributeArgument(
            argumentType: module.CorLibTypeFactory.String,
            value: assemblyName)));
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> (and imports all metadata elements for it).
    /// </summary>
    /// <param name="value"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='value']/node()"/></param>
    /// <param name="target"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='target']/node()"/></param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    public static CustomAttribute TypeMapWindowsRuntimeComWrappersTypeMapGroup(
        string value,
        TypeSignature target,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Create the following attribute:
        //
        // [TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(<VALUE>, <TARGET>, <TRIM_TARGET>)]
        return new(interopReferences.TypeMapAttributeWindowsRuntimeComWrappersTypeMapGroup_ctor_TrimTarget.Import(module), new CustomAttributeSignature(
            fixedArguments: [
                new CustomAttributeArgument(
                    argumentType: module.CorLibTypeFactory.String,
                    value: value),
                new CustomAttributeArgument(
                    argumentType: interopReferences.Type.Import(module).ToReferenceTypeSignature(),
                    value: target.Import(module)),
                new CustomAttributeArgument(
                    argumentType: interopReferences.Type.Import(module).ToReferenceTypeSignature(),
                    value: trimTarget.Import(module))]));
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> (and imports all metadata elements for it).
    /// </summary>
    /// <param name="source"><inheritdoc cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)" path="/param[@name='source']/node()"/></param>
    /// <param name="proxy"><inheritdoc cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)" path="/param[@name='proxy']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    public static CustomAttribute TypeMapAssociationWindowsRuntimeComWrappersTypeMapGroup(
        TypeSignature source,
        TypeSignature proxy,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Create the following attribute:
        //
        // [TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(<SOURCE>, <PROXY>)]
        return new(interopReferences.TypeMapAssociationAttributeWindowsRuntimeComWrappersTypeMapGroup_ctor.Import(module), new CustomAttributeSignature(
            fixedArguments: [
                new CustomAttributeArgument(
                    argumentType: interopReferences.Type.Import(module).ToReferenceTypeSignature(),
                    value: source.Import(module)),
                new CustomAttributeArgument(
                    argumentType: interopReferences.Type.Import(module).ToReferenceTypeSignature(),
                    value: proxy.Import(module))]));
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> (and imports all metadata elements for it).
    /// </summary>
    /// <param name="source"><inheritdoc cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)" path="/param[@name='source']/node()"/></param>
    /// <param name="proxy"><inheritdoc cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)" path="/param[@name='proxy']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    public static CustomAttribute TypeMapAssociationDynamicInterfaceCastableImplementationTypeMapGroup(
        TypeSignature source,
        TypeSignature proxy,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Create the following attribute:
        //
        // [TypeMap<DynamicInterfaceCastableImplementationTypeMapGroup>(<SOURCE>, <PROXY>)]
        return new(interopReferences.TypeMapAssociationAttributeDynamicInterfaceCastableImplementationTypeMapGroup_ctor.Import(module), new CustomAttributeSignature(
            fixedArguments: [
                new CustomAttributeArgument(
                    argumentType: interopReferences.Type.Import(module).ToReferenceTypeSignature(),
                    value: source.Import(module)),
                new CustomAttributeArgument(
                    argumentType: interopReferences.Type.Import(module).ToReferenceTypeSignature(),
                    value: proxy.Import(module))]));
    }
}

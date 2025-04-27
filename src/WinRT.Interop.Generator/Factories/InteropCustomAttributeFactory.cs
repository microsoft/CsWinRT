// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop custom attributes.
/// </summary>
internal static class InteropCustomAttributeFactory
{
    /// <summary>
    /// Creates a new custom attribute value for <see cref="FixedAddressValueTypeAttribute"/>.
    /// </summary>
    /// <param name="owningModule">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    public static CustomAttribute FixedAddressValueType(ModuleDefinition owningModule)
    {
        // Resolve the '[FixedAddressValueType]' attribute type
        TypeDefinition fixedAddressValueTypeAttributeType = owningModule.MetadataResolver.ResolveType(owningModule.DefaultImporter.ImportType(typeof(FixedAddressValueTypeAttribute)))!;

        // Import the constructor, so we can use it
        ICustomAttributeType fixedAddressValueTypeAttributeCtor = (ICustomAttributeType)owningModule.DefaultImporter.ImportMethod(fixedAddressValueTypeAttributeType.GetConstructor()!);

        return new(fixedAddressValueTypeAttributeCtor);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="IsReadOnlyAttribute"/>.
    /// </summary>
    /// <param name="owningModule">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    public static CustomAttribute IsReadOnly(ModuleDefinition owningModule)
    {
        // Resolve the '[IsReadOnly]' attribute type
        TypeDefinition isReadOnlyAttributeType = owningModule.MetadataResolver.ResolveType(owningModule.DefaultImporter.ImportType(typeof(IsReadOnlyAttribute)))!;

        // Import the constructor, so we can use it
        ICustomAttributeType isReadOnlyAttributeCtor = (ICustomAttributeType)owningModule.DefaultImporter.ImportMethod(isReadOnlyAttributeType.GetConstructor()!);

        return new(isReadOnlyAttributeCtor);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="UnmanagedCallersOnlyAttribute"/>.
    /// </summary>
    /// <param name="owningModule">The module that the attribute will be used from.</param>
    /// <returns>The resulting <see cref="CustomAttribute"/> instance.</returns>
    /// <remarks>The attribute will specify the <see cref="CallConvMemberFunction"/> calling convention.</remarks>
    public static CustomAttribute UnmanagedCallersOnly(ModuleDefinition owningModule)
    {
        // Resolve the '[UnmanagedCallersOnly]' attribute type
        TypeDefinition unmanagedCallersOnlyAttributeType = owningModule.MetadataResolver.ResolveType(owningModule.DefaultImporter.ImportType(typeof(UnmanagedCallersOnlyAttribute)))!;

        // Import the constructor, so we can use it
        ICustomAttributeType unmanagedCallersOnlyAttributeCtor = (ICustomAttributeType)owningModule.DefaultImporter.ImportMethod(unmanagedCallersOnlyAttributeType.GetConstructor()!);

        // Get the 'Type[]' signature and reuse it (we need it for both the argument and the element)
        TypeSignature typeArraySignature = owningModule.DefaultImporter.ImportType(typeof(Type[])).ToTypeSignature();

        // Create the following attribute:
        //
        // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        return new(unmanagedCallersOnlyAttributeCtor, new CustomAttributeSignature(
            fixedArguments: [],
            namedArguments: [new CustomAttributeNamedArgument(
                memberType: CustomAttributeArgumentMemberType.Field,
                memberName: "CallConvs"u8,
                argumentType: typeArraySignature,
                argument: new CustomAttributeArgument(
                    argumentType: typeArraySignature,
                    elements: unmanagedCallersOnlyAttributeType.ToTypeSignature()))]));
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
                    elements: interopReferences.CallConvMemberFunction.Import(module).ToTypeSignature(isValueType: false)))]));
    }
}

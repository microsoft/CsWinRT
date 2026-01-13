// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for marshaller types for SZ array types.
    /// </summary>
    public static class SzArrayMarshaller
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Free</c> marshaller method.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static MethodDefinition Free(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;

            // Define the 'Free' method as follows:
            //
            // public static void Free(uint size, <ABI_ELEMENT_TYPE>* destination)
            MethodDefinition freeMethod = new(
                name: "Free"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        elementType.GetAbiType(interopReferences).Import(module).MakePointerType()]));

            return freeMethod;
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

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
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()])); // TODO

            // For 'string', 'Type', reference types and blittable types, we can reuse the shared stubs from the 'WindowsRuntimeArrayHelpers'
            // type in WinRT.Runtime.dll, to simplify the code and reduce binary size (as we can reuse all these stubs for multiple types).
            if (SignatureComparer.IgnoreVersion.Equals(elementType, interopReferences.CorLibTypeFactory.String))
            {
                freeMethod.CilMethodBody = new CilMethodBody
                {
                    Instructions =
                    {
                        { Ldarg_0 },
                        { Ldarg_1 },
                        { Call, interopReferences.WindowsRuntimeArrayHelpersFreeHStringArrayUnsafe.Import(module) },
                        { Ret }
                    }
                };
            }
            else if (SignatureComparer.IgnoreVersion.Equals(elementType, interopReferences.Type))
            {
                freeMethod.CilMethodBody = new CilMethodBody
                {
                    Instructions =
                    {
                        { Ldarg_0 },
                        { Ldarg_1 },
                        { Call, interopReferences.WindowsRuntimeArrayHelpersFreeTypeArrayUnsafe.Import(module) },
                        { Ret }
                    }
                };
            }
            else if (!elementType.Resolve()!.IsValueType || elementType.IsKeyValuePairType(interopReferences))
            {
                freeMethod.CilMethodBody = new CilMethodBody
                {
                    Instructions =
                    {
                        { Ldarg_0 },
                        { Ldarg_1 },
                        { Call, interopReferences.WindowsRuntimeArrayHelpersFreeObjectArrayUnsafe.Import(module) },
                        { Ret }
                    }
                };
            }
            else if (elementType.Resolve()!.IsByRefLike) // TODO: check for blittable
            {
                freeMethod.CilMethodBody = new CilMethodBody
                {
                    Instructions =
                    {
                        { Ldarg_0 },
                        { Ldarg_1 },
                        { Call, interopReferences.WindowsRuntimeArrayHelpersFreeBlittableArrayUnsafe.Import(module) },
                        { Ret }
                    }
                };
            }

            return freeMethod;
        }
    }
}

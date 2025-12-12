// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Resolvers;
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
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static MethodDefinition Free(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;

            // Define the 'Free' method as follows:
            //
            // public static void Free(uint size, <ABI_ELEMENT_TYPE>* array)
            MethodDefinition freeMethod = new(
                name: "Free"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        elementType.GetAbiType(interopReferences).Import(module).MakePointerType()]));

            // For 'string', 'Type', reference types and blittable types, we can reuse the shared stubs from the 'WindowsRuntimeArrayHelpers'
            // type in WinRT.Runtime.dll, to simplify the code and reduce binary size (as we can reuse all these stubs for multiple types).
            if (elementType.IsTypeOfString())
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
            else if (elementType.IsTypeOfType(interopReferences))
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
            else if (!elementType.IsValueType || elementType.IsConstructedKeyValuePairType(interopReferences))
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
            else if (elementType.IsBlittable(interopReferences) || elementType.IsTypeOfException(interopReferences))
            {
                // If the element type is a blittable type, we just release the array. We also do this for arrays
                // of 'Exception' type, because the ABI type is 'HResult', which is a custom-mapped blittable type.
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
            else
            {
                TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

                // We need to properly dispose each ABI value, so we have to resolve the marshaller first
                ITypeDefOrRef marshallerType = InteropMarshallerResolver.GetMarshallerType(elementType, interopReferences, emitState);

                // Get the reference to the 'Dispose' method we need
                IMethodDefOrRef disposeMethod = marshallerType.GetMethodDefOrRef(
                    name: "Dispose"u8,
                    signature: MethodSignature.CreateStatic(
                        returnType: module.CorLibTypeFactory.Void,
                        parameterTypes: [elementAbiType]));

                // Labels for jumps
                CilInstruction ldarg_1_throwIfNull = new(Ldarg_1);
                CilInstruction ldarg_1_loopStart = new(Ldarg_1);
                CilInstruction ldloc_0_loopCheck = new(Ldloc_0);

                freeMethod.CilMethodBody = new CilMethodBody
                {
                    LocalVariables = { new CilLocalVariable(interopReferences.CorLibTypeFactory.Int32) },
                    Instructions =
                    {
                        // if (size == 0) return;
                        { Ldarg_0 },
                        { Brtrue_S, ldarg_1_throwIfNull.CreateLabel() },
                        { Ret },

                        // ArgumentNullException.ThrowIfNull(array);
                        { ldarg_1_throwIfNull },
                        { Ldstr, "array" },
                        { Call, interopReferences.ArgumentNullExceptionThrowIfNull.Import(module) },

                        // int i = 0; goto LoopCheck;
                        { Ldc_I4_0 },
                        { Stloc_0 },
                        { Br_S, ldloc_0_loopCheck.CreateLabel() },

                        // <MARSHALLER_TYPE>.Dispose(array[i]);
                        { ldarg_1_loopStart },
                        { Ldloc_0 },
                        { Conv_I },
                        { Sizeof, elementAbiType.Import(module).ToTypeDefOrRef() },
                        { Mul },
                        { Add },
                        { Ldobj, elementAbiType.Import(module).ToTypeDefOrRef() },
                        { Call, disposeMethod.Import(module) },

                        // i++;
                        { Ldloc_0 },
                        { Ldc_I4_1 },
                        { Add },
                        { Stloc_0 },

                        // if (i < size) goto LoopStart;
                        { ldloc_0_loopCheck },
                        { Conv_I8 },
                        { Ldarg_0 },
                        { Conv_U8 },
                        { Blt_S, ldarg_1_loopStart.CreateLabel() },

                        // Marshal.FreeCoTaskMem((nint)array);
                        { Ldarg_1 },
                        { Call, interopReferences.MarshalFreeCoTaskMem.Import(module) },
                        { Ret }
                    }
                };
            }

            return freeMethod;
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="MethodDefinition"/> type.
/// </summary>
internal static class MethodDefinitionExtensions
{
    extension(MethodDefinition method)
    {
        /// <summary>
        /// Creates a new default constructor for a type that is executed when its declaring type is loaded by the CLR.
        /// </summary>
        /// <param name="module">The target module the method will be added to.</param>
        /// <returns>The constructor.</returns>
        public static MethodDefinition CreateDefaultConstructor(ModuleDefinition module)
        {
            // We should call the 'object' constructor, for correctness
            MemberReference object_ctor = module.CorLibTypeFactory.CorLibScope
                .CreateTypeReference("System"u8, "Object"u8)
                .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(module.CorLibTypeFactory.Void));

            // Create the parameterless constructor
            return new(
                name: ".ctor"u8,
                attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, object_ctor.Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new default constructor for a type that is executed when its declaring type is loaded by the CLR.
        /// </summary>
        /// <param name="module">The target module the method will be added to.</param>
        /// <param name="constructorMethod">The <see cref="MemberReference"/> for the base constructor to invoke.</param>
        /// <returns>The constructor.</returns>
        public static MethodDefinition CreateDefaultConstructor(ModuleDefinition module, MemberReference constructorMethod)
        {
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module);

            // Emit a call to the base constructor ('CreateConstructor' already adds the 'ret' instruction)
            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Call, constructorMethod.Import(module));

            return ctor;
        }

        /// <inheritdoc cref="CilMethodBody.Instructions"/>
        public CilInstructionCollection CilInstructions => (method.CilMethodBody ??= new CilMethodBody()).Instructions;

        /// <inheritdoc cref="CilMethodBody.LocalVariables"/>
        public CilLocalVariableCollection CilLocalVariables => (method.CilMethodBody ??= new CilMethodBody()).LocalVariables;

        /// <inheritdoc cref="CilMethodBody.ExceptionHandlers"/>
        public IList<CilExceptionHandler> CilExceptionHandlers => (method.CilMethodBody ??= new CilMethodBody()).ExceptionHandlers;

        /// <summary>
        /// Sets the indices of the parameters that are to be marshalled as <c>out</c> parameters in the CIL method body.
        /// </summary>
        /// <remarks>
        /// Note that the indices are 1-based (as index 0 would represent the implicit <see langword="this"/> parameter).
        /// </remarks>
        public ReadOnlySpan<ushort> CilOutParameterIndices
        {
            set
            {
                foreach (ushort index in value)
                {
                    method.ParameterDefinitions.Add(new ParameterDefinition(
                        sequence: index,
                        name: null,
                        attributes: ParameterAttributes.Out));
                }
            }
        }

        /// <summary>
        /// Enumerates all types that are instantiated via a <see langword="newobj"/> instruction in a given method.
        /// </summary>
        /// <returns>The resulting types.</returns>
        public IEnumerable<ITypeDefOrRef> EnumerateNewobjTypes()
        {
            // Make sure that we do have some instructions to analyze
            if (method.CilMethodBody is not { Instructions: CilInstructionCollection instructions })
            {
                yield break;
            }

            // Go through instruction to look for new objects
            foreach (CilInstruction instruction in instructions)
            {
                // We only care for 'newobj' instructions
                if (instruction.OpCode != Newobj)
                {
                    continue;
                }

                // Check that we can retrieve the target object type
                if (instruction.Operand is not IMethodDefOrRef { DeclaringType: ITypeDefOrRef objectType })
                {
                    continue;
                }

                yield return objectType;
            }
        }

        /// <summary>
        /// Enumerates the element types of all arrays that are instantiated via a <see langword="newarr"/> instruction in a given method.
        /// </summary>
        /// <returns>The resulting element types.</returns>
        public IEnumerable<ITypeDefOrRef> EnumerateNewarrElementTypes()
        {
            // Make sure that we do have some instructions to analyze
            if (method.CilMethodBody is not { Instructions: CilInstructionCollection instructions })
            {
                yield break;
            }

            // Go through instruction to look for new arrays
            foreach (CilInstruction instruction in instructions)
            {
                // We only care for 'newarr' instructions
                if (instruction.OpCode != Newarr)
                {
                    continue;
                }

                // Check that we can retrieve the target element type
                if (instruction.Operand is not ITypeDefOrRef elementType)
                {
                    continue;
                }

                yield return elementType;
            }
        }
    }
}
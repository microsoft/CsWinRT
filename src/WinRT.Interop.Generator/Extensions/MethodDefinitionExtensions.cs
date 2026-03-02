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
#pragma warning disable // TODO: remove this method when available in AsmResolver (see: https://github.com/Washi1337/AsmResolver/pull/712)
        /// <summary>
        /// Creates a new public constructor for a type that is executed when its declaring type is loaded by the CLR.
        /// </summary>
        /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use to resolve fundamental type signatures.</param>
        /// <param name="parameterTypes">An ordered list of types the parameters of the constructor should have.</param>
        /// <returns>The constructor.</returns>
        /// <remarks>
        /// The resulting method's body will consist of a single <c>ret</c> instruction, and does not contain a call to
        /// any of the declaring type's base classes. For an idiomatic .NET binary, this should be added.
        /// </remarks>
        public static MethodDefinition CreateConstructor(CorLibTypeFactory corLibTypeFactory, params TypeSignature[] parameterTypes)
        {
            var ctor = new MethodDefinition(".ctor",
                MethodAttributes.Public
                | MethodAttributes.SpecialName
                | MethodAttributes.RuntimeSpecialName,
                MethodSignature.CreateInstance(corLibTypeFactory.Void, parameterTypes));

            for (int i = 0; i < parameterTypes.Length; i++)
                ctor.ParameterDefinitions.Add(new ParameterDefinition(null));

            ctor.CilMethodBody = new CilMethodBody();
            ctor.CilMethodBody.Instructions.Add(CilOpCodes.Ret);

            return ctor;
        }
#pragma warning restore

        /// <summary>
        /// Creates a new public constructor for a type that is executed when an instance of this type is allocated by the CLR.
        /// </summary>
        /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use to resolve fundamental type signatures.</param>
        /// <param name="constructorMethod">The <see cref="MemberReference"/> for the base constructor to invoke.</param>
        /// <param name="parameterTypes">An ordered list of types the parameters of the constructor should have.</param>
        /// <returns>The constructor.</returns>
        public static MethodDefinition CreateConstructor(
            CorLibTypeFactory corLibTypeFactory,
            MemberReference constructorMethod,
            params TypeSignature[] parameterTypes)
        {
            MethodDefinition ctor = new(
                name: ".ctor",
                attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                signature: MethodSignature.CreateInstance(corLibTypeFactory.Void, parameterTypes));

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                ctor.ParameterDefinitions.Add(new ParameterDefinition(null));
            }

            // Load the implicit 'this' parameter first
            ctor.CilMethodBody = new CilMethodBody { Instructions = { Ldarg_0 } };

            // Load all the input parameters (the indices start at '1', because '0' is used for 'this')
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                ctor.CilMethodBody.Instructions.Add(CilInstruction.CreateLdarg(i + 1));
            }

            // Call the base constructor forwarding the input parameters
            _ = ctor.CilMethodBody.Instructions.Add(Call, constructorMethod);
            _ = ctor.CilMethodBody.Instructions.Add(Ret);

            return ctor;
        }

        /// <summary>
        /// Creates a new default constructor for a type that is executed when an instance of this type is allocated by the CLR.
        /// </summary>
        /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use to resolve fundamental type signatures.</param>
        /// <returns>The constructor.</returns>
        public static MethodDefinition CreateDefaultConstructor(CorLibTypeFactory corLibTypeFactory)
        {
            // We should call the 'object' constructor, for correctness
            MemberReference object_ctor = corLibTypeFactory.CorLibScope
                .CreateTypeReference("System"u8, "Object"u8)
                .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(corLibTypeFactory.Void));

            // Create the parameterless constructor
            return CreateDefaultConstructor(corLibTypeFactory, object_ctor);
        }

        /// <summary>
        /// Creates a new default constructor for a type that is executed when an instance of this type is allocated by the CLR.
        /// </summary>
        /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use to resolve fundamental type signatures.</param>
        /// <param name="constructorMethod">The <see cref="MemberReference"/> for the base constructor to invoke.</param>
        /// <returns>The constructor.</returns>
        public static MethodDefinition CreateDefaultConstructor(CorLibTypeFactory corLibTypeFactory, MemberReference constructorMethod)
        {
            // This is the same as the overload above, except it calls the provided constructor instead of the 'object' constructor
            return new(
                name: ".ctor"u8,
                attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                signature: MethodSignature.CreateInstance(corLibTypeFactory.Void))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, constructorMethod },
                    { Ret }
                }
            };
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
        /// Enumerates all types that are used for local variables in a given method.
        /// </summary>
        /// <returns>The resulting types.</returns>
        public IEnumerable<TypeSignature> EnumerateLocalVariableTypes()
        {
            // Make sure that we do have a body to analyze
            if (method.CilMethodBody is not CilMethodBody body)
            {
                yield break;
            }

            // Go through local variables and gather their types
            foreach (CilLocalVariable localVariable in body.LocalVariables ?? [])
            {
                yield return localVariable.VariableType;
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
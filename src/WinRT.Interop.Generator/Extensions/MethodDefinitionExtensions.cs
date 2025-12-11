// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
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
                    { Call, object_ctor },
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
    }
}
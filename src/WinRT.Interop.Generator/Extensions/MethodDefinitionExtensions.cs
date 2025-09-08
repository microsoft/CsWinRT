// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="MethodDefinition"/> type.
/// </summary>
internal static class MethodDefinitionExtensions
{
    extension(MethodDefinition method)
    {
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

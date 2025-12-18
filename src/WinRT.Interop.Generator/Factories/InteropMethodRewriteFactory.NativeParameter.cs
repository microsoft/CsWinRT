// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodRewriteFactory"/>
internal partial class InteropMethodRewriteFactory
{
    /// <summary>
    /// Contains the logic for marshalling native parameters (i.e. parameters that are passed to native methods).
    /// </summary>
    public static class NativeParameter
    {
        /// <summary>
        /// Performs two-pass code generation on a target method to marshal a managed parameter.
        /// </summary>
        /// <param name="parameterType">The parameter type that needs to be marshalled.</param>
        /// <param name="method">The target method to perform two-pass code generation on.</param>
        /// <param name="tryMarker">The target IL instruction to replace with the right set of specialized instructions, for the optional <see langword="try"/> block.</param>
        /// <param name="loadMarker">The target IL instruction to replace with the right set of specialized instructions to load the marshalled value.</param>
        /// <param name="finallyMarker">The target IL instruction to replace with the right set of specialized instructions, for the optional <see langword="finally"/> block.</param>
        /// <param name="parameterIndex">The index of the parameter to marshal.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static void RewriteMethod(
            TypeSignature parameterType,
            MethodDefinition method,
            CilInstruction tryMarker,
            CilInstruction loadMarker,
            CilInstruction finallyMarker,
            int parameterIndex,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            // Validate that we do have some IL body for the input method (this should always be the case)
            if (method.CilMethodBody is not CilMethodBody body)
            {
                throw WellKnownInteropExceptions.MethodRewriteMissingBodyError(method);
            }

            // If we didn't find any of markers, it means the target method is either invalid
            foreach (CilInstruction marker in (ReadOnlySpan<CilInstruction>)[tryMarker, loadMarker, finallyMarker])
            {
                if (!body.Instructions.Contains(marker))
                {
                    throw WellKnownInteropExceptions.MethodRewriteMarkerInstructionNotFoundError(marker, method);
                }
            }

            // Validate that the target parameter index is in range
            if ((uint)parameterIndex >= method.Parameters.Count)
            {
                throw WellKnownInteropExceptions.MethodRewriteParameterIndexNotValidError(parameterIndex, method);
            }

            Parameter source = method.Parameters[parameterIndex];

            // Validate that the type matches
            if (!SignatureComparer.IgnoreVersion.Equals(source.ParameterType, parameterType))
            {
                throw WellKnownInteropExceptions.MethodRewriteSourceParameterTypeMismatchError(source.ParameterType, parameterType, method);
            }

            if (parameterType.IsValueType)
            {
                // If the return type is blittable, we can just load it directly it directly (simplest case)
                if (parameterType.IsBlittable(interopReferences))
                {
                    body.Instructions.RemoveRange([tryMarker, finallyMarker]);
                    body.Instructions.ReplaceRange(loadMarker, CilInstruction.CreateLdarg(parameterIndex));
                }
                else if (parameterType.IsConstructedKeyValuePairType(interopReferences))
                {
                    // TODO
                }
                else if (parameterType.IsConstructedNullableValueType(interopReferences))
                {
                    // TODO
                }
                else
                {
                    // TODO
                }
            }
            else if (parameterType.IsTypeOfString())
            {
                // TODO
            }
            else if (parameterType is GenericInstanceTypeSignature)
            {
                // TODO
            }
            else
            {
                // TODO
            }
        }
    }
}
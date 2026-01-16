// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Resolvers;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodRewriteFactory"/>
internal partial class InteropMethodRewriteFactory
{
    /// <summary>
    /// Contains the logic for emitting direct calls to <c>ConvertToUnmanaged</c> for a given value (already on the stack).
    /// </summary>
    /// <remarks>
    /// This is similar to <see cref="RetVal"/>, but without protected regions or any unwrapping.
    /// </remarks>
    public static class RawRetVal
    {
        /// <summary>
        /// Performs two-pass code generation on a target method to emit a direct call to <c>ConvertToUnmanaged</c>.
        /// </summary>
        /// <param name="parameterType">The parameter type that needs to be marshalled.</param>
        /// <param name="method">The target method to perform two-pass code generation on.</param>
        /// <param name="marker">The target IL instruction to replace with the right set of specialized instructions.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static void RewriteMethod(
            TypeSignature parameterType,
            MethodDefinition method,
            CilInstruction marker,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            // Validate that we do have some IL body for the input method (this should always be the case)
            if (method.CilMethodBody is not CilMethodBody body)
            {
                throw WellKnownInteropExceptions.MethodRewriteMissingBodyError(method);
            }

            // If we didn't find the marker, it means the target method is either invalid
            if (!body.Instructions.ReferenceContains(marker))
            {
                throw WellKnownInteropExceptions.MethodRewriteMarkerInstructionNotFoundError(marker, method);
            }

            // If the parameter type is blittable, we have nothing else to do (the value is already loaded)
            if (parameterType.IsBlittable(interopReferences))
            {
                _ = body.Instructions.ReferenceRemove(marker);

                return;
            }

            // For nullable values, we actually need to call 'BoxToUnmanaged'
            if (parameterType.IsConstructedNullableValueType(interopReferences))
            {
                InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                body.Instructions.ReferenceReplaceRange(marker, new CilInstruction(Call, marshallerType.BoxToUnmanaged().Import(module)));
            }
            else
            {
                // For any other types, we always just forward directly to 'ConvertToUnmanaged', without any other changes
                InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                body.Instructions.ReferenceReplaceRange(marker, new CilInstruction(Call, marshallerType.ConvertToUnmanaged().Import(module)));
            }
        }
    }
}
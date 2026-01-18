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

#pragma warning disable CS8620 // TODO: remove once Roslyn bug is fixed

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodRewriteFactory"/>
internal partial class InteropMethodRewriteFactory
{
    /// <summary>
    /// Contains the logic for disposing (or releasing) a given value (already on the stack).
    /// </summary>
    public static class Dispose
    {
        /// <summary>
        /// Performs two-pass code generation on a target method to dispose (or release) a given value.
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

            // For unmanaged (or blittable) value types, this call is not valid, as they can't be disposed.
            // We also need to check whether the type is 'Exception', as its ABI type is blittable as well.
            if ((parameterType.IsValueType && !parameterType.IsManagedValueType(interopReferences)) || parameterType.IsTypeOfException(interopReferences))
            {
                throw WellKnownInteropExceptions.MethodRewriteDisposeNotAvailableError(parameterType, method);
            }

            // For managed value types, we call 'Dispose' on their marshaller type
            if (parameterType.IsManagedValueType(interopReferences))
            {
                InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                body.Instructions.ReferenceReplaceRange(marker, new CilInstruction(Call, marshallerType.Dispose().Import(module)));
            }
            else if (parameterType.IsTypeOfString())
            {
                // When disposing 'string' values, we must use 'HStringMarshaller' (the ABI type is not actually a COM object)
                body.Instructions.ReferenceReplaceRange(marker, new CilInstruction(Call, interopReferences.HStringMarshallerFree.Import(module)));
            }
            else
            {
                // For everything else, we just release the native object. This also applies to generic value types,
                // such as 'KeyValuePair<TKey, TValue>', because they are actually interface types at the ABI level.
                body.Instructions.ReferenceReplaceRange(marker, new CilInstruction(Call, interopReferences.WindowsRuntimeUnknownMarshallerFree.Import(module)));
            }
        }
    }
}
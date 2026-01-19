// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Resolvers;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

#pragma warning disable CS8620 // TODO: remove once Roslyn bug is fixed

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory to rewrite interop method definitons, and add marshalling code as needed.
/// </summary>
internal static partial class InteropMethodRewriteFactory
{
    /// <summary>
    /// Contains the logic for marshalling managed parameters (i.e. parameters that are passed to managed methods).
    /// </summary>
    public static class ManagedParameter
    {
        /// <summary>
        /// Performs two-pass code generation on a target method to marshal an unmanaged parameter.
        /// </summary>
        /// <param name="parameterType">The parameter type that needs to be marshalled.</param>
        /// <param name="method">The target method to perform two-pass code generation on.</param>
        /// <param name="marker">The target IL instruction to replace with the right set of specialized instructions.</param>
        /// <param name="parameterIndex">The index of the parameter to marshal.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static void RewriteMethod(
            TypeSignature parameterType,
            MethodDefinition method,
            CilInstruction marker,
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

            // If we didn't find the marker, it means the target method is either invalid
            if (!body.Instructions.ReferenceContains(marker))
            {
                throw WellKnownInteropExceptions.MethodRewriteMarkerInstructionNotFoundError(marker, method);
            }

            // Validate that the target parameter index is in range
            if ((uint)parameterIndex >= method.Parameters.Count)
            {
                throw WellKnownInteropExceptions.MethodRewriteParameterIndexNotValidError(parameterIndex, method);
            }

            Parameter source = method.Parameters[parameterIndex];

            // Validate that the ABI type matches
            if (!SignatureComparer.IgnoreVersion.Equals(source.ParameterType, parameterType.GetAbiType(interopReferences)))
            {
                throw WellKnownInteropExceptions.MethodRewriteSourceParameterTypeMismatchError(source.ParameterType, parameterType, method);
            }

            // See comments in the marshalling code for 'ManagedValue' for additional details on the code below.
            // The two are identical, the only difference is this method also loads the parameters on the stack.
            if (parameterType.IsValueType)
            {
                if (parameterType.IsBlittable(interopReferences))
                {
                    body.Instructions.ReferenceReplaceRange(marker, CilInstruction.CreateLdarg(parameterIndex));
                }
                else if (parameterType.IsConstructedNullableValueType(interopReferences))
                {
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                    body.Instructions.ReferenceReplaceRange(marker, [
                        CilInstruction.CreateLdarg(parameterIndex),
                        new CilInstruction(Call, marshallerType.UnboxToManaged().Import(module))]);
                }
                else if (SignatureComparer.IgnoreVersion.Equals(parameterType, interopReferences.ReadOnlySpanChar))
                {
                    body.Instructions.ReferenceReplaceRange(marker, [
                        CilInstruction.CreateLdarg(parameterIndex),
                        new CilInstruction(Call, interopReferences.HStringMarshallerConvertToManagedUnsafe.Import(module))]);
                }
                else
                {
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                    body.Instructions.ReferenceReplaceRange(marker, [
                        CilInstruction.CreateLdarg(parameterIndex),
                        new CilInstruction(Call, marshallerType.ConvertToManaged().Import(module))]);
                }
            }
            else if (parameterType.IsTypeOfString())
            {
                body.Instructions.ReferenceReplaceRange(marker, [
                    CilInstruction.CreateLdarg(parameterIndex),
                    new CilInstruction(Call, interopReferences.HStringMarshallerConvertToManaged.Import(module))]);
            }
            else
            {
                InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                body.Instructions.ReferenceReplaceRange(marker, [
                    CilInstruction.CreateLdarg(parameterIndex),
                    new CilInstruction(Call, marshallerType.ConvertToManaged().Import(module))]);
            }
        }
    }
}
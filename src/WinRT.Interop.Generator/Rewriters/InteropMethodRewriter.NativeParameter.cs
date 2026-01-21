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
using WindowsRuntime.InteropGenerator.Resolvers;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

#pragma warning disable CS1573, CS8620 // TODO: remove once Roslyn bug is fixed

namespace WindowsRuntime.InteropGenerator.Rewriters;

/// <inheritdoc cref="InteropMethodRewriter"/>
internal partial class InteropMethodRewriter
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
                if (!body.Instructions.ReferenceContains(marker))
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
                    body.Instructions.ReferenceRemoveRange(tryMarker, finallyMarker);
                    body.Instructions.ReferenceReplaceRange(loadMarker, CilInstruction.CreateLdarg(parameterIndex));
                }
                else if (parameterType.IsConstructedKeyValuePairType(interopReferences))
                {
                    RewriteBody(
                        parameterType: parameterType,
                        body: body,
                        tryMarker: tryMarker,
                        loadMarker: loadMarker,
                        finallyMarker: finallyMarker,
                        parameterIndex: parameterIndex,
                        marshallerMethod: emitState.LookupTypeDefinition(parameterType, "Marshaller").GetMethod("ConvertToUnmanaged"),
                        disposeMethod: null,
                        interopReferences: interopReferences,
                        module: module);
                }
                else if (parameterType.IsConstructedNullableValueType(interopReferences))
                {
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                    RewriteBody(
                        parameterType: parameterType,
                        body: body,
                        tryMarker: tryMarker,
                        loadMarker: loadMarker,
                        finallyMarker: finallyMarker,
                        parameterIndex: parameterIndex,
                        marshallerMethod: marshallerType.BoxToUnmanaged(),
                        disposeMethod: null,
                        interopReferences: interopReferences,
                        module: module);
                }
                else
                {
                    // The last case handles all other value types, which need explicit disposal for their ABI values
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                    RewriteBody(
                        parameterType: parameterType,
                        body: body,
                        tryMarker: tryMarker,
                        loadMarker: loadMarker,
                        finallyMarker: finallyMarker,
                        parameterIndex: parameterIndex,
                        marshallerMethod: marshallerType.ConvertToUnmanaged(),
                        disposeMethod: marshallerType.Dispose(),
                        interopReferences: interopReferences,
                        module: module);
                }
            }
            else if (parameterType.IsTypeOfString())
            {
                RewriteBodyForTypeOfString(
                    body: body,
                    tryMarker: tryMarker,
                    loadMarker: loadMarker,
                    finallyMarker: finallyMarker,
                    parameterIndex: parameterIndex,
                    interopReferences: interopReferences,
                    module: module);
            }
            else if (parameterType.IsTypeOfType(interopReferences))
            {
                RewriteBodyForTypeOfType(
                    body: body,
                    tryMarker: tryMarker,
                    loadMarker: loadMarker,
                    finallyMarker: finallyMarker,
                    parameterIndex: parameterIndex,
                    interopReferences: interopReferences,
                    module: module);
            }
            else if (parameterType.IsTypeOfException(interopReferences))
            {
                // The ABI type of 'Exception' is unmanaged, so we can marshal the value directly
                body.Instructions.ReferenceRemoveRange(tryMarker, finallyMarker);
                body.Instructions.ReferenceReplaceRange(loadMarker, [
                    CilInstruction.CreateLdarg(parameterIndex),
                    new CilInstruction(Call, interopReferences.ExceptionMarshallerConvertToUnmanaged.Import(module))]);
            }
            else
            {
                // Get the marshaller for all other types (doesn't matter if constructed generics or not)
                InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(parameterType, interopReferences, emitState);

                RewriteBody(
                    parameterType: parameterType,
                    body: body,
                    tryMarker: tryMarker,
                    loadMarker: loadMarker,
                    finallyMarker: finallyMarker,
                    parameterIndex: parameterIndex,
                    marshallerMethod: marshallerType.ConvertToUnmanaged(),
                    disposeMethod: null,
                    interopReferences: interopReferences,
                    module: module);
            }
        }

        /// <inheritdoc cref="RewriteMethod"/>
        /// <param name="body">The target body to perform two-pass code generation on.</param>
        /// <param name="marshallerMethod">The method to invoke to marshal the managed value.</param>
        /// <param name="disposeMethod">The method to invoke to dispose the original ABI value, if a value type.</param>
        private static void RewriteBody(
            TypeSignature parameterType,
            CilMethodBody body,
            CilInstruction tryMarker,
            CilInstruction loadMarker,
            CilInstruction finallyMarker,
            int parameterIndex,
            IMethodDefOrRef marshallerMethod,
            IMethodDefOrRef? disposeMethod,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature parameterAbiType = parameterType.GetAbiType(interopReferences);

            // Prepare the new local for the ABI value (or 'WindowsRuntimeObjectReferenceValue').
            // This is only for parameter types that need some kind of disposal after the call.
            CilLocalVariable loc_parameter = parameterAbiType.IsTypeOfVoidPointer()
                ? new CilLocalVariable(interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToValueTypeSignature())
                : new CilLocalVariable(parameterAbiType.Import(module));

            body.LocalVariables.Add(loc_parameter);

            // Prepare the jump labels
            CilInstruction nop_tryStart = new(Nop);
            CilInstruction ldloc_or_a_finallyStart;
            CilInstruction nop_finallyEnd = new(Nop);

            // Marshal the value before the call
            body.Instructions.ReferenceReplaceRange(tryMarker, [
                CilInstruction.CreateLdarg(parameterIndex),
                new CilInstruction(Call, marshallerMethod.Import(module)),
                CilInstruction.CreateStloc(loc_parameter, body),
                nop_tryStart]);

            // Get the ABI value to pass to the native method. If we have a 'WindowsRuntimeObjectReferenceValue',
            // we'll get the pointer from it. Otherwise, we just load the ABI value and pass it directly to native.
            if (parameterAbiType.IsTypeOfVoidPointer())
            {
                body.Instructions.ReferenceReplaceRange(loadMarker, [
                    new CilInstruction(Ldloca_S, loc_parameter),
                    new CilInstruction(Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module))]);
            }
            else
            {
                body.Instructions.ReferenceReplaceRange(loadMarker, CilInstruction.CreateLdloc(loc_parameter, body));
            }

            // Release the ABI value, or the 'WindowsRuntimeObjectReferenceValue' value, after the call.
            // Once again we need specialized logic for when we're using 'WindowsRuntimeObjectReferenceValue'.
            // That is, for that object we'll need to call the instance 'Dispose' on it directly. For all
            // other cases, we'll instead load the local and pass it to the 'Dispose' method on the marshaller.
            if (parameterAbiType.IsTypeOfVoidPointer())
            {
                ldloc_or_a_finallyStart = new CilInstruction(Ldloca_S, loc_parameter);

                body.Instructions.ReferenceReplaceRange(finallyMarker, [
                    ldloc_or_a_finallyStart,
                    new CilInstruction(Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module)),
                    new CilInstruction(Endfinally),
                    nop_finallyEnd]);
            }
            else
            {
                ldloc_or_a_finallyStart = CilInstruction.CreateLdloc(loc_parameter, body);

                body.Instructions.ReferenceReplaceRange(finallyMarker, [
                    ldloc_or_a_finallyStart,
                    new CilInstruction(Call, disposeMethod!.Import(module)),
                    new CilInstruction(Endfinally),
                    nop_finallyEnd]);
            }

            // Setup the protected region to call the 'Dispose' method in a 'finally' block
            body.ExceptionHandlers.Add(new CilExceptionHandler
            {
                HandlerType = CilExceptionHandlerType.Finally,
                TryStart = nop_tryStart.CreateLabel(),
                TryEnd = ldloc_or_a_finallyStart.CreateLabel(),
                HandlerStart = ldloc_or_a_finallyStart.CreateLabel(),
                HandlerEnd = nop_finallyEnd.CreateLabel()
            });
        }

        /// <inheritdoc cref="RewriteMethod"/>
        /// <param name="body">The target body to perform two-pass code generation on.</param>
        private static void RewriteBodyForTypeOfString(
            CilMethodBody body,
            CilInstruction tryMarker,
            CilInstruction loadMarker,
            CilInstruction finallyMarker,
            int parameterIndex,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            // Declare the local variables:
            //   [0]: 'ref char' (for the pinned 'string')
            //   [1]: 'HStringReference' (for 'hstringReference')
            //   [2]: 'int?' (for 'length')
            CilLocalVariable loc_0_pinnedString = new(interopReferences.CorLibTypeFactory.Char.MakeByReferenceType().MakePinnedType());
            CilLocalVariable loc_1_hstringReference = new(interopReferences.HStringReference.Import(module).ToValueTypeSignature());
            CilLocalVariable loc_2_length = new(interopReferences.Nullable1.MakeGenericValueType(interopReferences.CorLibTypeFactory.Int32).Import(module));

            body.LocalVariables.Add(loc_0_pinnedString);
            body.LocalVariables.Add(loc_1_hstringReference);
            body.LocalVariables.Add(loc_2_length);

            // Prepare the jump labels
            CilInstruction ldarg_pinning = CilInstruction.CreateLdarg(parameterIndex);
            CilInstruction ldarg_lengthNullCheck = CilInstruction.CreateLdarg(parameterIndex);
            CilInstruction ldarg_getLength = CilInstruction.CreateLdarg(parameterIndex);
            CilInstruction ldloca_s_getHStringReference = new(Ldloca_S, loc_1_hstringReference);

            // Pin the input 'string' value, get the (possibly 'null') length, and create the 'HStringReference' value
            body.Instructions.ReferenceReplaceRange(tryMarker, [

                // fixed (char* p = value) { }
                CilInstruction.CreateLdarg(parameterIndex),
                new CilInstruction(Brtrue_S, ldarg_pinning.CreateLabel()),
                new CilInstruction(Ldc_I4_0),
                new CilInstruction(Conv_U),
                new CilInstruction(Br_S, ldarg_lengthNullCheck.CreateLabel()),
                ldarg_pinning,
                new CilInstruction(Call, interopReferences.StringGetPinnableReference.Import(module)),
                CilInstruction.CreateStloc(loc_0_pinnedString, body),
                CilInstruction.CreateLdloc(loc_0_pinnedString, body),
                new CilInstruction(Conv_U),

                // int? length = value?.Length;
                ldarg_lengthNullCheck,
                new CilInstruction(Brtrue_S, ldarg_getLength.CreateLabel()),
                new CilInstruction(Ldloca_S, loc_2_length),
                new CilInstruction(Initobj, interopReferences.NullableInt32.Import(module).ToTypeDefOrRef()),
                CilInstruction.CreateLdloc(loc_2_length, body),
                new CilInstruction(Br_S, ldloca_s_getHStringReference.CreateLabel()),
                ldarg_getLength,
                new CilInstruction(Call, interopReferences.Stringget_Length.Import(module)),
                new CilInstruction(Newobj, interopReferences.Nullable1_ctor(interopReferences.CorLibTypeFactory.Int32).Import(module)),

                // HStringMarshaller.ConvertToUnmanagedUnsafe(p, length, out HStringReference hstringReference);
                ldloca_s_getHStringReference,
                new CilInstruction(Call, interopReferences.HStringMarshallerConvertToUnmanagedUnsafe.Import(module))]);

            // Get the 'HString' value from the reference and pass it as a parameter
            body.Instructions.ReferenceReplaceRange(loadMarker, [
                new CilInstruction(Ldloca_S, loc_1_hstringReference),
                new CilInstruction(Call, interopReferences.HStringReferenceget_HString.Import(module))]);

            // Unpin the local (just assign 'null' to it)
            body.Instructions.ReferenceReplaceRange(finallyMarker, [
                new CilInstruction(Ldc_I4_0),
                new CilInstruction(Conv_U),
                CilInstruction.CreateStloc(loc_0_pinnedString, body)]);
        }

        /// <inheritdoc cref="RewriteMethod"/>
        /// <param name="body">The target body to perform two-pass code generation on.</param>
        private static void RewriteBodyForTypeOfType(
            CilMethodBody body,
            CilInstruction tryMarker,
            CilInstruction loadMarker,
            CilInstruction finallyMarker,
            int parameterIndex,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            // Declare the local variables:
            //   [0]: 'TypeReference' (for 'typeReference')
            //   [1]: 'ref byte' (for the pinned type reference)
            CilLocalVariable loc_0_typeReference = new(interopReferences.TypeReference.Import(module).ToValueTypeSignature());
            CilLocalVariable loc_1_pinnedTypeReference = new(interopReferences.CorLibTypeFactory.Byte.MakeByReferenceType().MakePinnedType());

            body.LocalVariables.Add(loc_0_typeReference);
            body.LocalVariables.Add(loc_1_pinnedTypeReference);

            // Get the 'TypeReference' value and pin it
            body.Instructions.ReferenceReplaceRange(tryMarker, [
                CilInstruction.CreateLdarg(parameterIndex),
                new CilInstruction(Ldloca_S, loc_0_typeReference),
                new CilInstruction(Call, interopReferences.TypeMarshallerConvertToUnmanagedUnsafe.Import(module)),
                new CilInstruction(Ldloca_S, loc_0_typeReference),
                new CilInstruction(Call, interopReferences.TypeReferenceGetPinnableReference.Import(module)),
                CilInstruction.CreateStloc(loc_1_pinnedTypeReference, body)]);

            // Get the ABI 'Type' value and pass it as a parameter
            body.Instructions.ReferenceReplaceRange(loadMarker, [
                new CilInstruction(Ldloca_S, loc_0_typeReference),
                new CilInstruction(Call, interopReferences.TypeReferenceConvertToUnmanagedUnsafe.Import(module))]);

            // Unpin the local (just assign 'null' to it)
            body.Instructions.ReferenceReplaceRange(finallyMarker, [
                new CilInstruction(Ldc_I4_0),
                new CilInstruction(Conv_U),
                CilInstruction.CreateStloc(loc_1_pinnedTypeReference, body)]);
        }
    }
}
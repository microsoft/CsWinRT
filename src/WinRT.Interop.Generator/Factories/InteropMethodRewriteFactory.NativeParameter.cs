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
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

#pragma warning disable CS1573

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
                    TypeSignature underlyingType = ((GenericInstanceTypeSignature)parameterType).TypeArguments[0];

                    // For 'Nullable<T>' return types, we need the marshaller for the instantiated 'T' type (same as for return values)
                    ITypeDefOrRef marshallerType = GetValueTypeMarshallerType(underlyingType, interopReferences, emitState);

                    // Get the right reference to the unboxing marshalling method to call
                    IMethodDefOrRef marshallerMethod = marshallerType.GetMethodDefOrRef(
                        name: "BoxToUnmanaged"u8,
                        signature: MethodSignature.CreateStatic(
                            returnType: interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                            parameterTypes: [parameterType]));

                    RewriteBody(
                        parameterType: parameterType,
                        body: body,
                        tryMarker: tryMarker,
                        loadMarker: loadMarker,
                        finallyMarker: finallyMarker,
                        parameterIndex: parameterIndex,
                        marshallerMethod: marshallerMethod,
                        disposeMethod: null,
                        interopReferences: interopReferences,
                        module: module);
                }
                else
                {
                    // The last case handles all other value types, which need explicit disposal for their ABI values
                    ITypeDefOrRef marshallerType = GetValueTypeMarshallerType(parameterType, interopReferences, emitState);

                    // Get the reference to 'ConvertToUnmanaged' to produce the resulting value to pass as argument
                    IMethodDefOrRef marshallerMethod = marshallerType.GetMethodDefOrRef(
                        name: "ConvertToUnmanaged"u8,
                        signature: MethodSignature.CreateStatic(
                            returnType: parameterType.GetAbiType(interopReferences),
                            parameterTypes: [parameterType]));

                    // Get the reference to 'Dispose' method to call on the ABI value
                    IMethodDefOrRef disposeMethod = marshallerType.GetMethodDefOrRef(
                        name: "Dispose"u8,
                        signature: MethodSignature.CreateStatic(
                            returnType: interopReferences.CorLibTypeFactory.Void,
                            parameterTypes: [parameterType.GetAbiType(interopReferences)]));

                    RewriteBody(
                        parameterType: parameterType,
                        body: body,
                        tryMarker: tryMarker,
                        loadMarker: loadMarker,
                        finallyMarker: finallyMarker,
                        parameterIndex: parameterIndex,
                        marshallerMethod: marshallerMethod,
                        disposeMethod: disposeMethod,
                        interopReferences: interopReferences,
                        module: module);
                }
            }
            else if (parameterType.IsTypeOfString())
            {
                // TODO
                body.Instructions.ReferenceRemoveRange(tryMarker, finallyMarker);
                body.Instructions.ReferenceReplaceRange(loadMarker, new CilInstruction(Ldnull));
            }
            else if (parameterType.IsTypeOfType(interopReferences))
            {
                // TODO
                body.Instructions.ReferenceRemoveRange(tryMarker, finallyMarker);
                body.Instructions.ReferenceReplaceRange(loadMarker, new CilInstruction(Ldnull));
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
                ITypeDefOrRef marshallerType = GetReferenceTypeMarshallerType(parameterType, interopReferences, emitState);

                // Get the reference to 'ConvertToUnmanaged' to produce the resulting value to pass as argument
                IMethodDefOrRef marshallerMethod = marshallerType.GetMethodDefOrRef(
                    name: "ConvertToUnmanaged"u8,
                    signature: MethodSignature.CreateStatic(
                        returnType: interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                        parameterTypes: [parameterType]));

                RewriteBody(
                    parameterType: parameterType,
                    body: body,
                    tryMarker: tryMarker,
                    loadMarker: loadMarker,
                    finallyMarker: finallyMarker,
                    parameterIndex: parameterIndex,
                    marshallerMethod: marshallerMethod,
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
    }
}
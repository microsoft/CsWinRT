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

#pragma warning disable CS1573

namespace WindowsRuntime.InteropGenerator.Rewriters;

/// <inheritdoc cref="InteropMethodRewriter"/>
internal partial class InteropMethodRewriter
{
    /// <summary>
    /// Contains the logic for marshalling return values.
    /// </summary>
    public static class ReturnValue
    {
        /// <summary>
        /// Performs two-pass code generation on a target method to marshal a managed return value.
        /// </summary>
        /// <param name="returnType">The return type that needs to be marshalled.</param>
        /// <param name="method">The target method to perform two-pass code generation on.</param>
        /// <param name="marker">The target IL instruction to replace with the right set of specialized instructions.</param>
        /// <param name="source">The method local containing the ABI value to marshal.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static void RewriteMethod(
            TypeSignature returnType,
            MethodDefinition method,
            CilInstruction marker,
            CilLocalVariable source,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            // Validate that we do have some IL body for the input method (this should always be the case)
            if (method.CilMethodBody is not CilMethodBody body)
            {
                throw WellKnownInteropExceptions.MethodRewriteMissingBodyError(method);
            }

            // If we didn't find the marker, it means the target method is either invalid, or the
            // supplied marker was incorrect (or the caller forgot to add it to the method body).
            if (!body.Instructions.ReferenceContains(marker))
            {
                throw WellKnownInteropExceptions.MethodRewriteMarkerInstructionNotFoundError(marker, method);
            }

            // Also validate that the target local variable is also actually part of the method
            if (!body.LocalVariables.ReferenceContains(source))
            {
                throw WellKnownInteropExceptions.MethodRewriteSourceLocalNotFoundError(source, method);
            }

            // Validate that the ABI type matches
            if (!SignatureComparer.IgnoreVersion.Equals(source.VariableType, returnType.GetAbiType(interopReferences)))
            {
                throw WellKnownInteropExceptions.MethodRewriteSourceLocalTypeMismatchError(source.VariableType, returnType, method);
            }

            if (returnType.IsValueType)
            {
                // If the return type is blittable, we can always return it directly (simplest case)
                if (returnType.IsBlittable(interopReferences))
                {
                    body.Instructions.ReferenceReplaceRange(marker, [
                        CilInstruction.CreateLdloc(source, body),
                        new CilInstruction(Ret)]);
                }
                else if (returnType.IsConstructedKeyValuePairType(interopReferences))
                {
                    // If the type is some constructed 'KeyValuePair<,>' type, we use the generated marshaller.
                    // So here we first marshal the managed value, then release the original interface pointer.
                    RewriteBody(
                        returnType: returnType,
                        body: body,
                        marker: marker,
                        source: source,
                        marshallerMethod: emitState.LookupTypeDefinition(returnType, "Marshaller").GetMethod("ConvertToManaged"),
                        releaseOrDisposeMethod: interopReferences.WindowsRuntimeUnknownMarshallerFree);
                }
                else if (returnType.IsConstructedNullableValueType(interopReferences))
                {
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(returnType, interopReferences, emitState);

                    // Emit code similar to 'KeyValuePair<,>' above, to marshal the resulting 'Nullable<T>' value
                    RewriteBody(
                        returnType: returnType,
                        body: body,
                        marker: marker,
                        source: source,
                        marshallerMethod: marshallerType.UnboxToManaged(),
                        releaseOrDisposeMethod: interopReferences.WindowsRuntimeUnknownMarshallerFree);
                }
                else if (returnType.IsManagedValueType(interopReferences))
                {
                    // Here we're marshalling a value type that is managed, meaning its ABI type will
                    // hold some references to unmanaged resources. In this case we need to resolve the
                    // marshaller type so we can both marshal the value and also clean resources after.
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(returnType, interopReferences, emitState);

                    // Emit code similar to the cases above, but calling 'Dispose' on the ABI type instead of releasing it
                    RewriteBody(
                        returnType: returnType,
                        body: body,
                        marker: marker,
                        source: source,
                        marshallerMethod: marshallerType.ConvertToManaged(),
                        releaseOrDisposeMethod: marshallerType.Dispose());
                }
                else
                {
                    // The last case is a non-blittable, unmanaged value type. That is, we still have to call
                    // the marshalling method to get the return value, but no resources cleanup is needed.
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(returnType, interopReferences, emitState);

                    // We can directly call the marshaller and return it, no 'try/finally' complexity is needed
                    body.Instructions.ReferenceReplaceRange(marker, [
                        CilInstruction.CreateLdloc(source, body),
                        new CilInstruction(Call, marshallerType.ConvertToManaged()),
                        new CilInstruction(Ret)]);
                }
            }
            else if (returnType.IsTypeOfString())
            {
                // When marshalling 'string' values, we must use 'HStringMarshaller' (the ABI type is not actually a COM object)
                RewriteBody(
                    returnType: returnType,
                    body: body,
                    marker: marker,
                    source: source,
                    marshallerMethod: interopReferences.HStringMarshallerConvertToManaged,
                    releaseOrDisposeMethod: interopReferences.HStringMarshallerFree);
            }
            else if (returnType.IsTypeOfType(interopReferences))
            {
                // 'Type' is special, in that the ABI type is a managed value type, but the return is a reference type
                RewriteBody(
                    returnType: returnType,
                    body: body,
                    marker: marker,
                    source: source,
                    marshallerMethod: interopReferences.TypeMarshallerConvertToManaged,
                    releaseOrDisposeMethod: interopReferences.TypeMarshallerDispose);
            }
            else if (returnType.IsTypeOfException(interopReferences))
            {
                // 'Exception' is also special, though it's simple: the ABI type is an unmanaged value type
                body.Instructions.ReferenceReplaceRange(marker, [
                    CilInstruction.CreateLdloc(source, body),
                    new CilInstruction(Call, interopReferences.ExceptionMarshallerConvertToManaged),
                    new CilInstruction(Ret)]);
            }
            else
            {
                // Get the marshaller type for either generic reference types, or all other reference types
                InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(returnType, interopReferences, emitState);

                // Marshal the value and release the original interface pointer
                RewriteBody(
                    returnType: returnType,
                    body: body,
                    marker: marker,
                    source: source,
                    marshallerMethod: marshallerType.ConvertToManaged(),
                    releaseOrDisposeMethod: interopReferences.WindowsRuntimeUnknownMarshallerFree);
            }
        }

        /// <inheritdoc cref="RewriteMethod"/>
        /// <param name="body">The target body to perform two-pass code generation on.</param>
        /// <param name="marshallerMethod">The method to invoke to marshal the managed value.</param>
        /// <param name="releaseOrDisposeMethod">The method to invoke to release or dispose the original ABI value.</param>
        private static void RewriteBody(
            TypeSignature returnType,
            CilMethodBody body,
            CilInstruction marker,
            CilLocalVariable source,
            IMethodDefOrRef marshallerMethod,
            IMethodDefOrRef releaseOrDisposeMethod)
        {
            // Add a new local for the marshalled return value. We need this because it will be
            // assigned from inside a protected region (a 'try') block, so we can't return the
            // value directly. Instead, we'll load and return the local after the handler code.
            CilLocalVariable loc_returnValue = new(returnType);

            body.LocalVariables.Add(loc_returnValue);

            // Setup the target instructions to be either jump labels or targets for the handler
            CilInstruction ldloc_tryStart = CilInstruction.CreateLdloc(source, body);
            CilInstruction ldloc_finallyStart = CilInstruction.CreateLdloc(source, body);
            CilInstruction ldloc_finallyEnd = CilInstruction.CreateLdloc(loc_returnValue, body);

            // Marshal the value and release the original interface pointer, or dispose the ABI value
            body.Instructions.ReferenceReplaceRange(marker, [
                ldloc_tryStart,
                new CilInstruction(Call, marshallerMethod),
                CilInstruction.CreateStloc(loc_returnValue, body),
                new CilInstruction(Leave_S, ldloc_finallyEnd.CreateLabel()),
                ldloc_finallyStart,
                new CilInstruction(Call, releaseOrDisposeMethod),
                new CilInstruction(Endfinally),
                ldloc_finallyEnd,
                new CilInstruction(Ret)]);

            // Setup the protected region to call the release or dispose method in a 'finally' block
            body.ExceptionHandlers.Add(new CilExceptionHandler
            {
                HandlerType = CilExceptionHandlerType.Finally,
                TryStart = ldloc_tryStart.CreateLabel(),
                TryEnd = ldloc_finallyStart.CreateLabel(),
                HandlerStart = ldloc_finallyStart.CreateLabel(),
                HandlerEnd = ldloc_finallyEnd.CreateLabel()
            });
        }
    }
}
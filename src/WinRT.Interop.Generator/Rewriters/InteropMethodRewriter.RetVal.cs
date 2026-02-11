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

#pragma warning disable CS1573, IDE0072

namespace WindowsRuntime.InteropGenerator.Rewriters;

/// <inheritdoc cref="InteropMethodRewriter"/>
internal partial class InteropMethodRewriter
{
    /// <summary>
    /// Contains the logic for marshalling native <c>[retval]</c> values.
    /// </summary>
    public static class RetVal
    {
        /// <summary>
        /// Performs two-pass code generation on a target method to marshal a managed <c>[retval]</c> value.
        /// </summary>
        /// <param name="retValType">The <c>[retval]</c> type that needs to be marshalled.</param>
        /// <param name="method">The target method to perform two-pass code generation on.</param>
        /// <param name="marker">The target IL instruction to replace with the right set of specialized instructions.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <remarks>
        /// <para>
        /// This method assumes the evaluation stack already has two values on its top:
        /// <list type="bullet">
        ///   <item>The target address of the <c>[retval]</c> value, as an unmanaged pointer.</item>
        ///   <item>The managed value to marshal and assign to the target address.</item>
        /// </list>
        /// </para>
        /// <para>
        /// This method also assumes that the target location has the correct ABI type for <paramref name="retValType"/>.
        /// </para>
        /// </remarks>
        public static void RewriteMethod(
            TypeSignature retValType,
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

            // If we didn't find the marker, it means the target method is either invalid, or the
            // supplied marker was incorrect (or the caller forgot to add it to the method body).
            if (!body.Instructions.ReferenceContains(marker))
            {
                throw WellKnownInteropExceptions.MethodRewriteMarkerInstructionNotFoundError(marker, method);
            }

            if (retValType.IsValueType)
            {
                // If the return type is blittable, we can assign it directly to the target address.
                // However, we must use the correct indirect store instruction for primitive types.
                if (retValType.IsBlittable(interopReferences))
                {
                    body.Instructions.ReferenceReplaceRange(marker, [CilInstruction.CreateStind(retValType, module)]);
                }
                else if (retValType.IsConstructedKeyValuePairType(interopReferences))
                {
                    // If the type is some constructed 'KeyValuePair<,>' type, we use the generated marshaller
                    RewriteBody(
                        body: body,
                        marker: marker,
                        marshallerMethod: emitState.LookupTypeDefinition(retValType, "Marshaller").GetMethod("ConvertToUnmanaged"),
                        interopReferences: interopReferences,
                        module: module);
                }
                else if (retValType.IsConstructedNullableValueType(interopReferences))
                {
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(retValType, interopReferences, emitState);

                    // Emit code similar to 'KeyValuePair<,>' above, to marshal the resulting 'Nullable<T>' value
                    RewriteBody(
                        body: body,
                        marker: marker,
                        marshallerMethod: marshallerType.BoxToUnmanaged(),
                        interopReferences: interopReferences,
                        module: module);
                }
                else
                {
                    // For all other struct types, we just always defer to their generated marshaller type
                    InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(retValType, interopReferences, emitState);

                    // Delegate to the marshaller to convert the managed value type on the evaluation stack
                    body.Instructions.ReferenceReplaceRange(marker, [
                        new CilInstruction(Call, marshallerType.ConvertToUnmanaged()),
                        new CilInstruction(Stobj, retValType.GetAbiType(interopReferences).ToTypeDefOrRef())]);
                }
            }
            else if (retValType.IsTypeOfString())
            {
                // When marshalling 'string' values, we must use 'HStringMarshaller'. Note that the
                // 'HStringMarshaller.ConvertToUnmanaged' method actually takes a 'ReadOnlySpan<char>',
                // so we first also need to create one from the 'string' value loaded on the stack.
                body.Instructions.ReferenceReplaceRange(marker, [
                    new CilInstruction(Call, interopReferences.MemoryExtensionsAsSpanCharString),
                    new CilInstruction(Call, interopReferences.HStringMarshallerConvertToUnmanaged),
                    new CilInstruction(Stind_I)]);
            }
            else if (retValType.IsTypeOfType(interopReferences))
            {
                // 'Type' values also need their own specialized marshaller
                body.Instructions.ReferenceReplaceRange(marker, [
                    new CilInstruction(Call, interopReferences.TypeMarshallerConvertToUnmanaged),
                    new CilInstruction(Stobj, interopReferences.AbiType)]);
            }
            else if (retValType.IsTypeOfException(interopReferences))
            {
                // 'Exception' is also special, and needs its own specialized marshaller
                body.Instructions.ReferenceReplaceRange(marker, [
                    new CilInstruction(Call, interopReferences.ExceptionMarshallerConvertToUnmanaged),
                    new CilInstruction(Stobj, interopReferences.AbiException)]);
            }
            else
            {
                // Get the marshaller type for either generic reference types, or all other reference types
                InteropMarshallerType marshallerType = InteropMarshallerTypeResolver.GetMarshallerType(retValType, interopReferences, emitState);

                // Marshal the value and assign it to the target location
                RewriteBody(
                    body: body,
                    marker: marker,
                    marshallerMethod: marshallerType.ConvertToUnmanaged(),
                    interopReferences: interopReferences,
                    module: module);
            }
        }

        /// <inheritdoc cref="RewriteMethod"/>
        /// <param name="body">The target body to perform two-pass code generation on.</param>
        /// <param name="marshallerMethod">The method to invoke to marshal the unmanaged value.</param>
        private static void RewriteBody(
            CilMethodBody body,
            CilInstruction marker,
            IMethodDefOrRef marshallerMethod,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            // We need a new local for the 'WindowsRuntimeObjectReferenceValue' returned from the
            // marshalling methods that the code will invoke. This is because we are going to call
            // the 'DetachThisPtrUnsafe()' method on it, which needs 'this' by reference.
            CilLocalVariable loc_returnValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());

            body.LocalVariables.Add(loc_returnValue);

            // Marshal the value and detach its native pointer before assigning it to the target location
            body.Instructions.ReferenceReplaceRange(marker, [
                new CilInstruction(Call, marshallerMethod),
                CilInstruction.CreateStloc(loc_returnValue, body),
                new CilInstruction(Ldloca_S, loc_returnValue),
                new CilInstruction(Call, interopReferences.WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe),
                new CilInstruction(Stind_I)]);
        }
    }
}
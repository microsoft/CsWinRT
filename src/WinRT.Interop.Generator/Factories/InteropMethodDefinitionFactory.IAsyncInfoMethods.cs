// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for method types for interfaces deriving from <c>Windows.Foundation.IAsyncInfo</c>.
    /// </summary>
    public static class IAsyncInfoMethods
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for a get method for a handler delegate of a specified type.
        /// </summary>
        /// <param name="methodName">The name of the get method.</param>
        /// <param name="handlerType">The type of the handler delegate.</param>
        /// <param name="vftblField">The vtable field definition for the interface slot to invoke.</param>
        /// <param name="convertToManagedMethod">The marshalling method to convert the handler delegate native pointer.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        public static MethodDefinition get_Handler(
            Utf8String methodName,
            TypeSignature handlerType,
            FieldDefinition vftblField,
            MethodDefinition convertToManagedMethod,
            InteropReferences interopReferences)
        {
            // Define the 'Handler' get method as follows:
            //
            // public static <HANDLER_TYPE> <METHOD_NAME>(WindowsRuntimeObjectReference thisReference)
            MethodDefinition handlerMethod = new(
                name: methodName,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: handlerType,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'void*' (the handler pointer that was retrieved)
            //   [3]: '<HANDLER_TYPE>' (the marshalled handler)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(interopReferences.Void.MakePointerType());
            CilLocalVariable loc_2_handlerPtr = new(interopReferences.Void.MakePointerType());
            CilLocalVariable loc_3_handler = new(handlerType);

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction ldloc_2_tryStart = new(Ldloc_2);
            CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
            CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

            // Create a method body for the 'Handler' method
            handlerMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_handlerPtr, loc_3_handler },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue },
                    { Stloc_0 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldloca_S, loc_2_handlerPtr },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblField },
                    { Calli, WellKnownTypeSignatureFactory.get_Handler(interopReferences).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
                    { Endfinally },
                    { nop_finallyEnd },

                    // '.try/.finally' code to marshal the handler
                    { ldloc_2_tryStart },
                    { Call, convertToManagedMethod },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_finallyEnd.CreateLabel() },
                    { ldloc_2_finallyStart },
                    { Call, interopReferences.WindowsRuntimeUnknownMarshallerFree },
                    { Endfinally },
                    { ldloc_3_finallyEnd },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloca_s_0_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerEnd = nop_finallyEnd.CreateLabel()
                    },
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloc_2_tryStart.CreateLabel(),
                        TryEnd = ldloc_2_finallyStart.CreateLabel(),
                        HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                        HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                    }
                }
            };

            return handlerMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for a set method for a handler delegate of a specified type.
        /// </summary>
        /// <param name="methodName">The name of the set method.</param>
        /// <param name="handlerType">The type of the handler delegate.</param>
        /// <param name="vftblField">The vtable field definition for the interface slot to invoke.</param>
        /// <param name="convertToUnmanagedMethod">The marshalling method to convert the handler delegate managed object.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        public static MethodDefinition set_Handler(
            Utf8String methodName,
            TypeSignature handlerType,
            FieldDefinition vftblField,
            MethodDefinition convertToUnmanagedMethod,
            InteropReferences interopReferences)
        {
            // Define the 'Handler' set method as follows:
            //
            // public static void <METHOD_NAME>(WindowsRuntimeObjectReference thisReference, <HANDLER_TYPE> handler)
            MethodDefinition handlerMethod = new(
                name: methodName,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                        handlerType]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'WindowsRuntimeObjectReferenceValue' (for 'handlerValue')
            //   [2]: 'void*' (for 'thisPtr')
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_handlerValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_2_thisPtr = new(interopReferences.Void.MakePointerType());

            // Jump labels
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_1_finallyStart = new(Ldloca_S, loc_1_handlerValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ret_finallyEnd = new(Ret);

            // Create a method body for the 'Handler' method
            handlerMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_handlerValue, loc_2_thisPtr },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue },
                    { Stloc_0 },

                    // Initialize 'handlerValue'
                    { ldarg_1_tryStart },
                    { Call, convertToUnmanagedMethod },
                    { Stloc_1 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Stloc_2 },
                    { Ldloc_2 },
                    { Ldloca_S, loc_1_handlerValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Ldloc_2 },
                    { Ldind_I },
                    { Ldfld, vftblField },
                    { Calli, WellKnownTypeSignatureFactory.set_Handler(interopReferences).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, ret_finallyEnd.CreateLabel() },

                    // '.finally' code (for 'handlerValue')
                    { ldloca_s_1_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
                    { Endfinally },

                    // '.finally' code (for 'thisValue')
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
                    { Endfinally },

                    // Return (after both '.finally' blocks)
                    { ret_finallyEnd }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldarg_1_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerEnd = ret_finallyEnd.CreateLabel()
                    },
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloca_s_0_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_1_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_1_finallyStart.CreateLabel(),
                        HandlerEnd = ldloca_s_0_finallyStart.CreateLabel()
                    }
                }
            };

            return handlerMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetResults</c> method of some async operation type.
        /// </summary>
        /// <param name="resultType">The result type for the async operation type.</param>
        /// <param name="vftblField">The vtable field definition for the interface slot to invoke.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition GetResults(
            TypeSignature resultType,
            FieldDefinition vftblField,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            // Define the 'GetResults' get method as follows:
            //
            // public static <RESULT_TYPE> GetResults(WindowsRuntimeObjectReference thisReference)
            MethodDefinition getResultsMethod = new(
                name: "GetResults"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: resultType,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: '<ABI_RESULT_TYPE>' (the ABI type for the type argument)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(interopReferences.Void.MakePointerType());
            CilLocalVariable loc_2_resultNative = new(resultType.GetAbiType(interopReferences));

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction nop_returnValueRewrite = new(Nop);

            // Create a method body for the 'GetResults' method
            getResultsMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_resultNative },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue },
                    { Stloc_0 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldarg_1 },
                    { Ldloca_S, loc_2_resultNative },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblField },
                    { Calli, WellKnownTypeSignatureFactory.get_TypedRetVal(resultType.GetAbiType(interopReferences).MakePointerType(), interopReferences).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
                    { Endfinally },
                    { nop_finallyEnd },
                    { nop_returnValueRewrite }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloca_s_0_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerEnd = nop_finallyEnd.CreateLabel()
                    }
                }
            };

            // Track rewriting the return value for this method
            emitState.TrackReturnValueMethodRewrite(
                returnType: resultType,
                method: getResultsMethod,
                marker: nop_returnValueRewrite,
                source: loc_2_resultNative);

            return getResultsMethod;
        }
    }
}
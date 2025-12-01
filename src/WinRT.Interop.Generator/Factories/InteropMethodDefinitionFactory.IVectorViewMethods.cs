// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for method types for constructed <c>IVectorView&lt;T&gt;</c> interfaces.
    /// </summary>
    public static class IVectorViewMethods
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetAt</c> method for some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <remarks>
        /// This method can also be used to define the <c>GetAt</c> method for <c>IVector&lt;T&gt;</c> interfaces.
        /// </remarks>
        public static MethodDefinition GetAt(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Define the 'GetAt' method as follows:
            //
            // public static <TYPE_ARGUMENT> GetAt(WindowsRuntimeObjectReference thisReference, uint index)
            MethodDefinition getAtMethod = new(
                name: "GetAt"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                        interopReferences.UInt32]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: '<ABI_TYPE_ARGUMENT>' (the ABI type for the type argument)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(interopReferences.Void.MakePointerType());
            CilLocalVariable loc_2_resultNative = new(elementType.GetAbiType(interopReferences));

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction nop_returnValueRewrite = new(Nop);

            // Create a method body for the 'GetAt' method
            getAtMethod.CilMethodBody = new CilMethodBody()
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
                    { Ldfld, vftblType.GetField("GetAt"u8) },

                    // This 'calli' instruction is always using 'IReadOnlyList1GetAtImpl', but the signature for
                    // the vtable slot for 'GetAt' for 'IVector<T>' is identical, so doing so is safe in this case.
                    { Calli, WellKnownTypeSignatureFactory.IReadOnlyList1GetAtImpl(elementType, interopReferences).MakeStandAloneSignature() },
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
                returnType: elementType,
                method: getAtMethod,
                marker: nop_returnValueRewrite,
                source: loc_2_resultNative);

            return getAtMethod;
        }
    }
}
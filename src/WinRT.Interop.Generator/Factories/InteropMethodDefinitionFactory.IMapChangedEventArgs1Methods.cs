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
    /// Helpers for method types for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> interfaces.
    /// </summary>
    public static class IMapChangedEventArgs1Methods
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Key</c> export method.
        /// </summary>
        /// <param name="argsType">The <see cref="TypeSignature"/> for the args type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Key(
            GenericInstanceTypeSignature argsType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = argsType.TypeArguments[0];
            TypeSignature keyAbiType = keyType.GetAbiType(interopReferences);

            // Define the 'Key' method as follows:
            //
            // public static <TYPE_ARGUMENT> Key(WindowsRuntimeObjectReference thisReference)
            MethodDefinition keyMethod = new(
                name: "Key"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: keyType,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: '<ABI_KEY_TYPE>' (the native value that was retrieved)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_keyNative = new(keyAbiType);

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction nop_returnValueRewrite = new(Nop);

            // Create a method body for the 'Key' method
            keyMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_keyNative },
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
                    { Ldloca_S, loc_2_keyNative },
                    { Conv_U },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.IMapChangedEventArgsVftbl.GetField("get_Key"u8) },
                    { Calli, WellKnownTypeSignatureFactory.get_UntypedRetVal(interopReferences).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
                    { Endfinally },
                    { nop_finallyEnd },

                    // Marshal and return the result
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
                returnType: keyType,
                method: keyMethod,
                marker: nop_returnValueRewrite,
                source: loc_2_keyNative);

            return keyMethod;
        }
    }
}
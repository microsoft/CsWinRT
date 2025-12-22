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
    /// Helpers for method types for constructed <c>IMap&lt;K, V&gt;</c> interfaces.
    /// </summary>
    public static class IMapMethods
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Insert</c> method for some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="dictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Insert(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];
            TypeSignature keyAbiType = keyType.GetAbiType(interopReferences);
            TypeSignature valueAbiType = valueType.GetAbiType(interopReferences);

            // Define the 'Insert' method as follows:
            //
            // public static bool Insert(WindowsRuntimeObjectReference objectReference, <KEY_TYPE> key, <VALUE_TYPE> value)
            MethodDefinition insertMethod = new(
                name: "Insert"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module),
                        keyType.Import(module),
                        valueType.Import(module)]))
            {
                NoInlining = true
            };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'bool' (for 'result')
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_result = new(interopReferences.CorLibTypeFactory.Boolean);

            // Jump labels
            CilInstruction nop_try_this = new(Nop);
            CilInstruction nop_try_key = new(Nop);
            CilInstruction nop_try_value = new(Nop);
            CilInstruction nop_ld_key = new(Nop);
            CilInstruction nop_ld_value = new(Nop);
            CilInstruction nop_finally_key = new(Nop);
            CilInstruction nop_finally_value = new(Nop);
            CilInstruction ldloca_0_finally_0 = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloc_2_finally_end_this = new(Ldloc_2);

            // Create a method body for the 'Insert' method
            insertMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_result },
                Instructions =
                {
                    // Load the local [0]
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },
                    { nop_try_this },

                    // Arguments loading inside outer 'try/finally' block
                    { nop_try_key },
                    { nop_try_value },

                    // 'Insert' call for the native delegate (and 'try' for local [2])
                    { Ldloca_S, loc_0_thisValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { nop_ld_key },
                    { nop_ld_value },
                    { Ldloca_S, loc_2_result },
                    { Conv_U },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField("Insert"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IDictionary2InsertImpl(keyAbiType, valueAbiType, interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, ldloc_2_finally_end_this.CreateLabel() },

                    // Optional 'finally' blocks for the marshalled parameters. These are intentionally
                    // in reverse order, as the inner-most parameter should be released first.
                    { nop_finally_value },
                    { nop_finally_key },

                    // 'finally' for local [0]
                    { ldloca_0_finally_0 },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },

                    // return result;
                    { ldloc_2_finally_end_this },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    // Setup 'try/finally' for local [0]
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = nop_try_this.CreateLabel(),
                        TryEnd = ldloca_0_finally_0.CreateLabel(),
                        HandlerStart = ldloca_0_finally_0.CreateLabel(),
                        HandlerEnd = ldloc_2_finally_end_this.CreateLabel()
                    }
                }
            };

            // Track rewriting the two parameters for this method
            emitState.TrackNativeParameterMethodRewrite(
                paraneterType: keyType,
                method: insertMethod,
                tryMarker: nop_try_key,
                loadMarker: nop_ld_key,
                finallyMarker: nop_finally_key,
                parameterIndex: 1);

            emitState.TrackNativeParameterMethodRewrite(
                paraneterType: valueType,
                method: insertMethod,
                tryMarker: nop_try_value,
                loadMarker: nop_ld_value,
                finallyMarker: nop_finally_value,
                parameterIndex: 2);

            return insertMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Remove</c> method for some <c>IMaplt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="dictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Remove(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature keyAbiType = keyType.GetAbiType(interopReferences);

            // Define the 'Remove' method as follows:
            //
            // public static bool Remove(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition removeMethod = new(
                name: "Remove"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'bool' (for 'result')
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature().Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_result = new(interopReferences.CorLibTypeFactory.Boolean);

            // Jump labels
            CilInstruction nop_try_this = new(Nop);
            CilInstruction nop_try_key = new(Nop);
            CilInstruction nop_ld_key = new(Nop);
            CilInstruction nop_finally_key = new(Nop);
            CilInstruction ldloca_s_0_finally_this = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloc_2_finally_end_this = new(Ldloc_2);

            // Create a method body for the 'Remove' method
            removeMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_result },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },
                    { nop_try_this },

                    // Load the key, possibly inside a 'try/finally' block
                    { nop_try_key },

                    // '.try' code
                    { Ldloca_S, loc_0_thisValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { nop_ld_key },
                    { Ldloca_S, loc_2_result },
                    { Conv_U },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField("Remove"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IDictionary2RemoveImpl(keyAbiType, interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, ldloc_2_finally_end_this.CreateLabel() },

                    // Optional 'finally' block for the marshalled key
                    { nop_finally_key },

                    // '.finally' code
                    { ldloca_s_0_finally_this },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },

                    // return result;
                    { ldloc_2_finally_end_this },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = nop_try_this.CreateLabel(),
                        TryEnd = ldloca_s_0_finally_this.CreateLabel(),
                        HandlerStart = ldloca_s_0_finally_this.CreateLabel(),
                        HandlerEnd = ldloc_2_finally_end_this.CreateLabel()
                    }
                }
            };

            // Track rewriting the return value for this method
            emitState.TrackNativeParameterMethodRewrite(
                paraneterType: keyType,
                method: removeMethod,
                tryMarker: nop_try_key,
                loadMarker: nop_ld_key,
                finallyMarker: nop_finally_key,
                parameterIndex: 1);

            return removeMethod;
        }
    }
}
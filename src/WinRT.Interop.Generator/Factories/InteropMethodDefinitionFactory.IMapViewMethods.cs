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
    /// Helpers for method types for constructed <c>IMapView&lt;K, V&gt;</c> interfaces.
    /// </summary>
    public static class IMapViewMethods
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>HasKey</c> method for some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <remarks>
        /// This method can also be used to define the <c>HasKey</c> method for <c>IMap&lt;K, V&gt;</c> interfaces.
        /// </remarks>
        public static MethodDefinition HasKey(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature keyAbiType = keyType.GetAbiType(interopReferences);

            // Define the 'HasKey' method as follows:
            //
            // public static bool HasKey(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition hasKeyMethod = new(
                name: "HasKey"u8,
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

            // Create a method body for the 'HasKey' method
            hasKeyMethod.CilMethodBody = new CilMethodBody()
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
                    { Ldfld, vftblType.GetField("HasKey"u8) },

                    // This 'calli' instruction is always using 'IReadOnlyDictionary2HasKeyImpl', but the signature for
                    // the vtable slot for 'HasKey' for 'IMap<K,V>' is identical, so doing so is safe in this case.
                    { Calli, WellKnownTypeSignatureFactory.IReadOnlyDictionary2HasKeyImpl(keyAbiType, interopReferences).Import(module).MakeStandAloneSignature() },
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
                method: hasKeyMethod,
                tryMarker: nop_try_key,
                loadMarker: nop_ld_key,
                finallyMarker: nop_finally_key,
                parameterIndex: 1);

            return hasKeyMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Lookup</c> method for some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <remarks>
        /// This method can also be used to define the <c>Lookup</c> method for <c>IMap&lt;K, V&gt;</c> interfaces.
        /// </remarks>
        public static MethodDefinition Lookup(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];
            TypeSignature keyAbiType = keyType.GetAbiType(interopReferences);
            TypeSignature valueAbiType = valueType.GetAbiType(interopReferences);

            // Define the 'Lookup' method as follows:
            //
            // public static <VALUE_TYPE> Lookup(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition lookupMethod = new(
                name: "Lookup"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: valueType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: <ABI_VALUE_TYPE> (for 'resultNative')
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature().Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_resultNative = new(valueAbiType.Import(module));

            // Jump labels
            CilInstruction nop_try_this = new(Nop);
            CilInstruction nop_try_key = new(Nop);
            CilInstruction nop_ld_key = new(Nop);
            CilInstruction nop_finally_key = new(Nop);
            CilInstruction ldloca_s_0_finally_this = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finally_end_this = new(Nop);
            CilInstruction nop_returnValueRewrite = new(Nop);

            // Create a method body for the 'Lookup' method
            lookupMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_resultNative },
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
                    { Ldloca_S, loc_2_resultNative },
                    { Conv_U },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField("Lookup"u8) },

                    // This 'calli' instruction is always using 'IReadOnlyDictionary2LookupImpl', but the signature for
                    // the vtable slot for 'HasKey' for 'IMap<K,V>' is identical, so doing so is safe in this case.
                    { Calli, WellKnownTypeSignatureFactory.IReadOnlyDictionary2LookupImpl(keyAbiType, valueAbiType, interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, nop_finally_end_this.CreateLabel() },

                    // Optional 'finally' block for the marshalled key
                    { nop_finally_key },

                    // '.finally' code
                    { ldloca_s_0_finally_this },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },

                    // Marshal and return the result
                    { nop_finally_end_this },
                    { nop_returnValueRewrite }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = nop_try_this.CreateLabel(),
                        TryEnd = ldloca_s_0_finally_this.CreateLabel(),
                        HandlerStart = ldloca_s_0_finally_this.CreateLabel(),
                        HandlerEnd = nop_finally_end_this.CreateLabel()
                    }
                }
            };

            // Track rewriting the 'key' parameter for this method
            emitState.TrackNativeParameterMethodRewrite(
                paraneterType: keyType,
                method: lookupMethod,
                tryMarker: nop_try_key,
                loadMarker: nop_ld_key,
                finallyMarker: nop_finally_key,
                parameterIndex: 1);

            // Track rewriting the return value for this method
            emitState.TrackReturnValueMethodRewrite(
                returnType: valueType,
                method: lookupMethod,
                marker: nop_returnValueRewrite,
                source: loc_2_resultNative);

            return lookupMethod;
        }
    }
}
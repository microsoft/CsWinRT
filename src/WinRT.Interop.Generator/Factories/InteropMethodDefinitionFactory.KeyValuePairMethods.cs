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
    /// Helpers for accessor methods for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class KeyValuePairMethods
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Key</c> or <c>get_Value</c> accessor method for some <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="keyOrValueType">The result type to return (must be either the key or value type from <paramref name="keyValuePairType"/>).</param>
        /// <param name="vftblType">The vtable type for <paramref name="keyValuePairType"/>.</param>
        /// <param name="vftblMethodName">The name of the vtable method to invoke.</param>
        /// <param name="accessorMethodName">The name of the accessor method to create.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition get_KeyOrValue(
            GenericInstanceTypeSignature keyValuePairType,
            TypeSignature keyOrValueType,
            TypeDefinition vftblType,
            Utf8String vftblMethodName,
            Utf8String accessorMethodName,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            // Define the accessor method as follows:
            //
            // public static <RESULT_TYPE> <ACCESSOR_METHOD_NAME>(void* thisPtr)
            MethodDefinition get_KeyOrValueMethod = new(
                name: accessorMethodName,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: keyOrValueType.Import(module),
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: '<ABI_RESULT_TYPE>' (the ABI type for the result type)
            CilLocalVariable loc_0_resultNative = new(keyOrValueType.GetAbiType(interopReferences).Import(module));

            // Jump labels
            CilInstruction nop_returnValueRewrite = new(Nop);

            // Create a method body for the 'GetAt' method
            get_KeyOrValueMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_resultNative },
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldloca_S, loc_0_resultNative },
                    { Ldarg_0 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField(vftblMethodName) },
                    { Calli, WellKnownTypeSignatureFactory.get_UntypedRetVal(interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { nop_returnValueRewrite }
                }
            };

            // Track rewriting the return value for this method
            emitState.TrackReturnValueMethodRewrite(
                returnType: keyOrValueType,
                method: get_KeyOrValueMethod,
                marker: nop_returnValueRewrite,
                source: loc_0_resultNative);

            return get_KeyOrValueMethod;
        }
    }
}
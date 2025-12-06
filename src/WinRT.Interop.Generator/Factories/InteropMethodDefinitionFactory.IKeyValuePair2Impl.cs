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
    /// Helpers for impl types for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class IKeyValuePair2Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Key</c> export method.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition get_Key(
            GenericInstanceTypeSignature keyValuePairType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            return get_KeyOrValue(
                keyValuePairType: keyValuePairType,
                keyOrValueType: keyValuePairType.TypeArguments[0],
                methodName: "get_Key"u8,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Value</c> export method.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition get_Value(
            GenericInstanceTypeSignature keyValuePairType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            return get_KeyOrValue(
                keyValuePairType: keyValuePairType,
                keyOrValueType: keyValuePairType.TypeArguments[1],
                methodName: "get_Value"u8,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Key</c> or <c>get_Value</c> export method.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="keyOrValueType">The <see cref="TypeSignature"/> for the result value.</param>
        /// <param name="methodName">The name of the method to generate.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        private static MethodDefinition get_KeyOrValue(
            GenericInstanceTypeSignature keyValuePairType,
            TypeSignature keyOrValueType,
            Utf8String methodName,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            // Define the method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int <METHOD_NAME>(void* thisPtr, <ABI_KEY_OR_VALUE_TYPE>* key)
            MethodDefinition get_KeyOrValueMethod = new(
                name: methodName,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        keyOrValueType.GetAbiType(interopReferences).Import(module).MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Reference the 'KeyValuePair<,>' accessor method
            MemberReference get_KeyOrValueAccessorMethod = keyValuePairType
                .Import(module)
                .ToTypeDefOrRef()
                .CreateMemberReference(
                    memberName: methodName,
                    signature: MethodSignature.CreateInstance(new GenericParameterSignature(
                        parameterType: GenericParameterType.Type,
                        index: keyValuePairType.TypeArguments.IndexOf(keyOrValueType))));

            // Declare the local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'KeyValuePair<,>' (the boxed object to get values from)
            CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);
            CilLocalVariable loc_1_keyValuePair = new(keyValuePairType.Import(module));

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
            CilInstruction nop_convertToUnmanaged = new(Nop);

            // Create a method body for the native export method
            get_KeyOrValueMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_hresult, loc_1_keyValuePair },
                Instructions =
                {
                    // Return 'E_POINTER' if the argument is 'null'
                    { Ldarg_1 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { Ldc_I4, unchecked((int)0x80004003) },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_1_tryStart },
                    { Ldarg_0 },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(module.CorLibTypeFactory.Object).Import(module) },
                    { Unbox_Any, keyValuePairType.Import(module).ToTypeDefOrRef() },
                    { Stloc_1 },
                    { Ldarg_1 },
                    { Ldloca_S, loc_1_keyValuePair },
                    { Call, get_KeyOrValueAccessorMethod },
                    { nop_convertToUnmanaged },
                    { Ldc_I4_0 },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [0]
                    { ldloc_0_returnHResult  },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Exception,
                        TryStart = ldarg_1_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            // Track the method for rewrite to marshal the result value
            emitState.TrackRetValValueMethodRewrite(
                retValType: keyOrValueType,
                method: get_KeyOrValueMethod,
                marker: nop_convertToUnmanaged);

            return get_KeyOrValueMethod;
        }
    }
}
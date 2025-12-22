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

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for method types for constructed <c>IVector&lt;T&gt;</c> interfaces.
    /// </summary>
    public static class IVectorMethods
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>SetAt</c> method for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition SetAt(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "SetAt"u8,
                methodSignature: WellKnownTypeSignatureFactory.IList1SetAtImpl(elementType.GetAbiType(interopReferences), interopReferences),
                listType: listType,
                vftblType: vftblType,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>InsertAt</c> method for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition InsertAt(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "InsertAt"u8,
                methodSignature: WellKnownTypeSignatureFactory.IList1InsertAtImpl(elementType.GetAbiType(interopReferences), interopReferences),
                listType: listType,
                vftblType: vftblType,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>SetAt</c> or <c>InsertAt</c> method for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="methodName">The name of the method to create.</param>
        /// <param name="methodSignature">The signature of the method to create.</param>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        private static MethodDefinition SetAtOrInsertAt(
            Utf8String methodName,
            MethodSignature methodSignature,
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'SetAt' or 'InsertAt' method as follows:
            //
            // public static void <METHOD_NAME>(WindowsRuntimeObjectReference thisReference, uint index, <ELEMENT_TYPE> value)
            MethodDefinition setAtOrInsertAtMethod = new(
                name: methodName,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        interopReferences.CorLibTypeFactory.UInt32,
                        elementType.Import(module)]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature().Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());

            // Jump labels
            CilInstruction nop_try_this = new(Nop);
            CilInstruction nop_try_value = new(Nop);
            CilInstruction nop_ld_value = new(Nop);
            CilInstruction nop_finally_value = new(Nop);
            CilInstruction ldloca_s_0_finally_this = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ret_finally_end_this = new(Ret);

            // Create a method body for the method
            setAtOrInsertAtMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },
                    { nop_try_this },

                    // Load the value, possibly inside a 'try/finally' block
                    { nop_try_value },

                    // '.try' code
                    { Ldloca_S, loc_0_thisValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldarg_1 },
                    { nop_ld_value },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField(methodName) },
                    { Calli, methodSignature.Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, ret_finally_end_this.CreateLabel() },

                    // Optional 'finally' block for the marshalled key
                    { nop_finally_value },

                    // '.finally' code
                    { ldloca_s_0_finally_this },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },

                    // return result;
                    { ret_finally_end_this }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = nop_try_this.CreateLabel(),
                        TryEnd = ldloca_s_0_finally_this.CreateLabel(),
                        HandlerStart = ldloca_s_0_finally_this.CreateLabel(),
                        HandlerEnd = ret_finally_end_this.CreateLabel()
                    }
                }
            };

            // Track rewriting the return value for this method
            emitState.TrackNativeParameterMethodRewrite(
                paraneterType: elementType,
                method: setAtOrInsertAtMethod,
                tryMarker: nop_try_value,
                loadMarker: nop_ld_value,
                finallyMarker: nop_finally_value,
                parameterIndex: 2);

            return setAtOrInsertAtMethod;
        }
    }
}
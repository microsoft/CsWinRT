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
        public static MethodDefinition SetAt(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "SetAt"u8,
                methodSignature: WellKnownTypeSignatureFactory.IList1SetAtImpl(elementType.GetAbiType(interopReferences), interopReferences),
                listType: listType,
                vftblType: vftblType,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>InsertAt</c> method for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition InsertAt(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "InsertAt"u8,
                methodSignature: WellKnownTypeSignatureFactory.IList1InsertAtImpl(elementType.GetAbiType(interopReferences), interopReferences),
                listType: listType,
                vftblType: vftblType,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Append</c> method for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition Append(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Define the 'Append' method as follows:
            //
            // public static void Append(WindowsRuntimeObjectReference thisReference, <ELEMENT_TYPE> value)
            MethodDefinition appendMethod = new(
                name: "Append"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                        elementType]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(interopReferences.Void.MakePointerType());

            // Jump labels
            CilInstruction nop_try_this = new(Nop);
            CilInstruction nop_try_value = new(Nop);
            CilInstruction nop_ld_value = new(Nop);
            CilInstruction nop_finally_value = new(Nop);
            CilInstruction ldloca_s_0_finally_this = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ret_finally_end_this = new(Ret);

            // Create a method body for the 'Append' method
            appendMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue },
                    { Stloc_0 },
                    { nop_try_this },

                    // Load the value, possibly inside a 'try/finally' block
                    { nop_try_value },

                    // '.try' code
                    { Ldloca_S, loc_0_thisValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { nop_ld_value },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField("Append"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IList1AppendImpl(elementAbiType, interopReferences).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, ret_finally_end_this.CreateLabel() },

                    // Optional 'finally' block for the marshalled key
                    { nop_finally_value },

                    // '.finally' code
                    { ldloca_s_0_finally_this },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
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

            // Track rewriting the 'value' parameter for this method
            emitState.TrackNativeParameterMethodRewrite(
                parameterType: elementType,
                method: appendMethod,
                tryMarker: nop_try_value,
                loadMarker: nop_ld_value,
                finallyMarker: nop_finally_value,
                parameterIndex: 1);

            return appendMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>IndexOf</c> method for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The vtable type for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition IndexOf(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Define the 'IndexOf' method as follows:
            //
            // public static bool IndexOf(WindowsRuntimeObjectReference thisReference, <ELEMENT_TYPE> value, out uint index)
            MethodDefinition indexOfMethod = new(
                name: "IndexOf"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                        elementType,
                        interopReferences.CorLibTypeFactory.UInt32.MakeByReferenceType()]))
            {
                NoInlining = true,
                CilOutParameterIndices = [3]
            };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'ref uint' (for the pinned 'out uint' parameter)
            //   [3]: 'bool' (for the return value)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(interopReferences.Void.MakePointerType());
            CilLocalVariable loc_2_pinnedIndex = new(interopReferences.CorLibTypeFactory.UInt32.MakeByReferenceType().MakePinnedType());
            CilLocalVariable loc_3_result = new(interopReferences.CorLibTypeFactory.Boolean);

            // Jump labels
            CilInstruction nop_try_this = new(Nop);
            CilInstruction nop_try_value = new(Nop);
            CilInstruction nop_ld_value = new(Nop);
            CilInstruction nop_finally_value = new(Nop);
            CilInstruction ldloca_s_0_finally_this = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloc_3_finally_end_this = new(Ldloc_3);

            // Create a method body for the 'IndexOf' method
            indexOfMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_pinnedIndex, loc_3_result },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue },
                    { Stloc_0 },
                    { nop_try_this },

                    // Load the value, possibly inside a 'try/finally' block
                    { nop_try_value },

                    // fixed (char* p = index) { }
                    { Ldarg_2 },
                    { Stloc_2 },

                    // '.try' code
                    { Ldloca_S, loc_0_thisValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { nop_ld_value },
                    { Ldloc_2 },
                    { Conv_U },
                    { Ldloca_S, loc_3_result },
                    { Conv_U },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField("IndexOf"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IList1IndexOfImpl(elementAbiType, interopReferences).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, ldloc_3_finally_end_this.CreateLabel() },

                    // Optional 'finally' block for the marshalled key. We could have another stub here to unpin
                    // the pinned 'index' parameter, but because we're immediately returning, we can just skip it.
                    // The ECMA-335 spec doesn't require pinned locals to be unpinned before the containing method
                    // returns. In the worst case scenario, objects will be kept alive longer, but here we're just
                    // immediately returning right after the previous statement here, so it doesn't really matter.
                    { nop_finally_value },

                    // '.finally' code
                    { ldloca_s_0_finally_this },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
                    { Endfinally },

                    // return result;
                    { ldloc_3_finally_end_this },
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
                        HandlerEnd = ldloc_3_finally_end_this.CreateLabel()
                    }
                }
            };

            // Track rewriting the 'value' parameter for this method
            emitState.TrackNativeParameterMethodRewrite(
                parameterType: elementType,
                method: indexOfMethod,
                tryMarker: nop_try_value,
                loadMarker: nop_ld_value,
                finallyMarker: nop_finally_value,
                parameterIndex: 1);

            return indexOfMethod;
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
        private static MethodDefinition SetAtOrInsertAt(
            Utf8String methodName,
            MethodSignature methodSignature,
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'SetAt' or 'InsertAt' method as follows:
            //
            // public static void <METHOD_NAME>(WindowsRuntimeObjectReference thisReference, uint index, <ELEMENT_TYPE> value)
            MethodDefinition setAtOrInsertAtMethod = new(
                name: methodName,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                        interopReferences.CorLibTypeFactory.UInt32,
                        elementType]))
            { NoInlining = true };

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(interopReferences.Void.MakePointerType());

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
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue },
                    { Stloc_0 },
                    { nop_try_this },

                    // Load the value, possibly inside a 'try/finally' block
                    { nop_try_value },

                    // '.try' code
                    { Ldloca_S, loc_0_thisValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldarg_1 },
                    { nop_ld_value },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField(methodName) },
                    { Calli, methodSignature.MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, ret_finally_end_this.CreateLabel() },

                    // Optional 'finally' block for the marshalled key
                    { nop_finally_value },

                    // '.finally' code
                    { ldloca_s_0_finally_this },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
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

            // Track rewriting the 'value' parameter for this method
            emitState.TrackNativeParameterMethodRewrite(
                parameterType: elementType,
                method: setAtOrInsertAtMethod,
                tryMarker: nop_try_value,
                loadMarker: nop_ld_value,
                finallyMarker: nop_finally_value,
                parameterIndex: 2);

            return setAtOrInsertAtMethod;
        }
    }
}
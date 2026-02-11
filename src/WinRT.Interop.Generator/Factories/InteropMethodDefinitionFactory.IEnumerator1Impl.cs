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
    /// Helpers for impl types for <see cref="System.Collections.Generic.IEnumerator{T}"/> interfaces.
    /// </summary>
    public static class IEnumerator1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_HasCurrent</c> export method.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition get_HasCurrent(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            return HasCurrentOrMoveNext(
                methodName: "get_HasCurrent"u8,
                adapterMethod: interopReferences.IEnumeratorAdapter1get_HasCurrent(elementType),
                enumeratorType,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>MoveNext</c> export method.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition MoveNext(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            return HasCurrentOrMoveNext(
                methodName: "MoveNext"u8,
                adapterMethod: interopReferences.IEnumeratorAdapter1MoveNext(elementType),
                enumeratorType,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Current</c> export method.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition get_Current(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // Define the 'get_Current' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Current(void* thisPtr, <ABI_ELEMENT_TYPE>* result)
            MethodDefinition currentMethod = new(
                name: "get_Current"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        elementType.GetAbiType(interopReferences).MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);
            CilInstruction nop_convertToUnmanaged = new(Nop);

            // Create a method body for the 'get_Current' method
            currentMethod.CilMethodBody = new CilMethodBody()
            {
                // Declare 1 variable:
                //   [0]: 'int' (the 'HRESULT' to return)
                LocalVariables = { new CilLocalVariable(module.CorLibTypeFactory.Int32) },
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(enumeratorType) },
                    { Call, interopReferences.IEnumeratorAdapter1GetInstance(elementType) },
                    { Callvirt, interopReferences.IEnumeratorAdapter1get_Current(elementType) },
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
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            // Track the method for rewrite to marshal the result value
            emitState.TrackRetValValueMethodRewrite(
                retValType: elementType,
                method: currentMethod,
                marker: nop_convertToUnmanaged);

            return currentMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetMany</c> export method.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition GetMany(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // Get the appropriate 'GetMany' method descriptor to delegate the logic to
            IMethodDescriptor getManyMethod = elementType switch
            {
                _ when elementType.IsBlittable(interopReferences) => interopReferences.IEnumeratorAdapterBlittableValueTypeGetMany(elementType),
                _ when elementType.IsConstructedKeyValuePairType(interopReferences) => interopReferences.IEnumeratorAdapterKeyValuePairTypeGetMany(
                    keyType: ((GenericInstanceTypeSignature)elementType).TypeArguments[0],
                    valueType: ((GenericInstanceTypeSignature)elementType).TypeArguments[1],
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsManagedValueType(interopReferences) => interopReferences.IEnumeratorAdapterManagedValueTypeGetMany(
                    elementType: elementType,
                    abiType: elementType.GetAbiType(interopReferences),
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsValueType => interopReferences.IEnumeratorAdapterUnmanagedValueTypeGetMany(
                    elementType: elementType,
                    abiType: elementType.GetAbiType(interopReferences),
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsTypeOfObject() => interopReferences.IEnumeratorAdapterOfObjectGetMany,
                _ when elementType.IsTypeOfString() => interopReferences.IEnumeratorAdapterOfStringGetMany,
                _ when elementType.IsTypeOfType(interopReferences) => interopReferences.IEnumeratorAdapterOfTypeGetMany,
                _ when elementType.IsTypeOfException(interopReferences) => interopReferences.IEnumeratorAdapterOfExceptionGetMany,
                _ => interopReferences.IEnumeratorAdapterReferenceTypeGetMany(
                    elementType: elementType,
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature())
            };

            // Define the 'GetMany' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetMany(void* thisPtr, uint itemsSize, <ABI_ELEMENT_TYPE>* items, uint* writtenCount)
            MethodDefinition getManyImplMethod = new(
                name: "GetMany"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32,
                        elementType.GetAbiType(interopReferences).MakePointerType(),
                        module.CorLibTypeFactory.UInt32.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction ldc_I4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_3_tryStart = new(Ldarg_3);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'GetMany' method
            getManyImplMethod.CilMethodBody = new CilMethodBody()
            {
                // Declare 1 variable:
                //   [0]: 'int' (the 'HRESULT' to return)
                LocalVariables = { new CilLocalVariable(module.CorLibTypeFactory.Int32) },
                Instructions =
                {
                    // Return 'E_POINTER' if either pointer argument is 'null'
                    { Ldarg_2 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Beq_S, ldc_I4_e_pointer.CreateLabel() },
                    { Ldarg_3 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { ldc_I4_e_pointer },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_3_tryStart },
                    { Ldarg_0 },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(enumeratorType) },
                    { Call, interopReferences.IEnumeratorAdapter1GetInstance(elementType) },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, getManyMethod },
                    { Stind_I4 },
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
                        TryStart = ldarg_3_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            return getManyImplMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Current</c> or <c>MoveNext</c> export method.
        /// </summary>
        /// <param name="methodName">The name of the method to generate.</param>
        /// <param name="adapterMethod">The adapter method to forward the call to.</param>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        private static MethodDefinition HasCurrentOrMoveNext(
            Utf8String methodName,
            MemberReference adapterMethod,
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // Define the method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int <NAME>(void* thisPtr, bool* result)
            MethodDefinition boolMethod = new(
                name: methodName,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Boolean.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the method
            boolMethod.CilMethodBody = new CilMethodBody()
            {
                // Declare 1 variable:
                //   [0]: 'int' (the 'HRESULT' to return)
                LocalVariables = { new CilLocalVariable(module.CorLibTypeFactory.Int32) },
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(enumeratorType) },
                    { Call, interopReferences.IEnumeratorAdapter1GetInstance(elementType) },
                    { Callvirt, adapterMethod },
                    { Stind_I1 },
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
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            return boolMethod;
        }
    }
}
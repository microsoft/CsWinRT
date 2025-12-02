// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop method definitions.
/// </summary>
internal static partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for impl types for <see cref="System.Collections.Generic.IEnumerator{T}"/> interfaces.
    /// </summary>
    public static class IEnumerator1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Current</c> export method.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition get_Current(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
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
                        elementType.GetAbiType(interopReferences).Import(module).MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(enumeratorType).Import(module) },
                    { Call, interopReferences.IEnumeratorAdapter1GetInstance(elementType).Import(module) },
                    { Callvirt, interopReferences.IEnumeratorAdapter1get_Current(elementType).Import(module) },
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

            // Marshal the managed value to the target address
            if (SignatureComparer.IgnoreVersion.Equals(elementType, module.CorLibTypeFactory.String))
            {
                currentMethod.CilMethodBody!.Instructions.ReplaceRange(nop_convertToUnmanaged, [
                    new CilInstruction(Call, interopReferences.MemoryExtensionsAsSpanCharString.Import(module)),
                    new CilInstruction(Call, interopReferences.HStringMarshallerConvertToUnmanaged.Import(module)),
                    new CilInstruction(Stind_I)]);
            }
            else
            {
                // TODO
                currentMethod.CilMethodBody!.Instructions.ReplaceRange(nop_convertToUnmanaged, [
                    new CilInstruction(Pop),
                    new CilInstruction(Pop)]);
            }

            return currentMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Current</c> export method.
        /// </summary>
        /// <param name="nameUtf8">The name of the method to generate.</param>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition HasCurrentOrMoveNext(
            ReadOnlySpan<byte> nameUtf8,
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
                name: nameUtf8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Boolean.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Get the reference to the target method to invoke
            MemberReference adapterMethod = nameUtf8.SequenceEqual("get_HasCurrent"u8)
                ? interopReferences.IEnumeratorAdapter1get_HasCurrent(elementType)
                : interopReferences.IEnumeratorAdapter1MoveNext(elementType);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Create a method body for the 'get_HasCurrent' method
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(enumeratorType).Import(module) },
                    { Call, interopReferences.IEnumeratorAdapter1GetInstance(elementType).Import(module) },
                    { Callvirt, adapterMethod.Import(module) },
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
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return boolMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetMany</c> export method.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition GetMany(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // Define the 'GetMany' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetMany(void* thisPtr, uint itemsSize, <ABI_ELEMENT_TYPE>* items, uint* writtenCount)
            MethodDefinition currentMethod = new(
                name: "GetMany"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32,
                        elementType.GetAbiType(interopReferences).Import(module).MakePointerType(),
                        module.CorLibTypeFactory.UInt32.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction ldc_I4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
            CilInstruction nop_implementation = new(Nop);

            // Create a method body for the 'get_Current' method
            currentMethod.CilMethodBody = new CilMethodBody()
            {
                // Declare 2 variables:
                //   [0]: 'int' (the 'HRESULT' to return)
                //   [1]: 'IEnumeratorAdapter<<ELEMENT_TYPE>>' (the adapter instance)
                LocalVariables =
                {
                    new CilLocalVariable(module.CorLibTypeFactory.Int32),
                    new CilLocalVariable(interopReferences.IEnumeratorAdapter1.MakeGenericReferenceType(elementType).Import(module))
                },
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
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(enumeratorType).Import(module) },
                    { Call, interopReferences.IEnumeratorAdapter1GetInstance(elementType).Import(module) },
                    { Stloc_1 },
                    { nop_implementation },
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
                        TryStart = ldarg_0_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            // TODO: replace 'nop_implementation' with the actual implementation of the method

            return currentMethod;
        }
    }
}
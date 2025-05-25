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

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop method definitions.
/// </summary>
internal static class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.IEnumerator{T}"/> types.
    /// </summary>
    public static class IEnumerator1
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Current</c> export method.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Current(
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
                        module.CorLibTypeFactory.Void.MakePointerType()])) // TODO
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
            currentMethod.CilMethodBody = new CilMethodBody(currentMethod)
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
                    new CilInstruction(Call, interopReferences.MemoryExtensionsAsSpanCharString),
                    new CilInstruction(Call, interopReferences.HStringMarshallerConvertToUnmanaged),
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
            boolMethod.CilMethodBody = new CilMethodBody(boolMethod)
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
    }
}

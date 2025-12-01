// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for impl types for interfaces deriving from <c>Windows.Foundation.IAsyncInfo</c>.
    /// </summary>
    public static class IAsyncInfoImpl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for a get method for a handler delegate of a specified type.
        /// </summary>
        /// <param name="methodName">The name of the get method.</param>
        /// <param name="asyncInfoType">The type of async info interface.</param>
        /// <param name="handlerType">The type of the handler delegate.</param>
        /// <param name="get_HandlerMethod">The interface method to invoke on <paramref name="asyncInfoType"/>.</param>
        /// <param name="convertToUnmanagedMethod">The method to use to convert the handler to unmanaged.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        public static MethodDefinition get_Handler(
            Utf8String methodName,
            TypeSignature asyncInfoType,
            TypeSignature handlerType,
            MemberReference get_HandlerMethod,
            MethodDefinition convertToUnmanagedMethod,
            InteropReferences interopReferences)
        {
            // Define the 'get_Handler' get method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int <METHOD_NAME>(void* thisPtr, void** result)
            MethodDefinition handlerMethod = new(
                name: methodName,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.Void.MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Declare the local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'WindowsRuntimeObjectReferenceValue' (the marshalled async info instance)
            CilLocalVariable loc_0_hresult = new(interopReferences.Int32);
            CilLocalVariable loc_1_handlerValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());

            // Create a method body for the 'get_Current' method
            handlerMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_hresult, loc_1_handlerValue },
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(asyncInfoType) },
                    { Callvirt, get_HandlerMethod },
                    { Call, convertToUnmanagedMethod },
                    { Stloc_1 },
                    { Ldloca_S, loc_1_handlerValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe },
                    { Stind_I },
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

            return handlerMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for a set method for a handler delegate of a specified type.
        /// </summary>
        /// <param name="methodName">The name of the get method.</param>
        /// <param name="asyncInfoType">The type of async info interface.</param>
        /// <param name="handlerType">The type of the handler delegate.</param>
        /// <param name="set_HandlerMethod">The interface method to invoke on <paramref name="asyncInfoType"/>.</param>
        /// <param name="convertToManagedMethod">The method to use to convert the handler to a managed object.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        public static MethodDefinition set_Handler(
            Utf8String methodName,
            TypeSignature asyncInfoType,
            TypeSignature handlerType,
            MemberReference set_HandlerMethod,
            MethodDefinition convertToManagedMethod,
            InteropReferences interopReferences)
        {
            // Define the 'set_Handler' get method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int <METHOD_NAME>(void* thisPtr, void* handler)
            MethodDefinition handlerMethod = new(
                name: methodName,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.Void.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Declare the local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_hresult = new(interopReferences.Int32);

            // Create a method body for the 'get_Current' method
            handlerMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_hresult },
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
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(asyncInfoType) },
                    { Ldarg_1 },
                    { Call, convertToManagedMethod },
                    { Callvirt, set_HandlerMethod },
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
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            return handlerMethod;
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal static partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for impl types for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> interfaces.
    /// </summary>
    public static class IMapChangedEventArgs1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_CollectionChanged</c> export method.
        /// </summary>
        /// <param name="argsType">The <see cref="TypeSignature"/> for the args type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition CollectionChanged(
            GenericInstanceTypeSignature argsType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = argsType.TypeArguments[0];

            // Define the 'get_CollectionChanged' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_CollectionChanged(void* thisPtr, CollectionChange* result)
            MethodDefinition collectionChangeMethod = new(
                name: "get_CollectionChanged"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.CollectionChange.Import(module).MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Create a method body for the 'get_CollectionChanged' method
            collectionChangeMethod.CilMethodBody = new CilMethodBody()
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(argsType).Import(module) },
                    { Callvirt, interopReferences.IMapChangedEventArgs1get_CollectionChange(elementType).Import(module) },
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
                        TryStart = ldarg_1_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return collectionChangeMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Key</c> export method.
        /// </summary>
        /// <param name="argsType">The <see cref="TypeSignature"/> for the args type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Key(
            GenericInstanceTypeSignature argsType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = argsType.TypeArguments[0];

            // Define the 'get_Key' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Key(void* thisPtr, <ABI_KEY_TYPE>* result)
            MethodDefinition currentMethod = new(
                name: "get_Key"u8,
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(argsType).Import(module) },
                    { Callvirt, interopReferences.IMapChangedEventArgs1get_Key(elementType).Import(module) },
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
    }
}

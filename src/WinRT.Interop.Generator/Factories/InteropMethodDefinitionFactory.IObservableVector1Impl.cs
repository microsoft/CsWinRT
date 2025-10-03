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

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for impl types for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> interfaces.
    /// </summary>
    public static class IObservableVector1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>add_VectorChanged</c> export method.
        /// </summary>
        /// <param name="vectorType">The <see cref="TypeSignature"/> for the vector type.</param>
        /// <param name="get_VectorChangedTableMethod">The <see cref="MethodDefinition"/> to get the event token table.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition add_VectorChanged(
            GenericInstanceTypeSignature vectorType,
            MethodDefinition get_VectorChangedTableMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // Prepare the 'VectorChangedEventHandler<<ELEMENT_TYPE>>' signature
            TypeSignature eventHandlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType(elementType);

            // Prepare the 'EventRegistrationTokenTable<VectorChangedEventHandler<ELEMENT_TYPE>>' signature
            TypeSignature eventRegistrationTokenTableType = interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType);

            // Prepare the 'ConditionalWeakTable<<VECTOR_TYPE>, EventRegistrationTokenTable<VectorChangedEventHandler<<ELEMENT_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(
                vectorType,
                interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType));

            // Define the 'add_VectorChanged' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int add_VectorChanged(void* thisPtr, void* handler, EventRegistrationToken* token)
            MethodDefinition add_VectorChangedMethod = new(
                name: "add_VectorChanged"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.EventRegistrationToken.MakePointerType().Import(module)]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Jump labels
            CilInstruction ldc_i4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction callvirt_catchHResult = new(Callvirt, interopReferences.Exceptionget_HResult.Import(module));
            CilInstruction ldloc_2_returnHResult = new(Ldloc_2);

            // Declare the local variables:
            //   [0]: '<VECTOR_TYPE>' (the 'unboxedValue' object)
            //   [1]: 'VectorChangedEventHandler<<ELEMENT_TYPE>>' (the 'managedHandler' object)
            //   [2]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_unboxedValue = new(vectorType.Import(module));
            CilLocalVariable loc_1_managedHandler = new(eventHandlerType.Import(module));
            CilLocalVariable loc_2_hresult = new(module.CorLibTypeFactory.Int32);

            // Create a method body for the 'add_VectorChanged' method
            add_VectorChangedMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_unboxedValue, loc_1_managedHandler, loc_2_hresult },
                Instructions =
                {
                    // Return 'E_POINTER' if either argument is 'null'
                    { Ldarg_1 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Beq_S, ldc_i4_e_pointer.CreateLabel() },
                    { Ldarg_2 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { ldc_i4_e_pointer },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(vectorType).Import(module) },
                    { Stloc_0 },
                    { Ldarg_1 },
                    { Call, emitState.LookupTypeDefinition(eventHandlerType, "Marshaller").GetMethod("ConvertToManaged"u8) },
                    { Stloc_1 },

                    // *token = VectorChangedTable.GetOrCreateValue(unboxedValue).AddEventHandler(managedHandler);
                    { Ldarg_2 },
                    { Call, get_VectorChangedTableMethod },
                    { Ldloc_0 },
                    { Callvirt, interopReferences.ConditionalWeakTable2GetOrCreateValue(conditionalWeakTableType).Import(module) },
                    { Ldloc_1 },
                    { Callvirt, interopReferences.EventRegistrationTokenTableAddEventHandler(eventRegistrationTokenTableType).Import(module) },
                    { Stobj, interopReferences.EventRegistrationToken.Import(module) },

                    // unboxedValue.VectorChanged += managedHandler;
                    { Ldloc_0 },
                    { Ldloc_1 },
                    { Callvirt, interopReferences.IObservableVector1add_VectorChanged(elementType).Import(module) },
                    { Ldc_I4_0 },
                    { Stloc_2 },
                    { Leave_S, ldloc_2_returnHResult.CreateLabel() },

                    // '.catch' code
                    { callvirt_catchHResult },
                    { Stloc_2 },
                    { Leave_S, ldloc_2_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [2]
                    { ldloc_2_returnHResult },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Exception,
                        TryStart = ldarg_0_tryStart.CreateLabel(),
                        TryEnd = callvirt_catchHResult.CreateLabel(),
                        HandlerStart = callvirt_catchHResult.CreateLabel(),
                        HandlerEnd = ldloc_2_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return add_VectorChangedMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>remove_VectorChanged</c> export method.
        /// </summary>
        /// <param name="vectorType">The <see cref="TypeSignature"/> for the vector type.</param>
        /// <param name="get_VectorChangedTableMethod">The <see cref="MethodDefinition"/> to get the event token table.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition remove_VectorChanged(
            GenericInstanceTypeSignature vectorType,
            MethodDefinition get_VectorChangedTableMethod,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // Prepare the 'VectorChangedEventHandler<<ELEMENT_TYPE>>' signature
            TypeSignature eventHandlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType(elementType);

            // Prepare the 'EventRegistrationTokenTable<VectorChangedEventHandler<ELEMENT_TYPE>>' signature
            TypeSignature eventRegistrationTokenTableType = interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType);

            // Prepare the 'ConditionalWeakTable<<VECTOR_TYPE>, EventRegistrationTokenTable<VectorChangedEventHandler<<ELEMENT_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(
                vectorType,
                interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType));

            // Define the 'remove_VectorChanged' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int remove_VectorChanged(void* thisPtr, EventRegistrationToken token)
            MethodDefinition remove_VectorChangedMethod = new(
                name: "remove_VectorChanged"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.EventRegistrationToken.ToValueTypeSignature().Import(module)]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Jump labels
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldc_i4_0_return0 = new(Ldc_I4_0);
            CilInstruction callvirt_catchHResult = new(Callvirt, interopReferences.Exceptionget_HResult.Import(module));
            CilInstruction ldloc_3_returnHResult = new(Ldloc_3);

            // Declare the local variables:
            //   [0]: '<VECTOR_TYPE>' (the 'unboxedValue' object)
            //   [1]: 'EventRegistrationTokenTable<VectorChangedEventHandler<<ELEMENT_TYPE>>>' (the 'table' object)
            //   [2]: 'VectorChangedEventHandler<<ELEMENT_TYPE>>' (the 'managedHandler' object)
            //   [3]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_unboxedValue = new(vectorType.Import(module));
            CilLocalVariable loc_1_table = new(eventRegistrationTokenTableType.Import(module));
            CilLocalVariable loc_2_managedHandler = new(eventHandlerType.Import(module));
            CilLocalVariable loc_3_hresult = new(module.CorLibTypeFactory.Int32);

            // Create a method body for the 'remove_VectorChanged' method
            remove_VectorChangedMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_unboxedValue, loc_1_table, loc_2_managedHandler, loc_3_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(vectorType).Import(module) },
                    { Stloc_0 },

                    // if (unboxedValue != null && VectorChangedTable.TryGetValue(unboxedValue, out EventRegistrationTokenTable<VectorChangedEventHandler<<ELEMENT_TYPE>>> table))
                    { Ldloc_0 },
                    { Brfalse_S, ldc_i4_0_return0.CreateLabel() },
                    { Call, get_VectorChangedTableMethod },
                    { Ldloc_0 },
                    { Ldloca_S, loc_1_table },
                    { Callvirt, interopReferences.ConditionalWeakTable2TryGetValue(conditionalWeakTableType).Import(module) },
                    { Brfalse_S, ldc_i4_0_return0.CreateLabel() },

                    // if (table.RemoveEventHandler(token, out VectorChangedEventHandler<<ELEMENT_TYPE>> managedHandler))
                    { Ldloc_1 },
                    { Ldarg_1 },
                    { Ldloca_S, loc_2_managedHandler },
                    { Callvirt, interopReferences.EventRegistrationTokenTableRemoveEventHandler(eventRegistrationTokenTableType).Import(module) },
                    { Brfalse_S, ldc_i4_0_return0.CreateLabel() },

                    // unboxedValue.VectorChanged -= managedHandler;
                    { Ldloc_0 },
                    { Ldloc_2 },
                    { Callvirt, interopReferences.IObservableVector1remove_VectorChanged(elementType).Import(module) },

                    // Return S_OK
                    { ldc_i4_0_return0 },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_returnHResult.CreateLabel() },

                    // '.catch' code
                    { callvirt_catchHResult },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [2]
                    { ldloc_3_returnHResult },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Exception,
                        TryStart = ldarg_0_tryStart.CreateLabel(),
                        TryEnd = callvirt_catchHResult.CreateLabel(),
                        HandlerStart = callvirt_catchHResult.CreateLabel(),
                        HandlerEnd = ldloc_3_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return remove_VectorChangedMethod;
        }
    }
}

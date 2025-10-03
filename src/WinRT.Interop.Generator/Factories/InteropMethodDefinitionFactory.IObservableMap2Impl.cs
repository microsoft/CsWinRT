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
    /// Helpers for impl types for <c>Windows.Foundation.Collections.IObservableMap&lt;K,V&gt;</c> interfaces.
    /// </summary>
    public static class IObservableMap2Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>add_MapChanged</c> export method.
        /// </summary>
        /// <param name="mapType">The <see cref="TypeSignature"/> for the map type.</param>
        /// <param name="get_MapChangedTableMethod">The <see cref="MethodDefinition"/> to get the event token table.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition add_MapChanged(
            GenericInstanceTypeSignature mapType,
            MethodDefinition get_MapChangedTableMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            // Prepare the 'MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>' signature
            TypeSignature eventHandlerType = interopReferences.MapChangedEventHandler2.MakeGenericReferenceType(keyType, valueType);

            // Prepare the 'EventRegistrationTokenTable<MapChangedEventHandler<KEY_TYPE>, <VALUE_TYPE>>' signature
            TypeSignature eventRegistrationTokenTableType = interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType);

            // Prepare the 'ConditionalWeakTable<<MAP_TYPE>, EventRegistrationTokenTable<MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(mapType, eventRegistrationTokenTableType);

            // Define the 'add_MapChanged' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int add_MapChanged(void* thisPtr, void* handler, EventRegistrationToken* token)
            MethodDefinition add_MapChangedMethod = new(
                name: "add_MapChanged"u8,
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
            //   [0]: '<MAP_TYPE>' (the 'unboxedValue' object)
            //   [1]: 'MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>' (the 'managedHandler' object)
            //   [2]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_unboxedValue = new(mapType.Import(module));
            CilLocalVariable loc_1_managedHandler = new(eventHandlerType.Import(module));
            CilLocalVariable loc_2_hresult = new(module.CorLibTypeFactory.Int32);

            // Create a method body for the 'add_MapChanged' method
            add_MapChangedMethod.CilMethodBody = new CilMethodBody()
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(mapType).Import(module) },
                    { Stloc_0 },
                    { Ldarg_1 },
                    { Call, emitState.LookupTypeDefinition(eventHandlerType, "Marshaller").GetMethod("ConvertToManaged"u8) },
                    { Stloc_1 },

                    // *token = MapChangedTable.GetOrCreateValue(unboxedValue).AddEventHandler(managedHandler);
                    { Ldarg_2 },
                    { Call, get_MapChangedTableMethod },
                    { Ldloc_0 },
                    { Callvirt, interopReferences.ConditionalWeakTable2GetOrCreateValue(conditionalWeakTableType).Import(module) },
                    { Ldloc_1 },
                    { Callvirt, interopReferences.EventRegistrationTokenTableAddEventHandler(eventRegistrationTokenTableType).Import(module) },
                    { Stobj, interopReferences.EventRegistrationToken.Import(module) },

                    // unboxedValue.MapChanged += managedHandler;
                    { Ldloc_0 },
                    { Ldloc_1 },
                    { Callvirt, interopReferences.IObservableMap2add_MapChanged(keyType, valueType).Import(module) },
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

            return add_MapChangedMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>remove_MapChanged</c> export method.
        /// </summary>
        /// <param name="mapType">The <see cref="TypeSignature"/> for the map type.</param>
        /// <param name="get_MapChangedTableMethod">The <see cref="MethodDefinition"/> to get the event token table.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition remove_MapChanged(
            GenericInstanceTypeSignature mapType,
            MethodDefinition get_MapChangedTableMethod,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            // Prepare the 'MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>' signature
            TypeSignature eventHandlerType = interopReferences.MapChangedEventHandler2.MakeGenericReferenceType(keyType, valueType);

            // Prepare the 'EventRegistrationTokenTable<MapChangedEventHandler<KEY_TYPE>, <VALUE_TYPE>>' signature
            TypeSignature eventRegistrationTokenTableType = interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType);

            // Prepare the 'ConditionalWeakTable<<MAP_TYPE>, EventRegistrationTokenTable<MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(mapType, eventRegistrationTokenTableType);

            // Define the 'remove_MapChanged' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int remove_MapChanged(void* thisPtr, EventRegistrationToken token)
            MethodDefinition remove_MapChangedMethod = new(
                name: "remove_MapChanged"u8,
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
            //   [0]: '<MAP_TYPE>' (the 'unboxedValue' object)
            //   [1]: 'EventRegistrationTokenTable<MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>>' (the 'table' object)
            //   [2]: 'MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>' (the 'managedHandler' object)
            //   [3]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_unboxedValue = new(mapType.Import(module));
            CilLocalVariable loc_1_table = new(eventRegistrationTokenTableType.Import(module));
            CilLocalVariable loc_2_managedHandler = new(eventHandlerType.Import(module));
            CilLocalVariable loc_3_hresult = new(module.CorLibTypeFactory.Int32);

            // Create a method body for the 'remove_MapChanged' method
            remove_MapChangedMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_unboxedValue, loc_1_table, loc_2_managedHandler, loc_3_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(mapType).Import(module) },
                    { Stloc_0 },

                    // if (unboxedValue != null && MapChangedTable.TryGetValue(unboxedValue, out EventRegistrationTokenTable<MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>> table))
                    { Ldloc_0 },
                    { Brfalse_S, ldc_i4_0_return0.CreateLabel() },
                    { Call, get_MapChangedTableMethod },
                    { Ldloc_0 },
                    { Ldloca_S, loc_1_table },
                    { Callvirt, interopReferences.ConditionalWeakTable2TryGetValue(conditionalWeakTableType).Import(module) },
                    { Brfalse_S, ldc_i4_0_return0.CreateLabel() },

                    // if (table.RemoveEventHandler(token, out MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>> managedHandler))
                    { Ldloc_1 },
                    { Ldarg_1 },
                    { Ldloca_S, loc_2_managedHandler },
                    { Callvirt, interopReferences.EventRegistrationTokenTableRemoveEventHandler(eventRegistrationTokenTableType).Import(module) },
                    { Brfalse_S, ldc_i4_0_return0.CreateLabel() },

                    // unboxedValue.MapChanged -= managedHandler;
                    { Ldloc_0 },
                    { Ldloc_2 },
                    { Callvirt, interopReferences.IObservableMap2remove_MapChanged(keyType, valueType).Import(module) },

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

            return remove_MapChangedMethod;
        }
    }
}

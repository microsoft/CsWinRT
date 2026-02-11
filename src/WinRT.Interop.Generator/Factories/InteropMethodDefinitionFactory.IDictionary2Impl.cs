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

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop method definitions.
/// </summary>
internal static partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for impl types for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> interfaces.
    /// </summary>
    public static class IDictionary2Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetView</c> export method.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition GetView(
            GenericInstanceTypeSignature dictionaryType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            // Define the 'GetView' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetView(void* thisPtr, void** result)
            MethodDefinition getViewMethod = new(
                name: "GetView"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(dictionaryType);
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);
            CilInstruction nop_convertToUnmanaged = new(Nop);

            // Create a method body for the 'GetView' method
            getViewMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(dictionaryType) },
                    { Stloc_0 },
                    { Ldarg_1 },
                    { Ldloc_0 },
                    { Call, interopReferences.IDictionaryAdapter2GetView(keyType, valueType) },
                    { nop_convertToUnmanaged },
                    { Ldc_I4_0 },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [1]
                    { ldloc_1_returnHResult  },
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
                        HandlerEnd = ldloc_1_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            // Track the method for rewrite to marshal the result value
            emitState.TrackRetValValueMethodRewrite(
                retValType: interopReferences.IReadOnlyDictionary2.MakeGenericReferenceType(keyType, valueType),
                method: getViewMethod,
                marker: nop_convertToUnmanaged);

            return getViewMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Insert</c> export method.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Insert(
            GenericInstanceTypeSignature dictionaryType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];
            TypeSignature keyAbiType = keyType.GetAbiType(interopReferences);
            TypeSignature valueAbiType = valueType.GetAbiType(interopReferences);

            // Define the 'Insert' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Insert(void* thisPtr, <ABI_KEY_TYPE> key, <ABI_VALUE_TYPE> value, bool* result)
            MethodDefinition insertMethod = new(
                name: "Insert"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        keyAbiType,
                        valueAbiType,
                        module.CorLibTypeFactory.Boolean.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(dictionaryType);
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter1Rewrite = new(Nop);
            CilInstruction nop_parameter2Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'Insert' method
            insertMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(dictionaryType) },
                    { Stloc_0 },
                    { Ldarg_3 },
                    { Ldloc_0 },
                    { nop_parameter1Rewrite },
                    { nop_parameter2Rewrite },
                    { Call, interopReferences.IDictionaryAdapter2Insert(keyType, valueType) },
                    { Stind_I1 },
                    { Ldc_I4_0 },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [1]
                    { ldloc_1_returnHResult  },
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
                        HandlerEnd = ldloc_1_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            // Track rewriting the two parameters for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: keyType,
                method: insertMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            emitState.TrackManagedParameterMethodRewrite(
                parameterType: valueType,
                method: insertMethod,
                marker: nop_parameter2Rewrite,
                parameterIndex: 2);

            return insertMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Remove</c> export method.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Remove(
            GenericInstanceTypeSignature dictionaryType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            // Define the 'Remove' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Remove(void* thisPtr, <ABI_KEY_TYPE> key)
            MethodDefinition removeMethod = new(
                name: "Remove"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        keyType.GetAbiType(interopReferences)]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(dictionaryType);
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter1Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Get the target 'Remove' method (we can optimize for 'string' types)
            IMethodDescriptor adapterRemoveMethod = keyType.IsTypeOfString()
                ? interopReferences.IDictionaryAdapterOfStringRemove(valueType)
                : interopReferences.IDictionaryAdapter2Remove(keyType, valueType);

            // Create a method body for the 'Remove' method
            removeMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(dictionaryType) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { nop_parameter1Rewrite },
                    { Call, adapterRemoveMethod },
                    { Ldc_I4_0 },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [1]
                    { ldloc_1_returnHResult  },
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
                        HandlerEnd = ldloc_1_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            // If the key type is 'string', we use 'ReadOnlySpan<char>' to avoid an allocation
            TypeSignature parameterType = keyType.IsTypeOfString()
                ? interopReferences.ReadOnlySpanChar
                : keyType;

            // Track rewriting the parameter for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: parameterType,
                method: removeMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            return removeMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Clear</c> export method.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition Clear(
            GenericInstanceTypeSignature dictionaryType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            // Define the 'Clear' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Clear(void* thisPtr)
            MethodDefinition clearMethod = new(
                name: "Clear"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(dictionaryType);
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'Clear' method
            clearMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(dictionaryType) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Callvirt, interopReferences.ICollection1Clear(interopReferences.KeyValuePair2.MakeGenericValueType(keyType, valueType)) },
                    { Ldc_I4_0 },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_1 },
                    { Leave_S, ldloc_1_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [1]
                    { ldloc_1_returnHResult  },
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
                        HandlerEnd = ldloc_1_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            return clearMethod;
        }
    }
}
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
    /// Helpers for impl types for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> interfaces.
    /// </summary>
    public static class IReadOnlyDictionary2Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Lookup</c> export method.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="lookupMethod">The interface method to invoke on <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <remarks>
        /// This method can also be used to define the <c>Lookup</c> method for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> interfaces.
        /// </remarks>
        public static MethodDefinition Lookup(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            MemberReference lookupMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];

            // Define the 'Lookup' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Lookup(void* thisPtr, <ABI_KEY_TYPE> key, <ABI_ELEMENT_TYPE>* result)
            MethodDefinition lookupImplMethod = new(
                name: "Lookup"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        keyType.GetAbiType(interopReferences),
                        valueType.GetAbiType(interopReferences).MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(readOnlyDictionaryType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter1Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);
            CilInstruction nop_convertToUnmanaged = new(Nop);

            IMethodDescriptor adapterLookupMethod;

            // Get the target 'Lookup' method (we can optimize for 'string' types)
            if (keyType.IsTypeOfString())
            {
                adapterLookupMethod = SignatureComparer.IgnoreVersion.Equals(readOnlyDictionaryType.GenericType, interopReferences.IReadOnlyDictionary2)
                    ? interopReferences.IReadOnlyDictionaryAdapterOfStringLookup(valueType)
                    : interopReferences.IDictionaryAdapterOfStringLookup(valueType);
            }
            else
            {
                // Otherwise use the provided method directly (it will always be valid)
                adapterLookupMethod = lookupMethod;
            }

            // Create a method body for the 'Lookup' method
            lookupImplMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // Return 'E_POINTER' if the argument is 'null'
                    { Ldarg_2 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { Ldc_I4, unchecked((int)0x80004003) },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([readOnlyDictionaryType]) },
                    { Stloc_0 },
                    { Ldarg_2 },
                    { Ldloc_0 },
                    { nop_parameter1Rewrite },
                    { Call, adapterLookupMethod },
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

            // If the key type is 'string', we use 'ReadOnlySpan<char>' to avoid an allocation
            TypeSignature parameterType = keyType.IsTypeOfString()
                ? interopReferences.ReadOnlySpanChar
                : keyType;

            // Track rewriting the parameter for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: parameterType,
                method: lookupImplMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            // Track the method for rewrite to marshal the result value
            emitState.TrackRetValValueMethodRewrite(
                retValType: valueType,
                method: lookupImplMethod,
                marker: nop_convertToUnmanaged);

            return lookupImplMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Size</c> export method.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="sizeMethod">The interface method to invoke on <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <remarks>
        /// This method can also be used to define the <c>get_Size</c> method for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> interfaces.
        /// </remarks>
        public static MethodDefinition get_Size(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            MemberReference sizeMethod,
            InteropReferences interopReferences)
        {
            // Define the 'get_Size' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Size(void* thisPtr, uint* result)
            MethodDefinition sizeImplMethod = new(
                name: "get_Size"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.UInt32.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(readOnlyDictionaryType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'get_Size' method
            sizeImplMethod.CilMethodBody = new CilMethodBody()
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([readOnlyDictionaryType]) },
                    { Stloc_0 },
                    { Ldarg_1 },
                    { Ldloc_0 },
                    { Call, sizeMethod },
                    { Stind_I4 },
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

            return sizeImplMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>HasKey</c> export method.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="containsKeyMethod">The interface method to invoke on <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <remarks>
        /// This method can also be used to define the <c>HasKey</c> method for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> interfaces.
        /// </remarks>
        public static MethodDefinition HasKey(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            MemberReference containsKeyMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];

            // Define the 'HasKey' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int HasKey(void* thisPtr, <ABI_KEY_TYPE> key, bool* result)
            MethodDefinition hasKeyImplMethod = new(
                name: "HasKey"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        keyType.GetAbiType(interopReferences),
                        interopReferences.Boolean.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(readOnlyDictionaryType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter1Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);
            CilInstruction nop_convertToUnmanaged = new(Nop);
            CilInstruction callHasKeyMethod;

            // Get the target 'HasKey' method (we can optimize for 'string' types).
            // We prepare the full instruction, as we need 'callvirt' in some cases.
            if (keyType.IsTypeOfString())
            {
                MethodSpecification hasKeyMethodSpecification = SignatureComparer.IgnoreVersion.Equals(readOnlyDictionaryType.GenericType, interopReferences.IReadOnlyDictionary2)
                    ? interopReferences.IReadOnlyDictionaryAdapterOfStringHasKey(valueType)
                    : interopReferences.IDictionaryAdapterOfStringHasKey(valueType);

                callHasKeyMethod = new(Call, hasKeyMethodSpecification);
            }
            else
            {
                // Otherwise just use 'ContainsKey' method passed as input
                callHasKeyMethod = new(Callvirt, containsKeyMethod);
            }

            // Create a method body for the 'HasKey' method
            hasKeyImplMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // Return 'E_POINTER' if the argument is 'null'
                    { Ldarg_2 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { Ldc_I4, unchecked((int)0x80004003) },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([readOnlyDictionaryType]) },
                    { Stloc_0 },
                    { Ldarg_2 },
                    { Ldloc_0 },
                    { nop_parameter1Rewrite },
                    { callHasKeyMethod },
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

            // If the key type is 'string', we use 'ReadOnlySpan<char>' to avoid an allocation
            TypeSignature parameterType = keyType.IsTypeOfString()
                ? interopReferences.ReadOnlySpanChar
                : keyType;

            // Track rewriting the parameter for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: parameterType,
                method: hasKeyImplMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            // Track the method for rewrite to marshal the result value
            emitState.TrackRetValValueMethodRewrite(
                retValType: interopReferences.Boolean,
                method: hasKeyImplMethod,
                marker: nop_convertToUnmanaged);

            return hasKeyImplMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Split</c> export method.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition Split(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];

            // Define the 'Split' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Split(void* thisPtr, void** first, void** second)
            MethodDefinition splitMethod = new(
                name: "Split"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.Void.MakePointerType().MakePointerType(),
                        interopReferences.Void.MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_DICTIONARY_TYPE>' (for 'thisObject')
            //   [1]: '<READONLY_DICTIONARY_TYPE>' (for 'firstObject')
            //   [2]: '<READONLY_DICTIONARY_TYPE>' (for 'secondObject')
            //   [3]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(readOnlyDictionaryType);
            CilLocalVariable loc_1_firstObject = new(readOnlyDictionaryType);
            CilLocalVariable loc_2_secondObject = new(readOnlyDictionaryType);
            CilLocalVariable loc_3_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_3_returnHResult = new(Ldloc_3);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);
            CilInstruction nop_firstObject_convertToUnmanaged = new(Nop);
            CilInstruction nop_secondObject_convertToUnmanaged = new(Nop);

            // Create a method body for the 'Split' method
            splitMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_firstObject, loc_2_secondObject, loc_3_hresult },
                Instructions =
                {
                    // Return 'E_POINTER' if the argument is 'null'
                    { Ldarg_2 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { Ldc_I4, unchecked((int)0x80004003) },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([readOnlyDictionaryType]) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Ldloca_S, loc_1_firstObject },
                    { Ldloca_S, loc_2_secondObject },
                    { Call, interopReferences.IReadOnlyDictionaryAdapter2Split(keyType, valueType) },
                    { Ldarg_1 },
                    { Ldloc_1 },
                    { nop_firstObject_convertToUnmanaged },
                    { Ldarg_2 },
                    { Ldloc_2 },
                    { nop_secondObject_convertToUnmanaged },
                    { Ldc_I4_0 },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [1]
                    { ldloc_3_returnHResult  },
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
                        HandlerEnd = ldloc_3_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            // Track the method for rewrite to marshal the two result values
            emitState.TrackRetValValueMethodRewrite(
                retValType: readOnlyDictionaryType,
                method: splitMethod,
                marker: nop_firstObject_convertToUnmanaged);

            emitState.TrackRetValValueMethodRewrite(
                retValType: readOnlyDictionaryType,
                method: splitMethod,
                marker: nop_secondObject_convertToUnmanaged);

            return splitMethod;
        }
    }
}
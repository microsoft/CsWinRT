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
    /// Helpers for impl types for <see cref="System.Collections.Generic.IReadOnlyList{T}"/> interfaces.
    /// </summary>
    public static class IReadOnlyList1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetAt</c> export method.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="getAtMethod">The adapter method to invoke on <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <remarks>
        /// This method can also be used to define the <c>GetAt</c> method for <see cref="System.Collections.Generic.IList{T}"/> interfaces.
        /// </remarks>
        public static MethodDefinition GetAt(
            GenericInstanceTypeSignature readOnlyListType,
            MemberReference getAtMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Define the 'GetAt' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetAt(void* thisPtr, uint index, <ABI_ELEMENT_TYPE>* result)
            MethodDefinition getAtImplMethod = new(
                name: "GetAt"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32,
                        elementType.GetAbiType(interopReferences).Import(module).MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(readOnlyListType.Import(module));
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
            CilInstruction nop_convertToUnmanaged = new(Nop);

            // Create a method body for the 'GetAt' method
            getAtImplMethod.CilMethodBody = new CilMethodBody()
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(readOnlyListType).Import(module) },
                    { Stloc_0 },
                    { Ldarg_2 },
                    { Ldloc_0 },
                    { Ldarg_1 },
                    { Call, getAtMethod.Import(module) },
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
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            // Track the method for rewrite to marshal the result value
            emitState.TrackRetValValueMethodRewrite(
                retValType: elementType,
                method: getAtImplMethod,
                marker: nop_convertToUnmanaged);

            return getAtImplMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Size</c> export method.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="sizeMethod">The adapter method to invoke on <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <remarks>
        /// This method can also be used to define the <c>get_Size</c> method for <see cref="System.Collections.Generic.IList{T}"/> interfaces.
        /// </remarks>
        public static MethodDefinition get_Size(
            GenericInstanceTypeSignature readOnlyListType,
            MemberReference sizeMethod,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            // Define the 'get_Size' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Size(void* thisPtr, uint* result)
            MethodDefinition sizeImplMethod = new(
                name: "get_Size"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(readOnlyListType.Import(module));
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(readOnlyListType).Import(module) },
                    { Stloc_0 },
                    { Ldarg_1 },
                    { Ldloc_0 },
                    { Call, sizeMethod.Import(module) },
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
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return sizeImplMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>IndexOf</c> export method.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="indexOfMethod">The adapter method to invoke on <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <remarks>
        /// This method can also be used to define the <c>IndexOf</c> method for <see cref="System.Collections.Generic.IList{T}"/> interfaces.
        /// </remarks>
        public static MethodDefinition IndexOf(
            GenericInstanceTypeSignature readOnlyListType,
            MemberReference indexOfMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Define the 'IndexOf' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int IndexOf(void* thisPtr, <ABI_ELEMENT_TYPE> value, uint* index, bool* result)
            MethodDefinition indexOfImplMethod = new(
                name: "IndexOf"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        elementType.GetAbiType(interopReferences).Import(module),
                        module.CorLibTypeFactory.UInt32.MakePointerType(),
                        module.CorLibTypeFactory.Boolean.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(readOnlyListType.Import(module));
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction ldc_i4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter1Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            MemberReference adapterIndexOfMethod;

            // Get the target 'IndexOf' method (we can optimize for 'string' types)
            if (elementType.IsTypeOfString())
            {
                adapterIndexOfMethod = SignatureComparer.IgnoreVersion.Equals(readOnlyListType.GenericType, interopReferences.IReadOnlyList1)
                    ? interopReferences.IReadOnlyListAdapterOfStringIndexOf
                    : interopReferences.IListAdapterOfStringIndexOf;
            }
            else
            {
                // Otherwise use the provided method directly (it will always be valid)
                adapterIndexOfMethod = indexOfMethod;
            }

            // Create a method body for the 'IndexOf' method
            indexOfImplMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // Return 'E_POINTER' if either argument is 'null'
                    { Ldarg_2 },
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(readOnlyListType).Import(module) },
                    { Stloc_0 },
                    { Ldarg_3 },
                    { Ldloc_0 },
                    { nop_parameter1Rewrite },
                    { Ldarg_2 },
                    { Call, adapterIndexOfMethod.Import(module) },
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
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            // If the element type is 'string', we use 'ReadOnlySpan<char>' to avoid an allocation
            TypeSignature parameterType = elementType.IsTypeOfString()
                ? interopReferences.ReadOnlySpanChar
                : elementType;

            // Track rewriting the parameter for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: parameterType,
                method: indexOfImplMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            return indexOfImplMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetMany</c> export method.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="getManyMethod">The adapter method to invoke on <paramref name="readOnlyListType"/> (if <see langword="null"/>, will be automatically selected).</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <remarks>
        /// This method can also be used to define the <c>GetMany</c> method for <see cref="System.Collections.Generic.IList{T}"/> interfaces.
        /// </remarks>
        public static MethodDefinition GetMany(
            GenericInstanceTypeSignature readOnlyListType,
            IMethodDescriptor? getManyMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Get the appropriate 'GetMany' method descriptor if the caller hasn't provided one
            getManyMethod ??= elementType switch
            {
                _ when elementType.IsBlittable(interopReferences) => interopReferences.IReadOnlyListAdapterBlittableValueTypeGetMany(elementType),
                _ when elementType.IsConstructedKeyValuePairType(interopReferences) => interopReferences.IReadOnlyListAdapterKeyValuePairTypeGetMany(
                    keyType: ((GenericInstanceTypeSignature)elementType).TypeArguments[0],
                    valueType: ((GenericInstanceTypeSignature)elementType).TypeArguments[1],
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsManagedValueType(interopReferences) => interopReferences.IReadOnlyListAdapterManagedValueTypeGetMany(
                    elementType: elementType,
                    abiType: elementType.GetAbiType(interopReferences),
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsValueType => interopReferences.IReadOnlyListAdapterUnmanagedValueTypeGetMany(
                    elementType: elementType,
                    abiType: elementType.GetAbiType(interopReferences),
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsTypeOfObject() => interopReferences.IReadOnlyListAdapterOfObjectGetMany,
                _ when elementType.IsTypeOfString() => interopReferences.IReadOnlyListAdapterOfStringGetMany,
                _ when elementType.IsTypeOfType(interopReferences) => interopReferences.IReadOnlyListAdapterOfTypeGetMany,
                _ when elementType.IsTypeOfException(interopReferences) => interopReferences.IReadOnlyListAdapterOfExceptionGetMany,
                _ => interopReferences.IReadOnlyListAdapterReferenceTypeGetMany(
                    elementType: elementType,
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature())
            };

            // Define the 'GetMany' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetMany(void* thisPtr, uint startIndex, uint itemsSize, <ABI_ELEMENT_TYPE>* items, uint* writtenCount)
            MethodDefinition getManyImplMethod = new(
                name: "GetMany"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.UInt32,
                        elementType.GetAbiType(interopReferences).Import(module).MakePointerType(),
                        module.CorLibTypeFactory.UInt32.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction ldc_I4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_s_4_tryStart = CilInstruction.CreateLdarg(4);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Create a method body for the 'GetMany' method
            getManyImplMethod.CilMethodBody = new CilMethodBody()
            {
                // Declare 1 variable:
                //   [0]: 'int' (the 'HRESULT' to return)
                LocalVariables = { new CilLocalVariable(module.CorLibTypeFactory.Int32) },
                Instructions =
                {
                    // Return 'E_POINTER' if either pointer argument is 'null'
                    { Ldarg_3 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Beq_S, ldc_I4_e_pointer.CreateLabel() },
                    { CilInstruction.CreateLdarg(4) },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { ldc_I4_e_pointer },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_s_4_tryStart },
                    { Ldarg_0 },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(readOnlyListType).Import(module) },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Ldarg_3 },
                    { Call, getManyMethod.Import(module) },
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
                        TryStart = ldarg_s_4_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return getManyImplMethod;
        }
    }
}
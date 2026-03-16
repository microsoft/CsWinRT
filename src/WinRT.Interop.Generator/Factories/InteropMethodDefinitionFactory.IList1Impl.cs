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
    /// Helpers for impl types for <see cref="System.Collections.Generic.IList{T}"/> interfaces.
    /// </summary>
    public static class IList1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetView</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition GetView(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'GetView' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetView(void* thisPtr, void** result)
            MethodDefinition getViewMethod = new(
                name: "GetView"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.Void.MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([listType]) },
                    { Stloc_0 },
                    { Ldarg_1 },
                    { Ldloc_0 },
                    { Call, interopReferences.IListAdapter1GetView(elementType) },
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
                retValType: interopReferences.IReadOnlyList1.MakeGenericReferenceType([elementType]),
                method: getViewMethod,
                marker: nop_convertToUnmanaged);

            return getViewMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>SetAt</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition SetAt(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "SetAt"u8,
                adapterMethod: interopReferences.IListAdapter1SetAt(elementType),
                listType: listType,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>InsertAt</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition InsertAt(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "InsertAt"u8,
                adapterMethod: interopReferences.IListAdapter1InsertAt(elementType),
                listType: listType,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>RemoveAt</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        public static MethodDefinition RemoveAt(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'RemoveAt' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int RemoveAt(void* thisPtr, uint index)
            MethodDefinition removeAtMethod = new(
                name: "RemoveAt"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.UInt32]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'RemoveAt' method
            removeAtMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([listType]) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListAdapter1RemoveAt(elementType) },
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

            return removeAtMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Append</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition Append(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'Append' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Append(void* thisPtr, <ABI_ELEMENT_TYPE> value)
            MethodDefinition appendMethod = new(
                name: "Append"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        elementType.GetAbiType(interopReferences)]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter1Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'Append' method
            appendMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([listType]) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { nop_parameter1Rewrite },
                    { Callvirt, interopReferences.ICollection1Add(elementType) },
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

            // Track rewriting the parameter for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: elementType,
                method: appendMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            return appendMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>RemoveAtEnd</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        public static MethodDefinition RemoveAtEnd(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'RemoveAtEnd' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int RemoveAtEnd(void* thisPtr)
            MethodDefinition removeAtEndMethod = new(
                name: "RemoveAtEnd"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [interopReferences.Void.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'RemoveAtEnd' method
            removeAtEndMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([listType]) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Call, interopReferences.IListAdapter1RemoveAtEnd(elementType) },
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

            return removeAtEndMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>Clear</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        public static MethodDefinition Clear(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'Clear' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Clear(void* thisPtr)
            MethodDefinition clearMethod = new(
                name: "Clear"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [interopReferences.Void.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([listType]) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Callvirt, interopReferences.ICollection1Clear(elementType) },
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

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetMany</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition GetMany(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Get the appropriate 'GetMany' method descriptor for 'IList<T>' types
            IMethodDescriptor getManyMethod = elementType switch
            {
                _ when elementType.IsBlittable(interopReferences) => interopReferences.IListAdapterBlittableValueTypeGetMany(elementType),
                _ when elementType.IsConstructedKeyValuePairType(interopReferences) => interopReferences.IListAdapterKeyValuePairTypeGetMany(
                    keyType: ((GenericInstanceTypeSignature)elementType).TypeArguments[0],
                    valueType: ((GenericInstanceTypeSignature)elementType).TypeArguments[1],
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsConstructedNullableValueType(interopReferences) => interopReferences.IListAdapterNullableTypeGetMany(
                    underlyingType: ((GenericInstanceTypeSignature)elementType).TypeArguments[0],
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsManagedValueType(interopReferences) => interopReferences.IListAdapterManagedValueTypeGetMany(
                    elementType: elementType,
                    abiType: elementType.GetAbiType(interopReferences),
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsValueType => interopReferences.IListAdapterUnmanagedValueTypeGetMany(
                    elementType: elementType,
                    abiType: elementType.GetAbiType(interopReferences),
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature()),
                _ when elementType.IsTypeOfObject() => interopReferences.IListAdapterOfObjectGetMany,
                _ when elementType.IsTypeOfString() => interopReferences.IListAdapterOfStringGetMany,
                _ when elementType.IsTypeOfType(interopReferences) => interopReferences.IListAdapterOfTypeGetMany,
                _ when elementType.IsTypeOfException(interopReferences) => interopReferences.IListAdapterOfExceptionGetMany,
                _ => interopReferences.IListAdapterReferenceTypeGetMany(
                    elementType: elementType,
                    elementMarshallerType: emitState.LookupTypeDefinition(elementType, "ElementMarshaller").ToTypeSignature())
            };

            // Reuse the logic for the implementation method for 'IReadOnlyList<T>' types
            return IReadOnlyList1Impl.GetMany(
                readOnlyListType: listType,
                getManyMethod: getManyMethod,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>ReplaceAll</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition ReplaceAll(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Define the 'ReplaceAll' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int ReplaceAll(void* thisPtr, uint size, <ABI_ELEMENT_TYPE>* items)
            MethodDefinition replaceAllMethod = new(
                name: "ReplaceAll"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.UInt32,
                        elementAbiType.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'uint' (for the 'i' loop variable)
            //   [2]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType);
            CilLocalVariable loc_1_i = new(interopReferences.UInt32);
            CilLocalVariable loc_2_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction ldarg_2_nullCheck = new(Ldarg_2);
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_loopCheck = new(Ldloc_1);
            CilInstruction ldloc_0_loopStart = new(Ldloc_0);
            CilInstruction nop_convertToManaged = new(Nop);
            CilInstruction ldloc_2_returnHResult = new(Ldloc_2);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'ReplaceAll' method
            replaceAllMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_i, loc_2_hresult },
                Instructions =
                {
                    // Return 'S_OK' if the size is '0'
                    { Ldarg_1 },
                    { Brtrue_S, ldarg_2_nullCheck.CreateLabel() },
                    { Ldc_I4_0 },
                    { Ret },

                    // Return 'E_POINTER' if the array is 'null'
                    { ldarg_2_nullCheck },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { Ldc_I4, unchecked((int)0x80004003) },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code to load the list
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([listType]) },
                    { Stloc_0 },

                    // list.Clear();
                    { Ldloc_0 },
                    { Callvirt, interopReferences.ICollection1Clear(elementType) },

                    // int i = 0;
                    { Ldc_I4_0 },
                    { Stloc_1 },
                    { Br_S, ldloc_1_loopCheck.CreateLabel() },

                    // list.Add(<CONVERT_TO_MANAGED>(items[i]));
                    { ldloc_0_loopStart },
                    { Ldarg_2 },
                    { Ldloc_1 },
                    { Conv_U8 },
                    { Sizeof, elementAbiType.ToTypeDefOrRef() },
                    { Conv_I8 },
                    { Mul },
                    { Conv_I },
                    { Add },
                    { CilInstruction.CreateLdind(elementAbiType) },
                    { nop_convertToManaged },
                    { Callvirt, interopReferences.ICollection1Add(elementType) },

                    // i++;
                    { Ldloc_1 },
                    { Ldc_I4_1 },
                    { Add },
                    { Stloc_1 },

                    // if (i < size) goto LoopStart;
                    { ldloc_1_loopCheck },
                    { Ldarg_1 },
                    { Blt_Un_S, ldloc_0_loopStart.CreateLabel() },

                    // return S_OK
                    { Ldc_I4_0 },
                    { Stloc_2 },
                    { Leave_S, ldloc_2_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_2 },
                    { Leave_S, ldloc_2_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [1]
                    { ldloc_2_returnHResult  },
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
                        HandlerEnd = ldloc_2_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            // Track rewriting each item for this method
            emitState.TrackManagedValueMethodRewrite(
                parameterType: elementType,
                method: replaceAllMethod,
                marker: nop_convertToManaged);

            return replaceAllMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>SetAt</c> or <c>InsertAt</c> export method.
        /// </summary>
        /// <param name="methodName">The name of the method to generate.</param>
        /// <param name="adapterMethod">The adapter method to forward the call to.</param>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        private static MethodDefinition SetAtOrInsertAt(
            Utf8String methodName,
            MemberReference adapterMethod,
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // Define the 'SetAt' or 'InsertAt' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int <NAME>(void* thisPtr, uint index, <ABI_ELEMENT_TYPE> value)
            MethodDefinition setAtOrInsertAtMethod = new(
                name: methodName,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Int32,
                    parameterTypes: [
                        interopReferences.Void.MakePointerType(),
                        interopReferences.UInt32,
                        elementType.GetAbiType(interopReferences)]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType);
            CilLocalVariable loc_1_hresult = new(interopReferences.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter2Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);

            // Create a method body for the 'SetAt' or 'InsertAt' method
            setAtOrInsertAtMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod([listType]) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Ldarg_1 },
                    { nop_parameter2Rewrite },
                    { Call, adapterMethod },
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

            // Track rewriting the parameter for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: elementType,
                method: setAtOrInsertAtMethod,
                marker: nop_parameter2Rewrite,
                parameterIndex: 2);

            return setAtOrInsertAtMethod;
        }
    }
}
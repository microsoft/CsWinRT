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

/// <summary>
/// A factory for interop method definitions.
/// </summary>
internal static partial class InteropMethodDefinitionFactory
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
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition GetView(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
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
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType.Import(module));
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(listType).Import(module) },
                    { Stloc_0 },
                    { Ldarg_1 },
                    { Ldloc_0 },
                    { Call, interopReferences.IListAdapter1GetView(elementType).Import(module) },
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
                retValType: interopReferences.IReadOnlyList1.MakeGenericReferenceType(elementType),
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
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition SetAt(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "SetAt"u8,
                adapterMethod: interopReferences.IListAdapter1SetAt(elementType),
                listType: listType,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>InsertAt</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition InsertAt(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            return SetAtOrInsertAt(
                methodName: "InsertAt"u8,
                adapterMethod: interopReferences.IListAdapter1InsertAt(elementType),
                listType: listType,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>RemoveAt</c> export method.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition RemoveAt(
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            ModuleDefinition module)
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
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType.Import(module));
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Create a method body for the 'RemoveAt' method
            removeAtMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(listType).Import(module) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListAdapter1RemoveAt(elementType).Import(module) },
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

            return removeAtMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>SetAt</c> or <c>InsertAt</c> export method.
        /// </summary>
        /// <param name="methodName">The name of the method to generate.</param>
        /// <param name="adapterMethod">The adapter method to forward the call to.</param>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        private static MethodDefinition SetAtOrInsertAt(
            Utf8String methodName,
            MemberReference adapterMethod,
            GenericInstanceTypeSignature listType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
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
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32,
                        elementType.GetAbiType(interopReferences).Import(module)]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Declare the local variables:
            //   [0]: '<READONLY_LIST_TYPE>' (for 'thisObject')
            //   [1]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_thisObject = new(listType.Import(module));
            CilLocalVariable loc_1_hresult = new(module.CorLibTypeFactory.Int32);

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction nop_parameter2Rewrite = new(Nop);
            CilInstruction ldloc_1_returnHResult = new(Ldloc_1);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Create a method body for the 'SetAt' or 'InsertAt' method
            setAtOrInsertAtMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(listType).Import(module) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Ldarg_1 },
                    { nop_parameter2Rewrite },
                    { Call, adapterMethod.Import(module) },
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
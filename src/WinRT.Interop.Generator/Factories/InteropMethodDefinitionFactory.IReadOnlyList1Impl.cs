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

/// <summary>
/// A factory for interop method definitions.
/// </summary>
internal static partial class InteropMethodDefinitionFactory
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
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition GetAt(
            GenericInstanceTypeSignature readOnlyListType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Define the 'GetAt' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetAt(void* thisPtr, uint index, <ABI_ELEMENT_TYPE>* result)
            MethodDefinition getAtMethod = new(
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
            getAtMethod.CilMethodBody = new CilMethodBody()
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
                    { Call, interopReferences.IReadOnlyListAdapter1GetAt(elementType).Import(module) },
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
                method: getAtMethod,
                marker: nop_convertToUnmanaged);

            return getAtMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>get_Size</c> export method.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition get_Size(
            GenericInstanceTypeSignature readOnlyListType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Define the 'get_Size' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Size(void* thisPtr, uint* result)
            MethodDefinition sizeMethod = new(
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
            sizeMethod.CilMethodBody = new CilMethodBody()
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
                    { Call, interopReferences.IReadOnlyListAdapter1Size(elementType).Import(module) },
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

            return sizeMethod;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>IndexOf</c> export method.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition IndexOf(
            GenericInstanceTypeSignature readOnlyListType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Define the 'IndexOf' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int IndexOf(void* thisPtr, <ABI_ELEMENT_TYPE> value, uint* index, bool* result)
            MethodDefinition indexOfMethod = new(
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

            // Get the target 'IndexOf' method (we can optimize for 'string' types)
            MemberReference adapterIndexOfMethod = elementType.IsTypeOfString()
                ? interopReferences.IReadOnlyListAdapterOfStringIndexOf()
                : interopReferences.IReadOnlyListAdapter1IndexOf(elementType);

            // Create a method body for the 'IndexOf' method
            indexOfMethod.CilMethodBody = new CilMethodBody()
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

            // Track rewriting the two parameters for this method
            emitState.TrackManagedParameterMethodRewrite(
                paraneterType: parameterType,
                method: indexOfMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            return indexOfMethod;
        }
    }
}
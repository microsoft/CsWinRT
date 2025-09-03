﻿// Copyright (c) Microsoft Corporation.
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
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for impl types for <see cref="System.Collections.Generic.IEnumerable{T}"/> interfaces.
    /// </summary>
    public static class IEnumerable1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>First</c> export method.
        /// </summary>
        /// <param name="enumerableType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition First(
            GenericInstanceTypeSignature enumerableType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // Define the 'First' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int First(void* thisPtr, void** result)
            MethodDefinition firstMethod = new(
                name: "First"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Reference the generated 'ConvertToUnmanaged' method to marshal the 'IEnumerator<T>' instance to unmanaged
            MemberReference convertToUnmanagedMethod = module
                .CreateTypeReference(
                    ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                    name: InteropUtf8NameFactory.TypeName(interopReferences.IEnumerator1.MakeGenericReferenceType(elementType), "Marshaller"))
                .CreateMemberReference("ConvertToUnmanaged", MethodSignature.CreateStatic(
                    returnType: interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                    parameterTypes: [interopReferences.IEnumerator1.MakeGenericReferenceType(elementType)]));

            // Labels for jumps
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Declare the local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'WindowsRuntimeObjectReferenceValue' (the marshalled 'IEnumerator<T>' instance)
            CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);
            CilLocalVariable loc_1_enumeratorValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature().Import(module));

            // Create a method body for the 'get_Current' method
            firstMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_hresult, loc_1_enumeratorValue },
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(enumerableType).Import(module) },
                    { Callvirt, interopReferences.IEnumerable1GetEnumerator(elementType).Import(module) },
                    { Call, convertToUnmanagedMethod.Import(module) },
                    { Stloc_1 },
                    { Ldloca_S, loc_1_enumeratorValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe.Import(module) },
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
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return firstMethod;
        }
    }
}

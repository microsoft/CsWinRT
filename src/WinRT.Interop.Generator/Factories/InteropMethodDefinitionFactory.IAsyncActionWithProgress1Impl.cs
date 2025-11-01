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
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for impl types for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> interfaces.
    /// </summary>
    public static class IAsyncActionWithProgress1Impl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>GetResults</c> export method.
        /// </summary>
        /// <param name="actionType">The <see cref="TypeSignature"/> for the async action type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        public static MethodDefinition GetResults(
            GenericInstanceTypeSignature actionType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature progressType = actionType.TypeArguments[0];

            // Define the 'GetResults' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int GetResults(void* thisPtr)
            MethodDefinition getResultsMethod = new(
                name: "GetResults"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Declare the local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);

            // Create a method body for the 'get_Current' method
            getResultsMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_hresult },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(actionType).Import(module) },
                    { Callvirt, interopReferences.IAsyncActionWithProgress1GetResults(progressType).Import(module) },
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
                        TryStart = ldarg_0_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            return getResultsMethod;
        }
    }
}

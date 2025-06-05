// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known CIL method bodies.
/// </summary>
internal static class WellKnownCilMethodBodyFactory
{
    /// <summary>
    /// Creates a <see cref="CilMethodBody"/> for a <see cref="System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute"/> method.
    /// </summary>
    /// <param name="interfaceType">The type of the interface being implemented (used to retrieve the reference for).</param>
    /// <param name="implementationMethod">The method being implemented.</param>
    /// <param name="forwardedMethod">The forwarded method with the actual marshalling implementation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <returns>The resulting method body.</returns>
    public static CilMethodBody DynamicInterfaceCastableImplementation(
        TypeSignature interfaceType,
        MethodDefinition implementationMethod,
        MethodDefinition forwardedMethod,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Prepare the method body with the basic setup and the call to the forwarded method
        CilMethodBody body = new(implementationMethod)
        {
            Instructions =
            {
                // Load 'this', cast to 'WindowsRuntimeObject', and call 'GetObjectReferenceForInterface'
                // with the 'RuntimeTypeHandle' value that's loaded for the target interface type.
                { Ldarg_0 },
                { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                { Ldtoken, interfaceType.Import(module).ToTypeDefOrRef() },
                { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                { Nop },
                { Call, forwardedMethod },
                { Ret }
            }
        };

        // Load all the additional parameters, based on the method signature
        for (int i = 1; i <= implementationMethod.Parameters.Count; i++)
        {
            body.Instructions.Add(CilInstruction.CreateLdloc(i));
        }

        // Call the forwarded method and return
        body.Instructions.Add(new CilInstruction(Call, forwardedMethod));
        body.Instructions.Add(new CilInstruction(Ret));

        return body;
    }
}

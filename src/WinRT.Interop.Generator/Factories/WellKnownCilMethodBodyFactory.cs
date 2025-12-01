// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
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
    /// <remarks>
    /// The resulting method body is meant to be used for direct method call forwarding (i.e. not for events).
    /// </remarks>
    public static CilMethodBody DynamicInterfaceCastableImplementation(
        TypeSignature interfaceType,
        MethodDefinition implementationMethod,
        MethodDefinition forwardedMethod,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Prepare the method body with the basic setup and the call to the forwarded method
        CilMethodBody body = new()
        {
            Instructions =
            {
                // Load 'this', cast to 'WindowsRuntimeObject', and call 'GetObjectReferenceForInterface'
                // with the 'RuntimeTypeHandle' value that's loaded for the target interface type.
                { Ldarg_0 },
                { Castclass, interopReferences.WindowsRuntimeObject },
                { Ldtoken, interfaceType.ToTypeDefOrRef() },
                { Call, interopReferences.TypeGetTypeFromHandle },
                { Callvirt, interopReferences.Typeget_TypeHandle },
                { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface },
            }
        };

        // Load all the additional parameters, based on the method signature
        for (int i = 1; i <= implementationMethod.Parameters.Count; i++)
        {
            body.Instructions.Add(CilInstruction.CreateLdarg(i));
        }

        // Call the forwarded method and return
        body.Instructions.Add(new CilInstruction(Call, forwardedMethod));
        body.Instructions.Add(new CilInstruction(Ret));

        return body;
    }

    /// <summary>
    /// Creates a <see cref="CilMethodBody"/> for a <see cref="System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute"/> method.
    /// </summary>
    /// <param name="interfaceType">The type of the interface being implemented (used to retrieve the reference for).</param>
    /// <param name="handlerType">The handler type for the event.</param>
    /// <param name="eventMethod">The method returning the event table to use.</param>
    /// <param name="eventAccessorAttributes">The kind of accessor method to generate.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <returns>The resulting method body.</returns>
    /// <remarks>
    /// The resulting method body is specifically meant to be used for event accessors.
    /// </remarks>
    [SuppressMessage("Style", "IDE0072", Justification = "We're intentionally only handling 'AddOn' and 'RemoveOn'.")]
    public static CilMethodBody DynamicInterfaceCastableImplementation(
        TypeSignature interfaceType,
        TypeSignature handlerType,
        MethodDefinition eventMethod,
        MethodSemanticsAttributes eventAccessorAttributes,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Get the right accessor method to invoke
        MemberReference accessorMethod = eventAccessorAttributes switch
        {
            MethodSemanticsAttributes.AddOn => interopReferences.EventSource1Subscribe(handlerType),
            MethodSemanticsAttributes.RemoveOn => interopReferences.EventSource1Unsubscribe(handlerType),
            _ => throw new ArgumentOutOfRangeException(nameof(eventAccessorAttributes), actualValue: eventAccessorAttributes, message: null)
        };

        // Prepare the method body like above, but specifically for event accessors
        return new()
        {
            LocalVariables =
            {
                new CilLocalVariable(interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature()),
                new CilLocalVariable(interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature())
            },
            Instructions =
            {
                // Cast 'this' and resolve the 'WindowsRuntimeObjectReference' instance for the interface
                { Ldarg_0 },
                { Castclass, interopReferences.WindowsRuntimeObject },
                { Stloc_0 },
                { Ldloc_0 },
                { Ldtoken, interfaceType.ToTypeDefOrRef() },
                { Call, interopReferences.TypeGetTypeFromHandle },
                { Callvirt, interopReferences.Typeget_TypeHandle },
                { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface },
                { Stloc_1 },

                // <EVENT_METHOD>(thisObject, thisReference).<ACCESSOR_METHOD>(value);
                { Ldloc_0 },
                { Ldloc_1 },
                { Call, eventMethod },
                { Ldarg_1 },
                { Callvirt, accessorMethod },
                { Ret }
            }
        };
    }
}
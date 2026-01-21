// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
                { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                { Ldtoken, interfaceType.Import(module).ToTypeDefOrRef() },
                { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
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
    /// <param name="interfaceType1">The type of the first interface being implemented (used to retrieve the reference for).</param>
    /// <param name="interfaceType2">The type of the second interface being implemented (used to retrieve the reference for).</param>
    /// <param name="implementationMethod">The method being implemented.</param>
    /// <param name="forwardedMethod1">The first forwarded method with the actual marshalling implementation.</param>
    /// <param name="forwardedMethod2">The second forwarded method with the actual marshalling implementation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <returns>The resulting method body.</returns>
    /// <remarks>
    /// <para>
    /// The resulting method body is meant to be used for direct method call forwarding (i.e. not for events).
    /// </para>
    /// <para>
    /// This method is used for mixed interface implementations that could be backed by two Windows Runtime interfaces on the native object.
    /// </para>
    /// </remarks>
    public static CilMethodBody DynamicInterfaceCastableImplementation(
        TypeSignature interfaceType1,
        TypeSignature interfaceType2,
        MethodDefinition implementationMethod,
        MethodDefinition forwardedMethod1,
        MethodDefinition forwardedMethod2,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Declare the local variables:
        //   [0]: 'WindowsRuntimeObject' (for 'thisObject')
        //   [1]: 'WindowsRuntimeObjectReference' (for 'interfaceReference')
        CilLocalVariable loc_0_thisObject = new(interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature().Import(module));
        CilLocalVariable loc_1_interfaceReference = new(interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module));

        // Prepare the jump labels
        CilInstruction ldloc_0_type2Check = new(Ldloc_0);
        CilInstruction nop_type1Args = new(Nop);
        CilInstruction nop_type2Args = new(Nop);

        // Prepare the method body with the basic setup and the call to the forwarded method
        CilMethodBody body = new()
        {
            LocalVariables = { loc_0_thisObject, loc_1_interfaceReference },
            Instructions =
            {
                // WindowsRuntimeObject thisObject = (WindowsRuntimeObject)this;
                { Ldarg_0 },
                { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                { Stloc_0 },

                // if (thisObject.TryGetObjectReferenceForInterface(typeof(<INTERFACE_TYPE1>), out interfaceReference))
                { Ldloc_0 },
                { Ldtoken, interfaceType1.Import(module).ToTypeDefOrRef() },
                { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                { Ldloca_S, loc_1_interfaceReference },
                { Callvirt, interopReferences.WindowsRuntimeObjectTryGetObjectReferenceForInterface.Import(module) },
                { Brfalse_S, ldloc_0_type2Check.CreateLabel() },

                // <FORWARDED_METHOD1>(interfaceReference, <ARGS>);
                { Ldloc_1 },
                { nop_type1Args },
                { Call, forwardedMethod1 },
                { Ret },

                // interfaceReference = thisObject.GetObjectReferenceForInterface(typeof(<INTERFACE_TYPE2>));
                { ldloc_0_type2Check },
                { Ldtoken, interfaceType2.Import(module).ToTypeDefOrRef() },
                { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                { Stloc_1 },

                // <FORWARDED_METHOD2>(interfaceReference, <ARGS>);
                { Ldloc_1 },
                { nop_type2Args },
                { Call, forwardedMethod2 },
                { Ret }
            }
        };

        List<CilInstruction> args1 = [];
        List<CilInstruction> args2 = [];

        // Prepare the instructions to load the parameters, based on the method signature
        for (int i = 1; i <= implementationMethod.Parameters.Count; i++)
        {
            args1.Add(CilInstruction.CreateLdarg(i));
            args2.Add(CilInstruction.CreateLdarg(i));
        }

        // Insert the arguments loading
        body.Instructions.ReferenceReplaceRange(nop_type1Args, args1);
        body.Instructions.ReferenceReplaceRange(nop_type2Args, args2);

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
                new CilLocalVariable(interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature().Import(module)),
                new CilLocalVariable(interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module))
            },
            Instructions =
            {
                // Cast 'this' and resolve the 'WindowsRuntimeObjectReference' instance for the interface
                { Ldarg_0 },
                { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                { Stloc_0 },
                { Ldloc_0 },
                { Ldtoken, interfaceType.Import(module).ToTypeDefOrRef() },
                { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                { Stloc_1 },

                // <EVENT_METHOD>(thisObject, thisReference).<ACCESSOR_METHOD>(value);
                { Ldloc_0 },
                { Ldloc_1 },
                { Call, eventMethod },
                { Ldarg_1 },
                { Callvirt, accessorMethod.Import(module) },
                { Ret }
            }
        };
    }
}
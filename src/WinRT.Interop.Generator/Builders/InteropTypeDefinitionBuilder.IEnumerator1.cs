// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.IEnumerator{T}"/> types.
    /// </summary>
    public static class IEnumerator1
    {
        /// <summary>
        /// Creates a new type definition for the methods for an <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="iteratorMethodsType">The resulting methods type.</param>
        public static void IIteratorMethods(
            GenericInstanceTypeSignature enumeratorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition iteratorMethodsType)
        {
            // We're declaring an 'internal abstract class' type
            iteratorMethodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "IIteratorMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(iteratorMethodsType);

            // Define the 'Current' method as follows:
            //
            // public static <TYPE_ARGUMENT> Current(WindowsRuntimeObjectReference thisReference)
            MethodDefinition currentMethod = new(
                name: "Current"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: enumeratorType.TypeArguments[0].Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]))
            {
                NoInlining = true
            };

            iteratorMethodsType.Methods.Add(currentMethod);

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'void*' (the native value that was retrieved)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true).Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_currentNative = new(module.CorLibTypeFactory.Void.MakePointerType());

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);

            // Create a method body for the 'Current' method
            currentMethod.CilMethodBody = new CilMethodBody(currentMethod)
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_currentNative },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldloca_S, loc_2_currentNative },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.IEnumerator1Vftbl.GetField("get_Current"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IEnumerator1CurrentImpl(interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },
                    { nop_finallyEnd }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloca_s_0_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerEnd = nop_finallyEnd.CreateLabel()
                    }
                }
            };

            // If the value is blittable, return it directly
            if (enumeratorType.TypeArguments[0].IsValueType) // TODO
            {
                _ = currentMethod.CilMethodBody.Instructions.Add(Ldloc_2);
                _ = currentMethod.CilMethodBody.Instructions.Add(Ret);
            }
            else
            {
                CilInstructionCollection instructions = currentMethod.CilMethodBody.Instructions;

                // Declare an additional variable:
                //   [3]: '<TYPE_ARGUMENT>' (for the marshalled value)
                CilLocalVariable loc_3_current = new(enumeratorType.TypeArguments[0].Import(module));

                currentMethod.CilMethodBody.LocalVariables.Add(loc_3_current);

                // Jump labels for the 'try/finally' blocks
                CilInstruction ldloc_2_tryStart = new(Ldloc_2);
                CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
                CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

                // We need to marshal the native value to a managed object
                instructions.Add(ldloc_2_tryStart);
                _ = instructions.Add(Call, interopReferences.HStringMarshallerConvertToManaged.Import(module));
                _ = instructions.Add(Stloc_3);
                _ = instructions.Add(Leave_S, ldloc_3_finallyEnd.CreateLabel());
                instructions.Add(ldloc_2_finallyStart);
                _ = instructions.Add(Call, interopReferences.HStringMarshallerFree.Import(module));
                _ = instructions.Add(Endfinally);
                instructions.Add(ldloc_3_finallyEnd);
                _ = instructions.Add(Ret);

                // Register the 'try/finally'
                currentMethod.CilMethodBody.ExceptionHandlers.Add(new CilExceptionHandler
                {
                    HandlerType = CilExceptionHandlerType.Finally,
                    TryStart = ldloc_2_tryStart.CreateLabel(),
                    TryEnd = ldloc_2_finallyStart.CreateLabel(),
                    HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                    HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                });
            }

            // Define the 'HasCurrent' method as follows:
            //
            // public static bool HasCurrent(WindowsRuntimeObjectReference thisReference)
            MethodDefinition hasCurrentMethod = new(
                name: "HasCurrent"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]));

            iteratorMethodsType.Methods.Add(hasCurrentMethod);

            // Create a method body for the 'HasCurrent' method
            hasCurrentMethod.CilMethodBody = new CilMethodBody(hasCurrentMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IIteratorMethodsHasCurrent.Import(module) },
                    { Ret }
                }
            };

            // Define the 'MoveNext' method as follows:
            //
            // public static bool HasCurrent(WindowsRuntimeObjectReference thisReference)
            MethodDefinition moveNextMethod = new(
                name: "MoveNext"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]));

            iteratorMethodsType.Methods.Add(moveNextMethod);

            // Create a method body for the 'HasCurrent' method
            moveNextMethod.CilMethodBody = new CilMethodBody(moveNextMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IIteratorMethodsMoveNext.Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type type.</param>
        /// <param name="iteratorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIteratorMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition iteratorMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];
            TypeSignature windowsRuntimeEnumerator1Type = interopReferences.WindowsRuntimeEnumerator1.MakeGenericInstanceType(elementType);

            // We're declaring an 'internal sealed class' type
            nativeObjectType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "NativeObject"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed,
                baseType: windowsRuntimeEnumerator1Type.Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(nativeObjectType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module, interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false));

            nativeObjectType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Call, interopReferences.WindowsRuntimeEnumerator1_ctor(windowsRuntimeEnumerator1Type).Import(module));

            // Define the 'CurrentNative' method as follows:
            //
            // public static <ELEMENT_TYPE> CurrentNative()
            MethodDefinition currentNativeMethod = new(
                name: "CurrentNative"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(elementType.Import(module)));

            nativeObjectType.Methods.Add(currentNativeMethod);

            // Mark the 'CurrentNative' method as overriding the base method
            nativeObjectType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.WindowsRuntimeEnumerator1CurrentNative.Import(module),
                body: currentNativeMethod));

            // Create a method body for the 'CurrentNative' method
            currentNativeMethod.CilMethodBody = new CilMethodBody(currentNativeMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.WindowsRuntimeObjectget_NativeObjectReference.Import(module) },
                    { Call, iteratorMethodsType.GetMethod("Current"u8) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature enumeratorType,
            TypeDefinition nativeObjectType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            // We're declaring an 'internal abstract class' type
            callbackType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "ComWrappersCallback"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeUnsealedObjectComWrappersCallback.Import(module)) }
            };

            module.TopLevelTypes.Add(callbackType);

            // Define the 'TryCreateObject' method as follows:
            //
            // public static bool TryCreateObject(
            //     void* value,
            //     ReadOnlySpan<char> runtimeClassName,
            //     out object? result,
            //     out CreatedWrapperFlags wrapperFlags)
            MethodDefinition tryCreateObjectMethod = new(
                name: "TryCreateObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.ReadOnlySpanChar.ToTypeSignature(isValueType: true).Import(module),
                        module.CorLibTypeFactory.Object.MakeByReferenceType(),
                        interopReferences.CreatedWrapperFlags.MakeByReferenceType().Import(module)]))
            {
                // The last two parameters are '[out]'
                ParameterDefinitions =
                {
                    new ParameterDefinition(sequence: 3, name: null, attributes: ParameterAttributes.Out),
                    new ParameterDefinition(sequence: 4, name: null, attributes: ParameterAttributes.Out)
                }
            };

            callbackType.Methods.Add(tryCreateObjectMethod);

            // Mark the 'CreateObject' method as implementing the interface method
            callbackType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IWindowsRuntimeUnsealedObjectComWrappersCallbackTryCreateObject.Import(module),
                body: tryCreateObjectMethod));

            // Constructor reference for the native object type
            MemberReference nativeObject_ctor = nativeObjectType
                .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false)]));

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'result')
            CilLocalVariable loc_0_result = new(interopReferences.WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false).Import(module));

            // Jump labels
            CilInstruction ldc_i4_0_noFlags = new(Ldc_I4_0);
            CilInstruction stind_i4_setFlags = new(Stind_I4);
            CilInstruction ldarg_3_failure = new(Ldarg_3);

            // Create a method body for the 'TryCreateObject' method
            tryCreateObjectMethod.CilMethodBody = new CilMethodBody(tryCreateObjectMethod)
            {
                LocalVariables = { loc_0_result },
                Instructions =
                {
                    // Compare the runtime class name for the fast path
                    { Ldarg_1 },
                    { Ldstr, InteropUtf8NameFactory.TypeName(enumeratorType) }, // TODO
                    { Call, interopReferences.MemoryExtensionsAsSpanCharString.Import(module) },
                    { Call, interopReferences.MemoryExtensionsSequenceEqualChar.Import(module) },
                    { Brfalse_S, ldarg_3_failure.CreateLabel() },

                    { Ldarg_0 },
                    { Ldnull }, // TODO (IID)
                    { Call, interopReferences.WindowsRuntimeObjectReferenceCreateUnsafe.Import(module) },
                    { Stloc_0 },
                    { Ldarg_3 },
                    { Ldloc_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceGetReferenceTrackerPtrUnsafe.Import(module) },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Beq_S, ldc_i4_0_noFlags.CreateLabel() },
                    { Ldc_I4_1 },
                    { Br_S, stind_i4_setFlags.CreateLabel() },
                    { ldc_i4_0_noFlags },
                    { stind_i4_setFlags },
                    { Ldarg_2 },
                    { Ldloc_0 },
                    { Newobj, nativeObject_ctor.Import(module) },
                    { Stind_Ref },
                    { Ldc_I4_1 },
                    { Ret },

                    // Failure path
                    { ldarg_3_failure },
                    { Ldc_I4_0 },
                    { Stind_I4 },
                    { Ldarg_2 },
                    { Ldnull },
                    { Stind_Ref },
                    { Ldc_I4_0 },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition nativeObjectType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute.Import(module));

            module.TopLevelTypes.Add(marshallerType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module);

            marshallerType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Call, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor.Import(module));

            // The 'ComputeVtables' method returns the 'ComWrappers.ComInterfaceEntry*' type
            PointerTypeSignature computeVtablesReturnType = interopReferences.ComInterfaceEntry.Import(module).MakePointerType();

            // Define the 'ComputeVtables' method as follows:
            //
            // public static ComInterfaceEntry* ComputeVtables(out int count)
            MethodDefinition computeVtablesMethod = new(
                name: "ComputeVtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: computeVtablesReturnType,
                    parameterTypes: [module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
            {
                // The parameter is '[out]'
                ParameterDefinitions = { new ParameterDefinition(sequence: 1, name: null, attributes: ParameterAttributes.Out) }
            };

            marshallerType.Methods.Add(computeVtablesMethod);

            // Mark the 'ComputeVtables' method as overriding the base method
            marshallerType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.WindowsRuntimeComWrappersMarshallerAttributeComputeVtables.Import(module),
                body: computeVtablesMethod));

            // Create a method body for the 'ComputeVtables' method
            computeVtablesMethod.CilMethodBody = new CilMethodBody(computeVtablesMethod)
            {
                Instructions =
                {
                    { Newobj, interopReferences.UnreachableException_ctor.Import(module) },
                    { Throw }
                }
            };

            // Define the 'CreateObject' method as follows:
            //
            // public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
            MethodDefinition createObjectMethod = new(
                name: "CreateObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Object,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.CreatedWrapperFlags.MakeByReferenceType().Import(module)]))
            {
                // The 'wrapperFlags' parameter is '[out]'
                ParameterDefinitions = { new ParameterDefinition(sequence: 2, name: null, attributes: ParameterAttributes.Out) }
            };

            marshallerType.Methods.Add(createObjectMethod);

            // Mark the 'CreateObject' method as overriding the base method
            marshallerType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.WindowsRuntimeComWrappersMarshallerAttributeCreateObject.Import(module),
                body: createObjectMethod));

            // Constructor reference for the native object type
            MemberReference nativeObject_ctor = nativeObjectType
                .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false)]));

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'result')
            CilLocalVariable loc_0_result = new(interopReferences.WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false).Import(module));

            // Jump labels
            CilInstruction ldc_i4_0_noFlags = new(Ldc_I4_0);
            CilInstruction stind_i4_setFlags = new(Stind_I4);

            // Create a method body for the 'CreateObject' method
            createObjectMethod.CilMethodBody = new CilMethodBody(createObjectMethod)
            {
                LocalVariables = { loc_0_result },
                Instructions =
                {
                    { Ldarg_1 },
                    { Ldnull }, // TODO (IID)
                    { Call, interopReferences.WindowsRuntimeObjectReferenceCreateUnsafe.Import(module) },
                    { Stloc_0 },
                    { Ldarg_2 },
                    { Ldloc_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceGetReferenceTrackerPtrUnsafe.Import(module) },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Beq_S, ldc_i4_0_noFlags.CreateLabel() },
                    { Ldc_I4_1 },
                    { Br_S, stind_i4_setFlags.CreateLabel() },
                    { ldc_i4_0_noFlags },
                    { stind_i4_setFlags },
                    { Ldloc_0 },
                    { Newobj, nativeObject_ctor.Import(module) },
                    { Ret },
                }
            };
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
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
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            iteratorMethodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "IIteratorMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IIteratorMethodsImpl1.MakeGenericInstanceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

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

            // Mark the 'Current' method as implementing the interface method
            iteratorMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IIteratorMethodsImpl1Current(elementType).Import(module),
                body: currentMethod));

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
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
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

            // The 'NativeObject' is deriving from 'WindowsRuntimeEnumerator<<ELEMENT_TYPE>, <IITERATOR_METHODS>>'
            TypeSignature windowsRuntimeEnumerator2Type = interopReferences.WindowsRuntimeEnumerator2.MakeGenericInstanceType(
                elementType,
                iteratorMethodsType.ToTypeSignature(isValueType: false));

            // We're declaring an 'internal sealed class' type
            nativeObjectType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "NativeObject"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: windowsRuntimeEnumerator2Type.Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(nativeObjectType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module, interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false));

            nativeObjectType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Call, interopReferences.WindowsRuntimeEnumerator1_ctor(windowsRuntimeEnumerator2Type).Import(module));
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="enumeratorImplType">The type returned by <see cref="ImplType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature enumeratorType,
            TypeDefinition nativeObjectType,
            TypeDefinition enumeratorImplType,
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
                    { Ldstr, enumeratorType.FullName }, // TODO
                    { Call, interopReferences.MemoryExtensionsAsSpanCharString.Import(module) },
                    { Call, interopReferences.MemoryExtensionsSequenceEqualChar.Import(module) },
                    { Brfalse_S, ldarg_3_failure.CreateLabel() },

                    // Create the 'NativeObject' instance to return
                    { Ldarg_0 },
                    { Call, enumeratorImplType.GetMethod("get_IID"u8) },
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
                    { Newobj, nativeObjectType.GetMethod(".ctor"u8) },
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
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="enumeratorImplType">The type returned by <see cref="ImplType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition nativeObjectType,
            TypeDefinition enumeratorImplType,
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
                    { Call, enumeratorImplType.GetMethod("get_IID"u8) },
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
                    { Newobj, nativeObjectType.GetMethod(".ctor"u8) },
                    { Ret },
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="enumeratorImplType">The type returned by <see cref="ImplType"/>.</param>
        /// <param name="enumeratorComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition enumeratorImplType,
            TypeDefinition enumeratorComWrappersCallbackType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "Marshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Prepare the external types we need in the implemented methods
            TypeSignature enumeratorType2 = enumeratorType.Import(module);
            TypeSignature windowsRuntimeObjectReferenceValueType = interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToTypeSignature(isValueType: true);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<ENUMERATOR_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: windowsRuntimeObjectReferenceValueType,
                    parameterTypes: [enumeratorType2]));

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Reference the instantiated 'ConvertToUnmanaged' method for the marshaller
            MethodSpecification windowsRuntimeInterfaceMarshallerConvertToUnmanaged =
                interopReferences.WindowsRuntimeInterfaceMarshallerConvertToUnmanaged
                .MakeGenericInstanceMethod(enumeratorType);

            // Create a method body for the 'ConvertToUnmanaged' method
            convertToUnmanagedMethod.CilMethodBody = new CilMethodBody(convertToUnmanagedMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, enumeratorImplType.GetMethod("get_IID"u8) },
                    { Call, windowsRuntimeInterfaceMarshallerConvertToUnmanaged.Import(module) },
                    { Ret }
                }
            };

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <ENUMERATOR_TYPE> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: enumeratorType2,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            marshallerType.Methods.Add(convertToManagedMethod);

            // Construct a descriptor for 'WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<<ENUMERATOR_CALLBACK_TYPE>>(void*)'
            IMethodDescriptor windowsRuntimeUnsealedObjectMarshallerConvertToManaged =
                interopReferences.WindowsRuntimeUnsealedObjectMarshallerConvertToManaged
                .Import(module)
                .MakeGenericInstanceMethod(enumeratorComWrappersCallbackType.ToTypeSignature(isValueType: false));

            // Create a method body for the 'ConvertToManaged' method
            convertToManagedMethod.CilMethodBody = new CilMethodBody(convertToManagedMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, windowsRuntimeUnsealedObjectMarshallerConvertToManaged },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="iteratorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIteratorMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition iteratorMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(enumeratorType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerator.Import(module)),
                    new InterfaceImplementation(interopReferences.IDisposable.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'IEnumerator<T>.Current' getter method
            MethodDefinition get_IEnumerator1CurrentMethod = new(
                name: $"System.Collections.Generic.IEnumerator<{elementType.FullName}>.get_Current",
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(elementType.Import(module)));

            interfaceImplType.Methods.Add(get_IEnumerator1CurrentMethod);

            // Mark the 'IEnumerator<T>.Current' get accessor method as implementing the interface accessor
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumerator1get_Current(elementType).Import(module),
                body: get_IEnumerator1CurrentMethod));

            // Create a method body for the 'IEnumerator<T>.Current' property
            get_IEnumerator1CurrentMethod.CilMethodBody = new CilMethodBody(get_IEnumerator1CurrentMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                    { Ldtoken, enumeratorType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Call, iteratorMethodsType.GetMethod("Current"u8) },
                    { Ret }
                }
            };

            // Create the 'IEnumerator<T>.Current' property
            PropertyDefinition enumerator1CurrentProperty = new(
                name: $"System.Collections.Generic.IEnumerator<{elementType.FullName}>.get_Current",
                attributes: PropertyAttributes.None,
                signature: new PropertySignature(CallingConventionAttributes.Property, elementType.Import(module), []))
            {
                GetMethod = get_IEnumerator1CurrentMethod
            };

            interfaceImplType.Properties.Add(enumerator1CurrentProperty);

            // Create the 'IEnumerator.Current' getter method
            MethodDefinition get_IEnumeratorCurrentMethod = new(
                name: "System.Collections.IEnumerator.get_Current"u8,
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Object));

            interfaceImplType.Methods.Add(get_IEnumeratorCurrentMethod);

            // Mark the 'IEnumerator.Current' get accessor method as implementing the interface accessor
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumeratorget_Current.Import(module),
                body: get_IEnumeratorCurrentMethod));

            // Create a method body for the 'IEnumerator.Current' property
            get_IEnumeratorCurrentMethod.CilMethodBody = new CilMethodBody(get_IEnumeratorCurrentMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Callvirt, interopReferences.IEnumerator1get_Current(elementType).Import(module) },
                    { Ret }
                }
            };

            // Create the 'IEnumerator.Current' property
            PropertyDefinition enumeratorCurrentProperty = new(
                name: "System.Collections.IEnumerator.get_Current"u8,
                attributes: PropertyAttributes.None,
                signature: new PropertySignature(CallingConventionAttributes.Property, elementType.Import(module), []))
            {
                GetMethod = get_IEnumeratorCurrentMethod
            };

            interfaceImplType.Properties.Add(enumeratorCurrentProperty);

            // Define the 'System.IEnumerator.MoveNext' method
            MethodDefinition moveNextMethod = new(
                name: "System.IEnumerator.MoveNext"u8,
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Boolean));

            interfaceImplType.Methods.Add(moveNextMethod);

            // Mark the 'Reset' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumeratorMoveNext.Import(module),
                body: moveNextMethod));

            // Create a method body for the 'MoveNext' method
            moveNextMethod.CilMethodBody = new CilMethodBody(moveNextMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                    { Ldtoken, enumeratorType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Call, iteratorMethodsType.GetMethod("MoveNext"u8) },
                    { Ret }
                }
            };

            // Define the 'System.IEnumerator.Reset' method
            MethodDefinition resetMethod = new(
                name: "System.IEnumerator.Reset"u8,
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void));

            interfaceImplType.Methods.Add(resetMethod);

            // Mark the 'Reset' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumeratorReset.Import(module),
                body: resetMethod));

            // Create a method body for the 'Reset' method
            resetMethod.CilMethodBody = new CilMethodBody(resetMethod)
            {
                Instructions =
                {
                    { Newobj, interopReferences.NotSupportedException_ctor.Import(module) },
                    { Throw }
                }
            };

            // Define the 'System.IDisposable.Dispose' method
            MethodDefinition disposeMethod = new(
                name: "System.IDisposable.Dispose"u8,
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void));

            interfaceImplType.Methods.Add(disposeMethod);

            // Mark the 'Dispose' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IDisposableDispose.Import(module),
                body: disposeMethod));

            // Create a method body for the 'Dispose' method
            disposeMethod.CilMethodBody = new CilMethodBody(disposeMethod)
            {
                Instructions = { { Ret } }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
        public static void ImplType(
            GenericInstanceTypeSignature enumeratorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType,
            out FieldDefinition iidRvaField)
        {
            // We're declaring an 'internal static class' type
            implType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "Impl"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(implType);

            // The vtable field looks like this:
            //
            // [FixedAddressValueType]
            // private static readonly <IEnumerator1Vftbl> Vftbl;
            FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, interopDefinitions.IEnumerator1Vftbl.ToTypeSignature(isValueType: true))
            {
                CustomAttributes = { new CustomAttribute(interopReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
            };

            implType.Fields.Add(vftblField);

            // Define the 'Current' method
            MethodDefinition currentMethod = InteropMethodDefinitionFactory.IEnumerator1.Current(
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            // Define the 'get_HasCurrent' method
            MethodDefinition hasCurrentMethod = InteropMethodDefinitionFactory.IEnumerator1.HasCurrentOrMoveNext(
                nameUtf8: "get_HasCurrent"u8,
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            // Define the 'MoveNext' method
            MethodDefinition moveNextMethod = InteropMethodDefinitionFactory.IEnumerator1.HasCurrentOrMoveNext(
                nameUtf8: "MoveNext"u8,
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            // Define the 'GetMany' method
            MethodDefinition getManyMethod = InteropMethodDefinitionFactory.IEnumerator1.GetMany(
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            implType.Methods.Add(currentMethod);
            implType.Methods.Add(hasCurrentMethod);
            implType.Methods.Add(moveNextMethod);
            implType.Methods.Add(getManyMethod);

            // Create the static constructor to initialize the vtable
            MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

            // Initialize the enumerator vtable
            cctor.CilMethodBody = new CilMethodBody(cctor)
            {
                Instructions =
                {
                    { Ldsflda, vftblField },
                    { Conv_U },
                    { Call, interopReferences.IInspectableImplget_Vtable.Import(module) },
                    { Ldobj, interopDefinitions.IInspectableVftbl },
                    { Stobj, interopDefinitions.IInspectableVftbl },
                    { Ldsflda, vftblField },
                    { Ldftn, currentMethod },
                    { Stfld, interopDefinitions.IEnumerator1Vftbl.Fields[6] },
                    { Ldsflda, vftblField },
                    { Ldftn, hasCurrentMethod },
                    { Stfld, interopDefinitions.IEnumerator1Vftbl.Fields[7] },
                    { Ldsflda, vftblField },
                    { Ldftn, moveNextMethod },
                    { Stfld, interopDefinitions.IEnumerator1Vftbl.Fields[8] },
                    { Ldsflda, vftblField },
                    { Ldftn, getManyMethod },
                    { Stfld, interopDefinitions.IEnumerator1Vftbl.Fields[9] },
                    { Ret }
                }
            };

            // Create the field for the IID for the enumerator type
            WellKnownMemberDefinitionFactory.IID(
                iidRvaFieldName: InteropUtf8NameFactory.TypeName(enumeratorType, "IID"),
                iidRvaDataType: interopDefinitions.IIDRvaDataSize_16,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(),
                out iidRvaField,
                out PropertyDefinition iidProperty,
                out MethodDefinition get_iidMethod);

            interopDefinitions.RvaFields.Fields.Add(iidRvaField);

            implType.Properties.Add(iidProperty);
            implType.Methods.Add(get_iidMethod);

            // Create the 'Vtable' property
            WellKnownMemberDefinitionFactory.Vtable(
                vftblField: vftblField,
                corLibTypeFactory: module.CorLibTypeFactory,
                out PropertyDefinition vtableProperty,
                out MethodDefinition get_VtableMethod);

            implType.Properties.Add(vtableProperty);
            implType.Methods.Add(get_VtableMethod);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="enumeratorComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition enumeratorComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IIterator`1<{enumeratorType.TypeArguments[0]}>"; // TODO

            ProxyType(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: enumeratorComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }
    }
}

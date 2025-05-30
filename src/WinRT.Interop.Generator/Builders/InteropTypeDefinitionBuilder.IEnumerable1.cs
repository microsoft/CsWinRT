// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.IEnumerable{T}"/> types.
    /// </summary>
    public static class IEnumerable1
    {
        /// <summary>
        /// Creates a new type definition for the interface type for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="interfaceType">The resulting interface type.</param>
        /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
        public static void Interface(
            GenericInstanceTypeSignature enumerableType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceType,
            out FieldDefinition iidRvaField)
        {
            // We're declaring an 'internal abstract class' type
            interfaceType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "Interface"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeInterface.Import(module)) }
            };

            module.TopLevelTypes.Add(interfaceType);

            // Track the type (it's needed by 'IReadOnlyList<T>')
            emitState.TrackTypeDefinition(interfaceType, enumerableType, "Interface");

            // Create the field for the IID for the enumerable type
            WellKnownMemberDefinitionFactory.IID(
                iidRvaFieldName: InteropUtf8NameFactory.TypeName(enumerableType, "IID"),
                iidRvaDataType: interopDefinitions.IIDRvaDataSize_16,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(),
                out iidRvaField,
                out PropertyDefinition iidProperty,
                out MethodDefinition get_iidMethod);

            interopDefinitions.RvaFields.Fields.Add(iidRvaField);

            interfaceType.Properties.Add(iidProperty);
            interfaceType.Methods.Add(get_iidMethod);

            // Mark the 'get_IID' method as implementing the interface method
            interfaceType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IWindowsRuntimeInterfaceget_IID.Import(module),
                body: get_iidMethod));
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="iterableMethodsType">The resulting methods type.</param>
        public static void IIterableMethods(
            GenericInstanceTypeSignature enumerableType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition iterableMethodsType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal static class' type
            iterableMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "IIterableMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(iterableMethodsType);

            // Define the 'First' method as follows:
            //
            // public static IEnumerator<<TYPE_ARGUMENT>> First(WindowsRuntimeObjectReference thisReference)
            MethodDefinition firstMethod = new(
                name: "First"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]))
            {
                NoInlining = true
            };

            iterableMethodsType.Methods.Add(firstMethod);

            // Get the generated 'ConvertToManaged' method to marshal the 'IEnumerator<T>' instance to managed
            MethodDefinition convertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.IEnumerator1.MakeGenericInstanceType(elementType),
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'void*' (the enumerator pointer that was retrieved)
            //   [3]: 'IEnumerator<<TYPE_ARGUMENT>>' (the marshalled enumerator)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true).Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_enumeratorPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_3_enumerator = new(interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module));

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction ldloc_2_tryStart = new(Ldloc_2);
            CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
            CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

            // Create a method body for the 'First' method
            firstMethod.CilMethodBody = new CilMethodBody(firstMethod)
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_enumeratorPtr, loc_3_enumerator },
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
                    { Ldloca_S, loc_2_enumeratorPtr },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.IEnumerable1Vftbl.GetField("First"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IEnumerable1FirstImpl(interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },
                    { nop_finallyEnd },

                    // '.try/.finally' code to marshal the enumerator
                    { ldloc_2_tryStart },
                    { Call, convertToManagedMethod },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_finallyEnd.CreateLabel() },
                    { ldloc_2_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectMarshallerFree.Import(module) },
                    { Endfinally },
                    { ldloc_3_finallyEnd },
                    { Ret }
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
                    },
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloc_2_tryStart.CreateLabel(),
                        TryEnd = ldloc_2_finallyStart.CreateLabel(),
                        HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                        HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <see cref="System.Collections.Generic.IEnumerable{T}"/> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="iterableMethodsType">The type returned by <see cref="IIterableMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="enumerableMethodsType">The resulting methods type.</param>
        public static void IEnumerableMethods(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition iterableMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition enumerableMethodsType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            enumerableMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "IEnumerableMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IEnumerableMethodsImpl1.MakeGenericInstanceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(enumerableMethodsType);

            // Track the type (it's needed by 'IReadOnlyList<T>')
            emitState.TrackTypeDefinition(enumerableMethodsType, enumerableType, "IEnumerableMethods");

            // Define the 'GetEnumerator' method as follows:
            //
            // public static IEnumerator<<TYPE_ARGUMENT>> GetEnumerator(WindowsRuntimeObjectReference thisReference)
            MethodDefinition getEnumeratorMethod = new(
                name: "GetEnumerator"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]));

            enumerableMethodsType.Methods.Add(getEnumeratorMethod);

            // Mark the 'GetEnumerator' method as implementing the interface method
            enumerableMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumerableMethodsImpl1GetEnumerator(elementType).Import(module),
                body: getEnumeratorMethod));

            // Create a method body for the 'GetEnumerator' method
            getEnumeratorMethod.CilMethodBody = new CilMethodBody(getEnumeratorMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, iterableMethodsType.GetMethod("First"u8) },
                    { Ret }
                },
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="iterableMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIterableMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition iterableMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // The 'NativeObject' is deriving from 'WindowsRuntimeEnumerable<<ELEMENT_TYPE>, <IITERABLE_METHODS>>'
            TypeSignature windowsRuntimeEnumerable1Type = interopReferences.WindowsRuntimeEnumerable2.MakeGenericInstanceType(
                elementType,
                iterableMethodsType.ToTypeSignature(isValueType: false));

            // We're declaring an 'internal sealed class' type
            nativeObjectType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "NativeObject"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: windowsRuntimeEnumerable1Type.Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(nativeObjectType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module, interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false));

            nativeObjectType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Call, interopReferences.WindowsRuntimeEnumerator1_ctor(windowsRuntimeEnumerable1Type).Import(module));
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="enumerableImplType">The type returned by <see cref="ImplType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature enumerableType,
            TypeDefinition nativeObjectType,
            TypeDefinition enumerableImplType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            // We're declaring an 'internal abstract class' type
            callbackType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "ComWrappersCallback"),
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
                    { Ldstr, enumerableType.FullName }, // TODO
                    { Call, interopReferences.MemoryExtensionsAsSpanCharString.Import(module) },
                    { Call, interopReferences.MemoryExtensionsSequenceEqualChar.Import(module) },
                    { Brfalse_S, ldarg_3_failure.CreateLabel() },

                    // Create the 'NativeObject' instance to return
                    { Ldarg_0 },
                    { Call, enumerableImplType.GetMethod("get_IID"u8) },
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
        /// Creates a new type definition for the marshaller attribute of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="enumerableImplType">The type returned by <see cref="ImplType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition nativeObjectType,
            TypeDefinition enumerableImplType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "ComWrappersMarshallerAttribute"),
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
                    { Call, enumerableImplType.GetMethod("get_IID"u8) },
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
        /// Creates a new type definition for the marshaller of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="enumerableImplType">The type returned by <see cref="ImplType"/>.</param>
        /// <param name="enumerableComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition enumerableImplType,
            TypeDefinition enumerableComWrappersCallbackType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "Marshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Prepare the external types we need in the implemented methods
            TypeSignature enumerableType2 = enumerableType.Import(module);
            TypeSignature windowsRuntimeObjectReferenceValueType = interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToTypeSignature(isValueType: true);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<ENUMERABLE_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: windowsRuntimeObjectReferenceValueType,
                    parameterTypes: [enumerableType2]));

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Reference the instantiated 'ConvertToUnmanaged' method for the marshaller
            MethodSpecification windowsRuntimeInterfaceMarshallerConvertToUnmanaged =
                interopReferences.WindowsRuntimeInterfaceMarshallerConvertToUnmanaged
                .MakeGenericInstanceMethod(enumerableType);

            // Create a method body for the 'ConvertToUnmanaged' method
            convertToUnmanagedMethod.CilMethodBody = new CilMethodBody(convertToUnmanagedMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, enumerableImplType.GetMethod("get_IID"u8) },
                    { Call, windowsRuntimeInterfaceMarshallerConvertToUnmanaged.Import(module) },
                    { Ret }
                }
            };

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <ENUMERABLE_TYPE> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: enumerableType2,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            marshallerType.Methods.Add(convertToManagedMethod);

            // Construct a descriptor for 'WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<<ENUMERABLE_CALLBACK_TYPE>>(void*)'
            IMethodDescriptor windowsRuntimeUnsealedObjectMarshallerConvertToManaged =
                interopReferences.WindowsRuntimeUnsealedObjectMarshallerConvertToManaged
                .Import(module)
                .MakeGenericInstanceMethod(enumerableComWrappersCallbackType.ToTypeSignature(isValueType: false));

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
        /// Creates a new type definition for the interface implementation of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="iterableMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIterableMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition iterableMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(enumerableType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'IEnumerable<T>.GetEnumerator' method
            MethodDefinition enumerable1GetEnumeratorMethod = new(
                name: $"System.Collections.Generic.IEnumerable<{elementType.FullName}>.GetEnumerator",
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module)));

            interfaceImplType.Methods.Add(enumerable1GetEnumeratorMethod);

            // Mark the 'IEnumerable<T>.GetEnumerator' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumerable1GetEnumerator(elementType).Import(module),
                body: enumerable1GetEnumeratorMethod));

            // Create a method body for the 'IEnumerable<T>.GetEnumerator' method
            enumerable1GetEnumeratorMethod.CilMethodBody = new CilMethodBody(enumerable1GetEnumeratorMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                    { Ldtoken, enumerableType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Call, iterableMethodsType.GetMethod("First"u8) },
                    { Ret }
                }
            };

            // Create the 'IEnumerable.GetEnumerator' method
            MethodDefinition enumerableGetEnumeratorMethod = new(
                name: "System.Collections.IEnumerable.GetEnumerator"u8,
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator.Import(module).ToTypeSignature(isValueType: false)));

            interfaceImplType.Methods.Add(enumerableGetEnumeratorMethod);

            // Mark the 'IEnumerable.GetEnumerator' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumerableGetEnumerator.Import(module),
                body: enumerableGetEnumeratorMethod));

            // Create a method body for the 'IEnumerable.GetEnumerator' method
            enumerableGetEnumeratorMethod.CilMethodBody = new CilMethodBody(enumerableGetEnumeratorMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Callvirt, interopReferences.IEnumerable1GetEnumerator(elementType).Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="interfaceType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition interfaceType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // We're declaring an 'internal static class' type
            implType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "Impl"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(implType);

            // The vtable field looks like this:
            //
            // [FixedAddressValueType]
            // private static readonly <IEnumerable1Vftbl> Vftbl;
            FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, interopDefinitions.IEnumerable1Vftbl.ToTypeSignature(isValueType: true))
            {
                CustomAttributes = { new CustomAttribute(interopReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
            };

            implType.Fields.Add(vftblField);

            // Define the 'First' method
            MethodDefinition firstMethod = InteropMethodDefinitionFactory.IEnumerable1.First(
                enumerableType: enumerableType,
                interopReferences: interopReferences,
                module: module);

            implType.Methods.Add(firstMethod);

            // Create the static constructor to initialize the vtable
            MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

            // Initialize the enumerable vtable
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
                    { Ldftn, firstMethod },
                    { Stfld, interopDefinitions.IEnumerable1Vftbl.Fields[6] },
                    { Ret }
                }
            };

            // Create the field for the IID for the enumerable type
            WellKnownMemberDefinitionFactory.IID(
                interfaceType.GetMethod("get_IID"u8),
                interopReferences: interopReferences,
                module: module,
                out PropertyDefinition iidProperty,
                out MethodDefinition get_iidMethod);

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
        /// Creates a new type definition for the proxy type of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="enumerableComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition enumerableComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IIterable`1<{enumerableType.TypeArguments[0]}>"; // TODO

            ProxyType(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: enumerableComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }
    }
}

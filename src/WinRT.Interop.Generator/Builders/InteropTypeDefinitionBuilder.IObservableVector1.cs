// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
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
    /// Helpers for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> types.
    /// </summary>
    public static class IObservableVector1
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="vectorType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature vectorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(vectorType, "IID"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates the cached factory type for the property for the event args for the vector.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="factoryType">The resulting factory type.</param>
        public static void EventSourceFactory(
            GenericInstanceTypeSignature vectorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition factoryType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // We're declaring an 'internal sealed class' type
            factoryType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "EventSourceFactory"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(factoryType);

            // 'Instance' field with the cached factory instance
            factoryType.Fields.Add(new FieldDefinition("Instance"u8, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, factoryType.ToReferenceTypeSignature()));

            // The actual factory is of type 'Func<WindowsRuntimeObject, WindowsRuntimeObjectReference, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>>'
            TypeSignature funcType = interopReferences.Func3.MakeGenericReferenceType(
                interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType(elementType));

            // 'Value' field with the cached factory delegate
            factoryType.Fields.Add(new FieldDefinition("Value"u8, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, funcType.Import(module)));

            // Add the parameterless constructor
            factoryType.Methods.Add(MethodDefinition.CreateDefaultConstructor(module));

            // The key for the lookup below is the associated handler type (which we need to construct), not the interface type
            TypeSignature handlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType(elementType);

            // Get the constructor for the generic event source type
            MethodDefinition eventSourceConstructor = emitState.LookupTypeDefinition(handlerType, "EventSource").GetConstructor(
                comparer: SignatureComparer.IgnoreVersion,
                parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(), interopReferences.CorLibTypeFactory.Int32])!;

            // Define the 'Callback' method as follows:
            //
            // public VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>> Callback(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
            MethodDefinition callbackMethod = new(
                name: "Callback"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType(elementType).Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature().Import(module),
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_2 },
                    { Ldc_I4_6 },
                    { Newobj, eventSourceConstructor },
                    { Ret }
                }
            };

            factoryType.Methods.Add(callbackMethod);

            // We need the static constructor to initialize the static fields
            MethodDefinition cctor = factoryType.GetOrCreateStaticConstructor(module);

            cctor.CilInstructions.Clear();

            // Create a new instance of the factory type and store it in the 'Instance' field
            _ = cctor.CilInstructions.Add(Newobj, factoryType.GetConstructor()!);
            _ = cctor.CilInstructions.Add(Stsfld, factoryType.Fields[0]);

            // Create the delegate type and store it in the 'Value' field
            _ = cctor.CilInstructions.Add(Ldsfld, factoryType.Fields[0]);
            _ = cctor.CilInstructions.Add(Ldftn, callbackMethod);
            _ = cctor.CilInstructions.Add(Newobj, interopReferences.Delegate_ctor(funcType).Import(module));
            _ = cctor.CilInstructions.Add(Stsfld, factoryType.Fields[1]);

            _ = cctor.CilInstructions.Add(Ret);
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="eventSourceFactoryType">The type returned by <see cref="EventSourceFactory"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="methodsType">The resulting methods type.</param>
        public static void Methods(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition eventSourceFactoryType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition methodsType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            methodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IObservableVectorMethodsImpl1.MakeGenericReferenceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(methodsType);

            // Prepare the 'VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>' signature
            TypeSignature eventHandlerEventSourceType = interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType(elementType);

            // Prepare the 'ConditionalWeakTable<WindowsRuntimeObject, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(
                interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                eventHandlerEventSourceType);

            // Define the backing field for 'VectorChangedTable'
            methodsType.Fields.Add(new FieldDefinition(
                name: "<VectorChangedTable>k__BackingField"u8,
                attributes: FieldAttributes.Private | FieldAttributes.Static,
                fieldType: conditionalWeakTableType.MakeModifierType(interopReferences.IsVolatile, isRequired: true).Import(module)));

            // Define the '<get_VectorChangedTable>g__MakeVectorChanged|2_0' method as follows:
            //
            // public ConditionalWeakTable<WindowsRuntimeObject, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>> <get_VectorChangedTable>g__MakeVectorChanged|2_0()
            MethodDefinition makeVectorChangedMethod = new(
                name: "<get_VectorChangedTable>g__MakeVectorChanged|2_0"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(conditionalWeakTableType.Import(module)))
            {
                CilInstructions =
                {
                    // _ = Interlocked.CompareExchange(ref <VectorChangedTable>k__BackingField, value: new(), comparand: null);
                    { Ldsflda, methodsType.Fields[0] },
                    { Newobj, conditionalWeakTableType.ToTypeDefOrRef().CreateConstructorReference(module.CorLibTypeFactory).Import(module) },
                    { Ldnull },
                    { Call, interopReferences.InterlockedCompareExchange1.MakeGenericInstanceMethod(conditionalWeakTableType).Import(module) },
                    { Pop },

                    // return <VectorChangedTable>k__BackingField;
                    { Ldsfld, methodsType.Fields[0] },
                    { Ret }
                }
            };

            methodsType.Methods.Add(makeVectorChangedMethod);

            // Label for the 'ret' (we are doing lazy-init for the backing 'ConditionalWeakTable<,>' field)
            CilInstruction ret = new(Ret);

            // Create the 'VectorChangedTable' getter method
            MethodDefinition get_VectorChangedTableMethod = new(
                name: "get_VectorChangedTable"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(conditionalWeakTableType.Import(module)))
            {
                CilInstructions =
                {
                    { Ldsfld, methodsType.Fields[0] },
                    { Dup },
                    { Brtrue_S, ret.CreateLabel() },
                    { Pop },
                    { Call, makeVectorChangedMethod },
                    { ret }
                }
            };

            methodsType.Methods.Add(get_VectorChangedTableMethod);

            // Create the 'VectorChangedTable' property
            PropertyDefinition enumerator1CurrentProperty = new(
                name: "VectorChangedTable"u8,
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_VectorChangedTableMethod))
            {
                GetMethod = get_VectorChangedTableMethod
            };

            methodsType.Properties.Add(enumerator1CurrentProperty);

            // Prepare the 'ConditionalWeakTable<WindowsRuntimeObject, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>>.GetOrAdd<WindowsRuntimeObjectReference>' method
            MethodSpecification conditionalWeakTableGetOrAddMethod = interopReferences.ConditionalWeakTableGetOrAdd(
                conditionalWeakTableType: conditionalWeakTableType,
                argType: interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature());

            // Create the 'VectorChanged' getter method
            MethodDefinition vectorChangedMethod = new(
                name: "VectorChanged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: eventHandlerEventSourceType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature().Import(module),
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module)]))
            {
                CilInstructions =
                {
                    { Call, get_VectorChangedTableMethod },
                    { Ldarg_0 },
                    { Ldsfld, eventSourceFactoryType.GetField("Value"u8) },
                    { Ldarg_1 },
                    { Callvirt, conditionalWeakTableGetOrAddMethod.Import(module) },
                    { Ret }
                }
            };

            // Add and implement the 'IObservableVectorMethodsImpl<T>.VectorChanged' method
            methodsType.AddMethodImplementation(
                declaration: interopReferences.IObservableVectorMethodsImpl1VectorChanged(elementType).Import(module),
                method: vectorChangedMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="vectorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition vectorMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // Get the base interfaces for the current element type
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(elementType);
            TypeSignature listType = interopReferences.IList1.MakeGenericReferenceType(elementType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeObservableVector<<ELEMENT_TYPE>, ...>'
            TypeSignature windowsRuntimeObservableVector1Type = interopReferences.WindowsRuntimeObservableVector6.MakeGenericReferenceType(
                elementType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(listType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(listType, "IVectorMethods").ToReferenceTypeSignature(),
                vectorMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: vectorType,
                nativeObjectBaseType: windowsRuntimeObservableVector1Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="TypeSignature"/> for the vector type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="vectorType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature vectorType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: vectorType.FullName, // TODO
                typeSignature: vectorType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }
    }
}

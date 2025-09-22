// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
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

            // Define the lazy 'VectorChangedTable' property for the conditional weak table
            InteropMemberDefinitionFactory.LazyVolatileReferenceDefaultConstructorReadOnlyProperty(
                propertyName: "VectorChangedTable",
                index: 2, // Arbitrary index, just copied from what Roslyn does here
                propertyType: conditionalWeakTableType,
                interopReferences: interopReferences,
                module: module,
                backingField: out FieldDefinition vectorChangedTableField,
                factoryMethod: out MethodDefinition makeVectorChangedMethod,
                getAccessorMethod: out MethodDefinition get_VectorChangedTableMethod,
                propertyDefinition: out PropertyDefinition vectorChangedTableProperty);

            methodsType.Fields.Add(vectorChangedTableField);
            methodsType.Methods.Add(makeVectorChangedMethod);
            methodsType.Methods.Add(get_VectorChangedTableMethod);
            methodsType.Properties.Add(vectorChangedTableProperty);

            // Prepare the 'ConditionalWeakTable<WindowsRuntimeObject, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>>.GetOrAdd<WindowsRuntimeObjectReference>' method
            MethodSpecification conditionalWeakTableGetOrAddMethod = interopReferences.ConditionalWeakTable2GetOrAdd(
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

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="vectorType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: vectorType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="vectorComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="vectorType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition vectorComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: vectorType,
                interfaceComWrappersCallbackType: vectorComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="vectorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition vectorMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(vectorType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IList1.MakeGenericReferenceType(elementType).Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.ICollection1.MakeGenericReferenceType(elementType).Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable1.MakeGenericReferenceType(elementType).Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Prepare the 'VectorChangedEventHandler<T>' signature
            TypeSignature handlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType(elementType);

            // Create the 'IObservableVector<T>.VectorChanged' add method
            MethodDefinition add_IObservableVector1VectorChangedMethod = new(
                name: $"Windows.Foundation.Collections.IObservableVector<{elementType.FullName}>.add_VectorChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [handlerType.Import(module)]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: vectorType,
                    handlerType: handlerType,
                    eventMethod: vectorMethodsType.GetMethod("VectorChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.AddOn,
                    interopReferences: interopReferences,
                    module: module)
            };

            // Add and implement the 'IObservableVector<T>.VectorChanged' add accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IObservableVector1add_VectorChanged(elementType).Import(module),
                method: add_IObservableVector1VectorChangedMethod);

            // Create the 'IObservableVector<T>.VectorChanged' remove method
            MethodDefinition remove_IObservableVector1VectorChangedMethod = new(
                name: $"Windows.Foundation.Collections.IObservableVector<{elementType.FullName}>.remove_VectorChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [handlerType.Import(module)]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: vectorType,
                    handlerType: handlerType,
                    eventMethod: vectorMethodsType.GetMethod("VectorChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.RemoveOn,
                    interopReferences: interopReferences,
                    module: module)
            };

            // Add and implement the 'IObservableVector<T>.VectorChanged' remove accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IObservableVector1remove_VectorChanged(elementType).Import(module),
                method: remove_IObservableVector1VectorChangedMethod);

            // Create the 'IObservableVector<T>.VectorChanged' event
            EventDefinition observableVector1VectorChangedProperty = new(
                name: $"Windows.Foundation.Collections.IObservableVector<{elementType.FullName}>.VectorChanged",
                attributes: default,
                eventType: handlerType.Import(module).ToTypeDefOrRef())
            {
                AddMethod = add_IObservableVector1VectorChangedMethod,
                RemoveMethod = remove_IObservableVector1VectorChangedMethod
            };

            interfaceImplType.Events.Add(observableVector1VectorChangedProperty);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="vectorType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature vectorType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // Prepare the 'VectorChangedEventHandler<<ELEMENT_TYPE>>' signature
            TypeSignature eventHandlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType(elementType);

            // Prepare the 'ConditionalWeakTable<<VECTOR_TYPE>, EventRegistrationTokenTable<VectorChangedEventHandler<<ELEMENT_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(
                vectorType,
                interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType));

            // Define the lazy 'VectorChangedTable' property for the conditional weak table
            InteropMemberDefinitionFactory.LazyVolatileReferenceDefaultConstructorReadOnlyProperty(
                propertyName: "VectorChangedTable",
                index: 8, // Arbitrary index, just copied from what Roslyn does here
                propertyType: conditionalWeakTableType,
                interopReferences: interopReferences,
                module: module,
                backingField: out FieldDefinition vectorChangedTableField,
                factoryMethod: out MethodDefinition makeVectorChangedMethod,
                getAccessorMethod: out MethodDefinition get_VectorChangedTableMethod,
                propertyDefinition: out PropertyDefinition vectorChangedTableProperty);

            // Prepare the two exported methods
            MethodDefinition add_VectorChangedMethod = InteropMethodDefinitionFactory.IObservableVector1Impl.add_VectorChanged(
                vectorType: vectorType,
                get_VectorChangedTableMethod: get_VectorChangedTableMethod,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "Impl"),
                vftblType: interopDefinitions.IObservableVectorVftbl,
                get_IidMethod: get_IidMethod,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [add_VectorChangedMethod]);

            // Add the members for the conditional weak table
            implType.Fields.Add(vectorChangedTableField);
            implType.Methods.Add(makeVectorChangedMethod);
            implType.Methods.Add(get_VectorChangedTableMethod);
            implType.Properties.Add(vectorChangedTableProperty);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="vectorComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition vectorComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IObservableVector`1<{vectorType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: vectorComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IObservableVector`1<{vectorType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: vectorType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: vectorType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}

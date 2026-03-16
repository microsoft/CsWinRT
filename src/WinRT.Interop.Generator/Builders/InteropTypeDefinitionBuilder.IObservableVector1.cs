// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
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
        /// Creates a new type definition for the event source factory for an <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="factoryType">The resulting factory type.</param>
        public static void EventSourceFactory(
            GenericInstanceTypeSignature vectorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition factoryType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            factoryType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "EventSourceFactory"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IObservableVectorEventSourceFactory1.MakeGenericReferenceType([elementType]).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(factoryType);

            // The key for the lookup below is the associated handler type (which we need to construct), not the interface type
            TypeSignature handlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType([elementType]);

            // Get the constructor for the generic event source type
            MethodDefinition eventSourceConstructor = emitState.LookupTypeDefinition(handlerType, "EventSource").GetConstructor(
                comparer: SignatureComparer.IgnoreVersion,
                parameterTypes: [
                    interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    interopReferences.Int32])!;

            // Create the 'VectorChanged' method
            MethodDefinition vectorChangedMethod = new(
                name: "VectorChanged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType([elementType]),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldc_I4_6 },
                    { Newobj, eventSourceConstructor },
                    { Ret }
                }
            };

            // Add and implement the 'IObservableVectorEventSourceFactory<T>.VectorChanged' method
            factoryType.AddMethodImplementation(
                declaration: interopReferences.IObservableVectorEventSourceFactory1VectorChanged(elementType),
                method: vectorChangedMethod);
        }

        /// <summary>
        /// Creates the cached callback type for an <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void EventSourceCallback(
            GenericInstanceTypeSignature vectorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // We're declaring an 'internal sealed class' type
            callbackType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "EventSourceCallback"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(callbackType);

            // 'Instance' field with the cached callback instance
            callbackType.Fields.Add(new FieldDefinition("Instance"u8, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, callbackType.ToReferenceTypeSignature()));

            // The actual callback is of type 'Func<WindowsRuntimeObject, WindowsRuntimeObjectReference, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>>'
            TypeSignature funcType = interopReferences.Func3.MakeGenericReferenceType([
                interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType([elementType])]);

            // 'Value' field with the cached callback delegate
            callbackType.Fields.Add(new FieldDefinition("Value"u8, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, funcType));

            // Add the parameterless constructor
            callbackType.Methods.Add(MethodDefinition.CreateDefaultConstructor(interopReferences.CorLibTypeFactory));

            // Get the handler type to use as key (same as above)
            TypeSignature handlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType([elementType]);

            // Get the constructor for the generic event source type (same as above)
            MethodDefinition eventSourceConstructor = emitState.LookupTypeDefinition(handlerType, "EventSource").GetConstructor(
                comparer: SignatureComparer.IgnoreVersion,
                parameterTypes: [
                    interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(), interopReferences.Int32])!;

            // Define the 'Create' method as follows:
            //
            // public VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>> Create(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
            MethodDefinition createMethod = new(
                name: "Create"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType([elementType]),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_2 },
                    { Ldc_I4_6 },
                    { Newobj, eventSourceConstructor },
                    { Ret }
                }
            };

            callbackType.Methods.Add(createMethod);

            // We need the static constructor to initialize the static fields
            MethodDefinition cctor = callbackType.GetOrCreateStaticConstructor(module);

            cctor.CilInstructions.Clear();

            // Create a new instance of the callback type and store it in the 'Instance' field
            _ = cctor.CilInstructions.Add(Newobj, callbackType.GetConstructor()!);
            _ = cctor.CilInstructions.Add(Stsfld, callbackType.Fields[0]);

            // Create the delegate type and store it in the 'Value' field
            _ = cctor.CilInstructions.Add(Ldsfld, callbackType.Fields[0]);
            _ = cctor.CilInstructions.Add(Ldftn, createMethod);
            _ = cctor.CilInstructions.Add(Newobj, interopReferences.Delegate_ctor(funcType));
            _ = cctor.CilInstructions.Add(Stsfld, callbackType.Fields[1]);

            _ = cctor.CilInstructions.Add(Ret);
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="eventSourceCallbackType">The type returned by <see cref="EventSourceCallback"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="methodsType">The resulting methods type.</param>
        public static void Methods(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition eventSourceCallbackType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition methodsType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // We're declaring an 'internal static class' type
            methodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(methodsType);

            // Prepare the 'VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>' signature
            TypeSignature eventHandlerEventSourceType = interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType([elementType]);

            // Prepare the 'ConditionalWeakTable<WindowsRuntimeObject, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType([
                interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                eventHandlerEventSourceType]);

            // Define the lazy 'VectorChangedTable' property for the conditional weak table
            InteropMemberDefinitionFactory.LazyVolatileReferenceDefaultConstructorReadOnlyProperty(
                propertyName: "VectorChangedTable",
                index: 2, // Arbitrary index, just copied from what Roslyn does here
                propertyType: conditionalWeakTableType,
                interopReferences: interopReferences,
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
                    returnType: eventHandlerEventSourceType,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Call, get_VectorChangedTableMethod },
                    { Ldarg_0 },
                    { Ldsfld, eventSourceCallbackType.GetField("Value"u8) },
                    { Ldarg_1 },
                    { Callvirt, conditionalWeakTableGetOrAddMethod },
                    { Ret }
                }
            };

            methodsType.Methods.Add(vectorChangedMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="factoryType">The <see cref="TypeDefinition"/> instance returned by <see cref="EventSourceFactory"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition factoryType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // Get the base interfaces for the current element type
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType([elementType]);
            TypeSignature listType = interopReferences.IList1.MakeGenericReferenceType([elementType]);

            // The 'NativeObject' is deriving from 'WindowsRuntimeObservableVector<<ELEMENT_TYPE>, ...>'
            TypeSignature windowsRuntimeObservableVector1Type = interopReferences.WindowsRuntimeObservableVector6.MakeGenericReferenceType([
                elementType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(listType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(listType, "IVectorMethods").ToReferenceTypeSignature(),
                factoryType.ToReferenceTypeSignature()]);

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
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature vectorType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(vectorType, useWindowsUIXamlProjections),
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
        /// Creates a new type definition for the interface implementation of some <c>IObservableVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="vectorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature vectorType,
            TypeDefinition vectorMethodsType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
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
                CustomAttributes =
                {
                    new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor),
                    InteropCustomAttributeFactory.Guid(vectorType, interopDefinitions, interopReferences, useWindowsUIXamlProjections)
                },
                Interfaces =
                {
                    new InterfaceImplementation(vectorType.ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IList1.MakeGenericReferenceType([elementType]).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.ICollection1.MakeGenericReferenceType([elementType]).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable1.MakeGenericReferenceType([elementType]).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable)
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Prepare the 'VectorChangedEventHandler<T>' signature
            TypeSignature handlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType([elementType]);

            // Create the 'IObservableVector<T>.VectorChanged' add method
            MethodDefinition add_IObservableVector1VectorChangedMethod = new(
                name: $"Windows.Foundation.Collections.IObservableVector<{elementType.FullName}>.add_VectorChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [handlerType]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: vectorType,
                    handlerType: handlerType,
                    eventMethod: vectorMethodsType.GetMethod("VectorChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.AddOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'IObservableVector<T>.VectorChanged' add accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IObservableVector1add_VectorChanged(elementType),
                method: add_IObservableVector1VectorChangedMethod);

            // Create the 'IObservableVector<T>.VectorChanged' remove method
            MethodDefinition remove_IObservableVector1VectorChangedMethod = new(
                name: $"Windows.Foundation.Collections.IObservableVector<{elementType.FullName}>.remove_VectorChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [handlerType]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: vectorType,
                    handlerType: handlerType,
                    eventMethod: vectorMethodsType.GetMethod("VectorChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.RemoveOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'IObservableVector<T>.VectorChanged' remove accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IObservableVector1remove_VectorChanged(elementType),
                method: remove_IObservableVector1VectorChangedMethod);

            // Create the 'IObservableVector<T>.VectorChanged' event
            EventDefinition observableVector1VectorChangedProperty = new(
                name: $"Windows.Foundation.Collections.IObservableVector<{elementType.FullName}>.VectorChanged",
                attributes: default,
                eventType: handlerType.ToTypeDefOrRef())
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
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature vectorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // Prepare the 'VectorChangedEventHandler<<ELEMENT_TYPE>>' signature
            TypeSignature eventHandlerType = interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType([elementType]);

            // Prepare the 'ConditionalWeakTable<<VECTOR_TYPE>, EventRegistrationTokenTable<VectorChangedEventHandler<<ELEMENT_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType([
                vectorType,
                interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType([eventHandlerType])]);

            // Define the lazy 'VectorChangedTable' property for the conditional weak table
            InteropMemberDefinitionFactory.LazyVolatileReferenceDefaultConstructorReadOnlyProperty(
                propertyName: "VectorChangedTable",
                index: 8, // Arbitrary index, just copied from what Roslyn does here
                propertyType: conditionalWeakTableType,
                interopReferences: interopReferences,
                backingField: out FieldDefinition vectorChangedTableField,
                factoryMethod: out MethodDefinition makeVectorChangedMethod,
                getAccessorMethod: out MethodDefinition get_VectorChangedTableMethod,
                propertyDefinition: out PropertyDefinition vectorChangedTableProperty);

            MethodDefinition add_VectorChangedMethod = InteropMethodDefinitionFactory.IObservableVector1Impl.add_VectorChanged(
                vectorType: vectorType,
                get_VectorChangedTableMethod: get_VectorChangedTableMethod,
                interopReferences: interopReferences,
                emitState: emitState);

            MethodDefinition remove_VectorChangedMethod = InteropMethodDefinitionFactory.IObservableVector1Impl.remove_VectorChanged(
                vectorType: vectorType,
                get_VectorChangedTableMethod: get_VectorChangedTableMethod,
                interopReferences: interopReferences);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "Impl"),
                vftblType: interopDefinitions.IObservableVectorVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [add_VectorChangedMethod, remove_VectorChangedMethod]);

            // Add the members for the conditional weak table
            implType.Fields.Add(vectorChangedTableField);
            implType.Methods.Add(makeVectorChangedMethod);
            implType.Methods.Add(get_VectorChangedTableMethod);
            implType.Properties.Add(vectorChangedTableProperty);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, vectorType, "Impl");
        }
    }
}
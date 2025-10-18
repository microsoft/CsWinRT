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
    /// Helpers for <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c> types.
    /// </summary>
    public static class IObservableMap2
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IObservableMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="mapType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature mapType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(mapType, "IID"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates the cached factory type for the property for the event args for the map.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="factoryType">The resulting factory type.</param>
        public static void EventSourceFactory(
            GenericInstanceTypeSignature mapType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition factoryType)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            // We're declaring an 'internal sealed class' type
            factoryType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(mapType),
                name: InteropUtf8NameFactory.TypeName(mapType, "EventSourceFactory"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(factoryType);

            // 'Instance' field with the cached factory instance
            factoryType.Fields.Add(new FieldDefinition("Instance"u8, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, factoryType.ToReferenceTypeSignature()));

            // The actual factory is of type 'Func<WindowsRuntimeObject, WindowsRuntimeObjectReference, MapChangedEventHandlerEventSource<<KEY_TYPE>, <VALUE_TYPE>>>'
            TypeSignature funcType = interopReferences.Func3.MakeGenericReferenceType(
                interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                interopReferences.MapChangedEventHandler2EventSource.MakeGenericReferenceType(keyType, valueType));

            // 'Value' field with the cached factory delegate
            factoryType.Fields.Add(new FieldDefinition("Value"u8, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, funcType.Import(module)));

            // Add the parameterless constructor
            factoryType.Methods.Add(MethodDefinition.CreateDefaultConstructor(module));

            // The key for the lookup below is the associated handler type (which we need to construct), not the interface type
            TypeSignature handlerType = interopReferences.MapChangedEventHandler2.MakeGenericReferenceType(keyType, valueType);

            // Get the constructor for the generic event source type
            MethodDefinition eventSourceConstructor = emitState.LookupTypeDefinition(handlerType, "EventSource").GetConstructor(
                comparer: SignatureComparer.IgnoreVersion,
                parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(), interopReferences.CorLibTypeFactory.Int32])!;

            // Define the 'Callback' method as follows:
            //
            // public MapChangedEventHandlerEventSource<<KEY_TYPE>, <VALUE_TYPE>> Callback(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
            MethodDefinition callbackMethod = new(
                name: "Callback"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.MapChangedEventHandler2EventSource.MakeGenericReferenceType(keyType, valueType).Import(module),
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
        /// Creates a new type definition for the methods for an <c>IObservableMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="eventSourceFactoryType">The type returned by <see cref="EventSourceFactory"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="methodsType">The resulting methods type.</param>
        public static void Methods(
            GenericInstanceTypeSignature mapType,
            TypeDefinition eventSourceFactoryType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition methodsType)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            // We're declaring an 'internal abstract class' type
            methodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(mapType),
                name: InteropUtf8NameFactory.TypeName(mapType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IObservableMapMethodsImpl2.MakeGenericReferenceType(keyType, valueType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(methodsType);

            // Prepare the 'MapChangedEventHandlerEventSource<<KEY_TYPE>, <VALUE_TYPE>>' signature
            TypeSignature eventHandlerEventSourceType = interopReferences.MapChangedEventHandler2EventSource.MakeGenericReferenceType(keyType, valueType);

            // Prepare the 'ConditionalWeakTable<WindowsRuntimeObject, MapChangedEventHandlerEventSource<<KEY_TYPE>, <VALUE_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(
                interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                eventHandlerEventSourceType);

            // Define the lazy 'MapChangedTable' property for the conditional weak table
            InteropMemberDefinitionFactory.LazyVolatileReferenceDefaultConstructorReadOnlyProperty(
                propertyName: "MapChangedTable",
                index: 2, // Arbitrary index, just copied from what Roslyn does here
                propertyType: conditionalWeakTableType,
                interopReferences: interopReferences,
                module: module,
                backingField: out FieldDefinition mapChangedTableField,
                factoryMethod: out MethodDefinition makeMapChangedMethod,
                getAccessorMethod: out MethodDefinition get_MapChangedTableMethod,
                propertyDefinition: out PropertyDefinition mapChangedTableProperty);

            methodsType.Fields.Add(mapChangedTableField);
            methodsType.Methods.Add(makeMapChangedMethod);
            methodsType.Methods.Add(get_MapChangedTableMethod);
            methodsType.Properties.Add(mapChangedTableProperty);

            // Prepare the 'ConditionalWeakTable<WindowsRuntimeObject, MapChangedEventHandlerEventSource<<KEY_TYPE>, <VALUE_TYPE>>>.GetOrAdd<WindowsRuntimeObjectReference>' method
            MethodSpecification conditionalWeakTableGetOrAddMethod = interopReferences.ConditionalWeakTable2GetOrAdd(
                conditionalWeakTableType: conditionalWeakTableType,
                argType: interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature());

            // Create the 'MapChanged' getter method
            MethodDefinition mapChangedMethod = new(
                name: "MapChanged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: eventHandlerEventSourceType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature().Import(module),
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module)]))
            {
                CilInstructions =
                {
                    { Call, get_MapChangedTableMethod },
                    { Ldarg_0 },
                    { Ldsfld, eventSourceFactoryType.GetField("Value"u8) },
                    { Ldarg_1 },
                    { Callvirt, conditionalWeakTableGetOrAddMethod.Import(module) },
                    { Ret }
                }
            };

            // Add and implement the 'IObservableMapMethodsImpl<TKey, TValue>.MapChanged' method
            methodsType.AddMethodImplementation(
                declaration: interopReferences.IObservableMapMethodsImpl2MapChanged(keyType, valueType).Import(module),
                method: mapChangedMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IObservableMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="mapMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature mapType,
            TypeDefinition mapMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            // Get the base interfaces for the current element type
            TypeSignature keyValuePairType = interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);
            TypeSignature dictionaryType = interopReferences.IDictionary2.MakeGenericReferenceType(keyType, valueType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeObservableMap<<KEY_TYPE>, <VALUE_TYPE>, ...>'
            TypeSignature windowsRuntimeObservableMap2Type = interopReferences.WindowsRuntimeObservableMap7.MakeGenericReferenceType(
                keyType,
                valueType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(dictionaryType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(dictionaryType, "IMapMethods").ToReferenceTypeSignature(),
                mapMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: mapType,
                nativeObjectBaseType: windowsRuntimeObservableMap2Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IObservableMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="TypeSignature"/> for the map type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="mapType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature mapType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: mapType.FullName, // TODO
                typeSignature: mapType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IObservableMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="mapType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature mapType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: mapType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IObservableMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="mapComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="mapType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature mapType,
            TypeDefinition mapComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: mapType,
                interfaceComWrappersCallbackType: mapComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IObservableMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="mapMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature mapType,
            TypeDefinition mapMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            // Prepare all the necessary base interface types
            TypeSignature keyValuePairType = interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType);
            TypeSignature dictionaryType = interopReferences.IDictionary2.MakeGenericReferenceType(keyType, valueType);
            TypeSignature collectionType = interopReferences.ICollection1.MakeGenericReferenceType(keyValuePairType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(mapType),
                name: InteropUtf8NameFactory.TypeName(mapType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(mapType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(dictionaryType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(collectionType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(enumerableType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Prepare the 'MapChangedEventHandler<K, V>' signature
            TypeSignature handlerType = interopReferences.MapChangedEventHandler2.MakeGenericReferenceType(keyType, valueType);

            // Create the 'IObservableMap<K, V>.MapChanged' add method
            MethodDefinition add_IObservableMap2MapChangedMethod = new(
                name: $"Windows.Foundation.Collections.IObservableMap<{keyType.FullName},{valueType.FullName}>.add_MapChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [handlerType.Import(module)]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: mapType,
                    handlerType: handlerType,
                    eventMethod: mapMethodsType.GetMethod("MapChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.AddOn,
                    interopReferences: interopReferences,
                    module: module)
            };

            // Add and implement the 'IObservableMap<K, V>.MapChanged' add accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IObservableMap2add_MapChanged(keyType, valueType).Import(module),
                method: add_IObservableMap2MapChangedMethod);

            // Create the 'IObservableMap<K, V>.MapChanged' remove method
            MethodDefinition remove_IObservableMap2MapChangedMethod = new(
                name: $"Windows.Foundation.Collections.IObservableMap<{keyType.FullName},{valueType.FullName}>.remove_MapChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [handlerType.Import(module)]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: mapType,
                    handlerType: handlerType,
                    eventMethod: mapMethodsType.GetMethod("MapChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.RemoveOn,
                    interopReferences: interopReferences,
                    module: module)
            };

            // Add and implement the 'IObservableMap<K, V>.MapChanged' remove accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IObservableMap2remove_MapChanged(keyType, valueType).Import(module),
                method: remove_IObservableMap2MapChangedMethod);

            // Create the 'IObservableMap<K, V>.MapChanged' event
            EventDefinition observableMap2MapChangedProperty = new(
                name: $"Windows.Foundation.Collections.IObservableMap<{keyType.FullName},{valueType.FullName}>.MapChanged",
                attributes: default,
                eventType: handlerType.Import(module).ToTypeDefOrRef())
            {
                AddMethod = add_IObservableMap2MapChangedMethod,
                RemoveMethod = remove_IObservableMap2MapChangedMethod
            };

            interfaceImplType.Events.Add(observableMap2MapChangedProperty);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IObservableMap&lt;K,V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature mapType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            // Prepare the 'MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>' signature
            TypeSignature eventHandlerType = interopReferences.MapChangedEventHandler2.MakeGenericReferenceType(keyType, valueType);

            // Prepare the 'ConditionalWeakTable<<MAP_TYPE>, EventRegistrationTokenTable<MapChangedEventHandler<<KEY_TYPE>, <VALUE_TYPE>>>' signature
            TypeSignature conditionalWeakTableType = interopReferences.ConditionalWeakTable2.MakeGenericReferenceType(
                mapType,
                interopReferences.EventRegistrationTokenTable1.MakeGenericReferenceType(eventHandlerType));

            // Define the lazy 'MapChangedTable' property for the conditional weak table
            InteropMemberDefinitionFactory.LazyVolatileReferenceDefaultConstructorReadOnlyProperty(
                propertyName: "MapChangedTable",
                index: 8, // Arbitrary index, just copied from what Roslyn does here
                propertyType: conditionalWeakTableType,
                interopReferences: interopReferences,
                module: module,
                backingField: out FieldDefinition mapChangedTableField,
                factoryMethod: out MethodDefinition makeMapChangedMethod,
                getAccessorMethod: out MethodDefinition get_MapChangedTableMethod,
                propertyDefinition: out PropertyDefinition mapChangedTableProperty);

            MethodDefinition add_MapChangedMethod = InteropMethodDefinitionFactory.IObservableMap2Impl.add_MapChanged(
                mapType: mapType,
                get_MapChangedTableMethod: get_MapChangedTableMethod,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);

            MethodDefinition remove_MapChangedMethod = InteropMethodDefinitionFactory.IObservableMap2Impl.remove_MapChanged(
                mapType: mapType,
                get_MapChangedTableMethod: get_MapChangedTableMethod,
                interopReferences: interopReferences,
                module: module);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(mapType),
                name: InteropUtf8NameFactory.TypeName(mapType, "Impl"),
                vftblType: interopDefinitions.IObservableMapVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [add_MapChangedMethod, remove_MapChangedMethod]);

            // Add the members for the conditional weak table
            implType.Fields.Add(mapChangedTableField);
            implType.Methods.Add(makeMapChangedMethod);
            implType.Methods.Add(get_MapChangedTableMethod);
            implType.Properties.Add(mapChangedTableProperty);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IObservableMap&lt;K,V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="mapComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature mapType,
            TypeDefinition mapComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            string runtimeClassName = $"Windows.Foundation.Collections.IObservableMap`2<{keyType},{valueType}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(mapType),
                name: InteropUtf8NameFactory.TypeName(mapType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: mapComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IObservableMap&lt;K,V&gt;</c> interface.
        /// </summary>
        /// <param name="mapType">The <see cref="GenericInstanceTypeSignature"/> for the map type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature mapType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature keyType = mapType.TypeArguments[0];
            TypeSignature valueType = mapType.TypeArguments[1];

            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IObservableMap`2<{keyType},{valueType}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: mapType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: mapType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}

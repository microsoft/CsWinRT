// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
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
    /// Helpers for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> types.
    /// </summary>
    public static class IReadOnlyDictionary2
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="readOnlyDictionaryType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(readOnlyDictionaryType, "IID"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the vtable for an <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vftblType">The resulting vtable type.</param>
        public static void Vftbl(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition vftblType)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];

            // All reference types can share the same vtable type (as it just uses 'void*' for the ABI type).
            // The 'IMapView<K, V>' interface doesn't use 'V' as a by-value parameter anywhere in the vtable,
            // so we can aggressively share vtable types for all cases where 'K' is a reference type.
            if (!keyType.IsValueType || keyType.IsKeyValuePairType(interopReferences))
            {
                vftblType = interopDefinitions.IReadOnlyDictionary2Vftbl;

                return;
            }

            // If we already have a vtable type for this key type, we can reuse it.
            // Just like above, this is because the value type doesn't matter here.
            if (emitState.TryGetIMapView2VftblType(keyType, out vftblType!))
            {
                return;
            }

            // Construct a signature using 'object' as the value, and we use that to generate
            // the namespace and type name for the shared vtable type ('object' is a placeholder).
            TypeSignature sharedReadOnlyDictionaryType = interopReferences.IReadOnlyDictionary2.MakeGenericReferenceType(
                keyType,
                module.CorLibTypeFactory.Object);

            // Otherwise, we must construct a new specialized vtable type
            TypeDefinition newVftblType = WellKnownTypeDefinitionFactory.IReadOnlyDictionary2Vftbl(
                ns: InteropUtf8NameFactory.TypeNamespace(sharedReadOnlyDictionaryType),
                name: InteropUtf8NameFactory.TypeName(sharedReadOnlyDictionaryType, "Vftbl"),
                keyType: keyType,
                valueType: module.CorLibTypeFactory.Void,
                interopReferences: interopReferences,
                module: module);

            // Go through the lookup so that we can reuse the vtable later
            vftblType = emitState.GetOrAddIMapView2VftblType(keyType, newVftblType);

            // If we won the race and this is the vtable type that was just created, we can add it to the module
            if (vftblType == newVftblType)
            {
                module.TopLevelTypes.Add(newVftblType);
            }
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="mapViewMethodsType">The resulting methods type.</param>
        public static void IMapViewMethods(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition mapViewMethodsType)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];

            // We're declaring an 'internal abstract class' type
            mapViewMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyDictionaryType),
                name: InteropUtf8NameFactory.TypeName(readOnlyDictionaryType, "IMapViewMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IMapViewMethodsImpl2.MakeGenericReferenceType(keyType, valueType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(mapViewMethodsType);

            // Define the 'HasKey' method as follows:
            //
            // public static bool HasKey(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition hasKeyMethod = new(
                name: "HasKey"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]))
            { NoInlining = true };

            // Add and implement the 'HasKey' method
            mapViewMethodsType.AddMethodImplementation(
                declaration: interopReferences.IMapViewMethodsImpl2HasKey(keyType, valueType).Import(module),
                method: hasKeyMethod);

            // Create a method body for the 'HasKey' method
            hasKeyMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };

            // Define the 'Lookup' method as follows:
            //
            // public static <VALUE_TYPE> Lookup(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition lookupMethod = new(
                name: "Lookup"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: valueType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]))
            { NoInlining = true };

            // Add and implement the 'Lookup' method
            mapViewMethodsType.AddMethodImplementation(
                declaration: interopReferences.IMapViewMethodsImpl2HasKey(keyType, valueType).Import(module),
                method: lookupMethod);

            // Create a method body for the 'Lookup' method
            lookupMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="mapViewMethodsType">The type returned by <see cref="IMapViewMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="readOnlyDictionaryMethodsType">The resulting methods type.</param>
        public static void IReadOnlyDictionaryMethods(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition mapViewMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition readOnlyDictionaryMethodsType)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];

            // We're declaring an 'internal static class' type
            readOnlyDictionaryMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyDictionaryType),
                name: InteropUtf8NameFactory.TypeName(readOnlyDictionaryType, "IReadOnlyDictionaryMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(readOnlyDictionaryMethodsType);

            // Define the 'Item' getter method as follows:
            //
            // public static <VALUE_TYPE> Item(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition get_ItemMethod = new(
                name: "Item"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: valueType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]));

            readOnlyDictionaryMethodsType.Methods.Add(get_ItemMethod);

            // Create a method body for the 'Item' method
            get_ItemMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IReadOnlyDictionaryMethods2get_Item(keyType, valueType, mapViewMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Count' method as follows:
            //
            // public static int Count(WindowsRuntimeObjectReference thisReference)
            MethodDefinition countMethod = new(
                name: "Count"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]));

            readOnlyDictionaryMethodsType.Methods.Add(countMethod);

            // Create a method body for the 'Count' method
            countMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IReadOnlyDictionaryMethodsCount.Import(module) },
                    { Ret }
                }
            };

            // Define the 'ContainsKey' method as follows:
            //
            // public static bool ContainsKey(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition containsKeyMethod = new(
                name: "ContainsKey"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]));

            readOnlyDictionaryMethodsType.Methods.Add(containsKeyMethod);

            // Create a method body for the 'ContainsKey' method
            containsKeyMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IReadOnlyDictionaryMethods2ContainsKey(keyType, valueType, mapViewMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'TryGetValue' method as follows:
            //
            // public static bool TryGetValue(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key, out <VALUE_TYPE> value)
            MethodDefinition tryGetValueMethod = new(
                name: "TryGetValue"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module),
                        valueType.Import(module).MakeByReferenceType()]))
            { CilOutParameterIndices = [3] };

            readOnlyDictionaryMethodsType.Methods.Add(tryGetValueMethod);

            // Create a method body for the 'TryGetValue' method
            tryGetValueMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_3 },
                    { Call, interopReferences.IReadOnlyDictionaryMethods2TryGetValue(keyType, valueType, mapViewMethodsType).Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="mapViewMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IMapViewMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition mapViewMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];
            TypeSignature keyValuePairType = interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeReadOnlyDictionary<<KEY_TYPE>, <VALUE_TYPE>, <IENUMERABLE_INTERFACE>, <IITERABLE_METHODS, <IMAPVIEW_METHODS>>'
            TypeSignature windowsRuntimeReadOnlyDictionary5Type = interopReferences.WindowsRuntimeReadOnlyDictionary5.MakeGenericReferenceType(
                keyType,
                valueType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToReferenceTypeSignature(),
                mapViewMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: readOnlyDictionaryType,
                nativeObjectBaseType: windowsRuntimeReadOnlyDictionary5Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature readOnlyDictionaryType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: readOnlyDictionaryType.FullName, // TODO
                typeSignature: readOnlyDictionaryType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: readOnlyDictionaryType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="readOnlyDictionaryComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition readOnlyDictionaryComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: readOnlyDictionaryType,
                interfaceComWrappersCallbackType: readOnlyDictionaryComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="readOnlyDictionaryMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IReadOnlyDictionaryMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition readOnlyDictionaryMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];
            TypeSignature keyValuePairType = interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType);
            TypeSignature readOnlyCollectionType = interopReferences.IReadOnlyCollection1.MakeGenericReferenceType(keyValuePairType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyDictionaryType),
                name: InteropUtf8NameFactory.TypeName(readOnlyDictionaryType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(readOnlyDictionaryType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(readOnlyCollectionType.Import(module).ToTypeDefOrRef())
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'get_Item' getter method
            MethodDefinition get_ItemMethod = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.get_Item",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(valueType.Import(module), keyType.Import(module)));

            // Add and implement the 'get_Item' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyDictionary2get_Item(keyType, valueType).Import(module),
                method: get_ItemMethod);

            // Create a body for the 'get_Item' method
            get_ItemMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: readOnlyDictionaryType,
                implementationMethod: get_ItemMethod,
                forwardedMethod: readOnlyDictionaryMethodsType.GetMethod("Item"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Item' property
            PropertyDefinition itemProperty = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.Item",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_ItemMethod))
            { GetMethod = get_ItemMethod };

            interfaceImplType.Properties.Add(itemProperty);

            // Create the 'get_Keys' getter method
            MethodDefinition get_KeysMethod = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.get_Keys",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerable1.MakeGenericReferenceType(keyType).Import(module)));

            // Add and implement the 'get_Keys' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyDictionary2get_Keys(keyType, valueType).Import(module),
                method: get_KeysMethod);

            // Create a body for the 'get_Keys' method
            get_KeysMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Newobj, interopReferences.ReadOnlyDictionaryKeyCollection2_ctor(keyType, valueType).Import(module) },
                    { Ret }
                }
            };

            // Create the 'Keys' property
            PropertyDefinition keysProperty = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.Keys",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_KeysMethod))
            { GetMethod = get_KeysMethod };

            interfaceImplType.Properties.Add(keysProperty);

            // Create the 'get_Values' getter method
            MethodDefinition get_ValuesMethod = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.get_Values",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerable1.MakeGenericReferenceType(valueType).Import(module)));

            // Add and implement the 'get_Values' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyDictionary2get_Values(keyType, valueType).Import(module),
                method: get_ValuesMethod);

            // Create a body for the 'get_Values' method
            get_ValuesMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Newobj, interopReferences.ReadOnlyDictionaryValueCollection2_ctor(keyType, valueType).Import(module) },
                    { Ret }
                }
            };

            // Create the 'Values' property
            PropertyDefinition valuesProperty = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.Values",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_ValuesMethod))
            { GetMethod = get_ValuesMethod };

            interfaceImplType.Properties.Add(valuesProperty);

            // Create the 'ContainsKey' method
            MethodDefinition containsKeyMethod = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.ContainsKey",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyType.Import(module)]));

            // Add and implement the 'ContainsKey' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyDictionary2ContainsKey(keyType, valueType).Import(module),
                method: containsKeyMethod);

            // Create a body for the 'ContainsKey' method
            containsKeyMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: readOnlyDictionaryType,
                implementationMethod: containsKeyMethod,
                forwardedMethod: readOnlyDictionaryMethodsType.GetMethod("ContainsKey"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'TryGetValue' method
            MethodDefinition tryGetValueMethod = new(
                name: $"System.Collections.Generic.IReadOnlyDictionary<{keyType.FullName},{valueType.FullName}>.TryGetValue",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyType.Import(module), valueType.Import(module).MakeByReferenceType()]))
            { CilOutParameterIndices = [2] };

            // Add and implement the 'TryGetValue' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyDictionary2ContainsKey(keyType, valueType).Import(module),
                method: tryGetValueMethod);

            // Create a body for the 'TryGetValue' method
            tryGetValueMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: readOnlyDictionaryType,
                implementationMethod: tryGetValueMethod,
                forwardedMethod: readOnlyDictionaryMethodsType.GetMethod("TryGetValue"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'get_Count' getter method
            MethodDefinition get_CountMethod = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.get_Count",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Int32));

            // Add and implement the 'get_Count' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyCollection1get_Count(keyValuePairType).Import(module),
                method: get_CountMethod);

            // Create a body for the 'get_Count' method
            get_CountMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: readOnlyCollectionType,
                implementationMethod: get_CountMethod,
                forwardedMethod: readOnlyDictionaryMethodsType.GetMethod("Count"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Count' property
            PropertyDefinition countProperty = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Count",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CountMethod))
            { GetMethod = get_CountMethod };

            interfaceImplType.Properties.Add(countProperty);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyDictionaryType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition vftblType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyDictionaryType),
                name: InteropUtf8NameFactory.TypeName(readOnlyDictionaryType, "Impl"),
                vftblType: vftblType,
                get_IidMethod: get_IidMethod,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: []);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, readOnlyDictionaryType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="readOnlyDictionaryComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition readOnlyDictionaryComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];

            string runtimeClassName = $"Windows.Foundation.Collections.IMapView`2<{keyType},{valueType}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyDictionaryType),
                name: InteropUtf8NameFactory.TypeName(readOnlyDictionaryType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: readOnlyDictionaryComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IMapView&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyDictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature readOnlyDictionaryType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature keyType = readOnlyDictionaryType.TypeArguments[0];
            TypeSignature valueType = readOnlyDictionaryType.TypeArguments[1];

            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IMapView`2<{keyType},{valueType}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: readOnlyDictionaryType,
                proxyTypeMapSourceType: readOnlyDictionaryType,
                proxyTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                interfaceTypeMapSourceType: readOnlyDictionaryType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}

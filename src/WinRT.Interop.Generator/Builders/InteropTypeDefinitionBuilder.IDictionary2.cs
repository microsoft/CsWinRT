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
    /// Helpers for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> types.
    /// </summary>
    public static class IDictionary2
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="dictionaryType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature dictionaryType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(dictionaryType, "IID"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the vtable for an <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vftblType">The resulting vtable type.</param>
        public static void Vftbl(
            GenericInstanceTypeSignature dictionaryType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition vftblType)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            bool isKeyReferenceType = !keyType.IsValueType || keyType.IsKeyValuePairType(interopReferences);
            bool isValueReferenceType = !valueType.IsValueType || valueType.IsKeyValuePairType(interopReferences);

            // We can share the vtable type for 'void*' when both key and value types are reference types
            if (isKeyReferenceType && isValueReferenceType)
            {
                vftblType = interopDefinitions.IReadOnlyDictionary2Vftbl;

                return;
            }

            // If both the key and the value types are not reference types, we can't possibly share
            // the vtable type. So in this case, we just always construct a specialized new type.
            if (!isKeyReferenceType && !isValueReferenceType)
            {
                vftblType = WellKnownTypeDefinitionFactory.IReadOnlyDictionary2Vftbl(
                    ns: InteropUtf8NameFactory.TypeNamespace(dictionaryType),
                    name: InteropUtf8NameFactory.TypeName(dictionaryType, "Vftbl"),
                    keyType: keyType,
                    valueType: valueType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(vftblType);

                return;
            }

            // Helper to create vtable types that can be shared between multiple key/value types
            static void GetOrCreateVftbl(
                TypeSignature keyType,
                TypeSignature valueType,
                TypeSignature displayKeyType,
                TypeSignature displayValueType,
                InteropReferences interopReferences,
                InteropGeneratorEmitState emitState,
                ModuleDefinition module,
                out TypeDefinition vftblType)
            {
                // If we already have a vtable type for this pair, reuse that
                if (emitState.TryGetIMap2VftblType(keyType, valueType, out vftblType!))
                {
                    return;
                }

                // Create a dummy signature just to generate the mangled name for the vtable type
                TypeSignature sharedReadOnlyDictionaryType = interopReferences.IDictionary2.MakeGenericReferenceType(
                    displayKeyType,
                    displayValueType);

                // Construct a new specialized vtable type
                TypeDefinition newVftblType = WellKnownTypeDefinitionFactory.IDictionary2Vftbl(
                    ns: InteropUtf8NameFactory.TypeNamespace(sharedReadOnlyDictionaryType),
                    name: InteropUtf8NameFactory.TypeName(sharedReadOnlyDictionaryType, "Vftbl"),
                    keyType: keyType,
                    valueType: valueType,
                    interopReferences: interopReferences,
                    module: module);

                // Go through the lookup so that we can reuse the vtable later
                vftblType = emitState.GetOrAddIMap2VftblType(keyType, valueType, newVftblType);

                // If we won the race and this is the vtable type that was just created, we can add it to the module
                if (vftblType == newVftblType)
                {
                    module.TopLevelTypes.Add(newVftblType);
                }
            }

            // Get or create a shared vtable where the reference type is replaced with just 'void*'
            if (isKeyReferenceType)
            {
                GetOrCreateVftbl(
                    keyType: module.CorLibTypeFactory.Void.MakePointerType(),
                    valueType: valueType,
                    displayKeyType: module.CorLibTypeFactory.Object,
                    displayValueType: valueType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out vftblType);
            }
            else
            {
                GetOrCreateVftbl(
                    keyType: keyType,
                    valueType: module.CorLibTypeFactory.Void.MakePointerType(),
                    displayKeyType: keyType,
                    displayValueType: module.CorLibTypeFactory.Object,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out vftblType);
            }
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="mapMethodsType">The resulting methods type.</param>
        public static void IMapMethods(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition mapMethodsType)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            // We're declaring an 'internal abstract class' type
            mapMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(dictionaryType),
                name: InteropUtf8NameFactory.TypeName(dictionaryType, "IMapMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IMapMethodsImpl2.MakeGenericReferenceType(keyType, valueType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(mapMethodsType);

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
            mapMethodsType.AddMethodImplementation(
                declaration: interopReferences.IMapMethodsImpl2HasKey(keyType, valueType).Import(module),
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
            mapMethodsType.AddMethodImplementation(
                declaration: interopReferences.IMapMethodsImpl2Lookup(keyType, valueType).Import(module),
                method: lookupMethod);

            // Create a method body for the 'Lookup' method
            lookupMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };

            // Define the 'Insert' method as follows:
            //
            // public static bool Insert(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key, <VALUE_TYPE> value)
            MethodDefinition insertMethod = new(
                name: "Insert"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module),
                        valueType.Import(module)]))
            { NoInlining = true };

            // Add and implement the 'Insert' method
            mapMethodsType.AddMethodImplementation(
                declaration: interopReferences.IMapMethodsImpl2Insert(keyType, valueType).Import(module),
                method: insertMethod);

            // Create a method body for the 'Insert' method
            insertMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };

            // Define the 'Remove' method as follows:
            //
            // public static void Remove(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition removeMethod = new(
                name: "Remove"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]))
            { NoInlining = true };

            // Add and implement the 'Remove' method
            mapMethodsType.AddMethodImplementation(
                declaration: interopReferences.IMapMethodsImpl2Remove(keyType, valueType).Import(module),
                method: removeMethod);

            // Create a method body for the 'Remove' method
            removeMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="mapMethodsType">The type returned by <see cref="IMapMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="dictionaryMethodsType">The resulting methods type.</param>
        public static void IDictionaryMethods(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition mapMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition dictionaryMethodsType)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            // We're declaring an 'internal static class' type
            dictionaryMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(dictionaryType),
                name: InteropUtf8NameFactory.TypeName(dictionaryType, "IDictionaryMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(dictionaryMethodsType);

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
                        keyType.Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2get_Item(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(get_ItemMethod);

            // Define the 'Item' setter method as follows:
            //
            // public static void Item(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key, <VALUE_TYPE> value)
            MethodDefinition set_ItemMethod = new(
                name: "Item"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module),
                        valueType.Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IDictionaryMethods2set_Item(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(set_ItemMethod);

            // Define the 'Add' method as follows:
            //
            // public static void Add(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key, <VALUE_TYPE> value)
            MethodDefinition addMethod = new(
                name: "Add"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module),
                        valueType.Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IDictionaryMethods2Add(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(addMethod);

            // Define the 'Remove' method as follows:
            //
            // public static bool Remove(WindowsRuntimeObjectReference thisReference, <KEY_TYPE> key)
            MethodDefinition removeMethod = new(
                name: "Remove"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        keyType.Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2Remove(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(removeMethod);

            // Define the 'Count' method as follows:
            //
            // public static int Count(WindowsRuntimeObjectReference thisReference)
            MethodDefinition countMethod = new(
                name: "Count"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IDictionaryMethodsCount.Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(countMethod);

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
                        keyType.Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2ContainsKey(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(containsKeyMethod);

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
            {
                CilOutParameterIndices = [3],
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IDictionaryMethods2TryGetValue(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(tryGetValueMethod);

            // Define the 'Add' ('KeyValuePair<,>') method as follows:
            //
            // public static void Add(WindowsRuntimeObjectReference thisReference, KeyValuePair<<KEY_TYPE>, <VALUE_TYPE>> item)
            MethodDefinition addKeyValuePairMethod = new(
                name: "Add"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2AddKeyValuePair(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(addKeyValuePairMethod);

            // Define the 'Remove' ('KeyValuePair<,>') method as follows:
            //
            // public static void Remove(WindowsRuntimeObjectReference thisReference, KeyValuePair<<KEY_TYPE>, <VALUE_TYPE>> item)
            MethodDefinition removeKeyValuePairMethod = new(
                name: "Remove"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2RemoveKeyValuePair(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(removeKeyValuePairMethod);

            // Define the 'Clear' method as follows:
            //
            // public static void Clear(WindowsRuntimeObjectReference thisReference)
            MethodDefinition clearMethod = new(
                name: "Clear"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IDictionaryMethodsClear.Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(clearMethod);

            // Define the 'Contains' method as follows:
            //
            // public static bool Contains(WindowsRuntimeObjectReference thisReference, KeyValuePair<<KEY_TYPE>, <VALUE_TYPE>> item)
            MethodDefinition containsMethod = new(
                name: "Contains"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2Contains(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(containsMethod);

            // We need to pass the 'IIterableMethods' type as a second type argument, as it's needed to enumerate key-value pairs
            TypeDefinition iterableMethodsType = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.IEnumerable1.MakeGenericReferenceType(interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType)),
                key: "IIterableMethods");

            // Define the 'CopyTo' method as follows:
            //
            // public static void CopyTo(
            //     WindowsRuntimeObjectReference thisIMapReference,
            //     WindowsRuntimeObjectReference thisIIterableReference,
            //     KeyValuePair<<KEY_TYPE>, <VALUE_TYPE>>[] array,
            //     int arrayIndex)
            MethodDefinition copyToMethod = new(
                name: "CopyTo"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module).MakeSzArrayType(),
                        module.CorLibTypeFactory.Int32]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Ldarg_3 },
                    { Call, interopReferences.IDictionaryMethods2CopyTo(keyType, valueType, mapMethodsType, iterableMethodsType).Import(module) },
                    { Ret }
                }
            };

            dictionaryMethodsType.Methods.Add(copyToMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="mapMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IMapMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition mapMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];
            TypeSignature keyValuePairType = interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeDictionary<<KEY_TYPE>, <VALUE_TYPE>, <IENUMERABLE_INTERFACE>, <IITERABLE_METHODS, <IMAP_METHODS>>'
            TypeSignature windowsRuntimeDictionary5Type = interopReferences.WindowsRuntimeDictionary5.MakeGenericReferenceType(
                keyType,
                valueType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToReferenceTypeSignature(),
                mapMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: dictionaryType,
                nativeObjectBaseType: windowsRuntimeDictionary5Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="dictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature dictionaryType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: dictionaryType.FullName, // TODO
                typeSignature: dictionaryType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="dictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: dictionaryType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="dictionaryComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="dictionaryType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition dictionaryComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: dictionaryType,
                interfaceComWrappersCallbackType: dictionaryComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="dictionaryMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IDictionaryMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition dictionaryMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];
            TypeSignature keyValuePairType = interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType);
            TypeSignature collectionType = interopReferences.ICollection1.MakeGenericReferenceType(keyValuePairType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(dictionaryType),
                name: InteropUtf8NameFactory.TypeName(dictionaryType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(dictionaryType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(collectionType.Import(module).ToTypeDefOrRef())
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Get the getter and setter accessor methods for 'IDictionary<TKey, TValue>'
            MethodDefinition[] itemMethods = dictionaryMethodsType.GetMethods("Item"u8);

            // Create the 'get_Item' getter method
            MethodDefinition get_ItemMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.get_Item",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(valueType.Import(module), keyType.Import(module)));

            // Add and implement the 'get_Item' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2get_Item(keyType, valueType).Import(module),
                method: get_ItemMethod);

            // Create a body for the 'get_Item' method
            get_ItemMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: dictionaryType,
                implementationMethod: get_ItemMethod,
                forwardedMethod: itemMethods[0],
                interopReferences: interopReferences,
                module: module);

            // Create the 'set_Item' setter method
            MethodDefinition set_ItemMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.set_Item",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [keyType.Import(module), valueType.Import(module)]));

            // Add and implement the 'set_Item' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2set_Item(keyType, valueType).Import(module),
                method: set_ItemMethod);

            // Create a body for the 'set_Item' method
            set_ItemMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: dictionaryType,
                implementationMethod: set_ItemMethod,
                forwardedMethod: itemMethods[1],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Item' property
            PropertyDefinition itemProperty = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.Item",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_ItemMethod))
            {
                GetMethod = get_ItemMethod,
                SetMethod = set_ItemMethod
            };

            interfaceImplType.Properties.Add(itemProperty);

            // Create the 'get_Keys' getter method
            MethodDefinition get_KeysMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.get_Keys",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.ICollection1.MakeGenericReferenceType(keyType).Import(module)))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Newobj, interopReferences.DictionaryKeyCollection2_ctor(keyType, valueType).Import(module) },
                    { Ret }
                }
            };

            // Add and implement the 'get_Keys' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2get_Keys(keyType, valueType).Import(module),
                method: get_KeysMethod);

            // Create the 'Keys' property
            PropertyDefinition keysProperty = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.Keys",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_KeysMethod))
            { GetMethod = get_KeysMethod };

            interfaceImplType.Properties.Add(keysProperty);

            // Create the 'get_Values' getter method
            MethodDefinition get_ValuesMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.get_Values",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.ICollection1.MakeGenericReferenceType(valueType).Import(module)))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Newobj, interopReferences.DictionaryValueCollection2_ctor(keyType, valueType).Import(module) },
                    { Ret }
                }
            };

            // Add and implement the 'get_Values' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2get_Values(keyType, valueType).Import(module),
                method: get_ValuesMethod);

            // Create the 'Values' property
            PropertyDefinition valuesProperty = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.Values",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_ValuesMethod))
            { GetMethod = get_ValuesMethod };

            interfaceImplType.Properties.Add(valuesProperty);

            // Create the 'ContainsKey' method
            MethodDefinition containsKeyMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.ContainsKey",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyType.Import(module)]));

            // Add and implement the 'ContainsKey' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2ContainsKey(keyType, valueType).Import(module),
                method: containsKeyMethod);

            // Create a body for the 'ContainsKey' method
            containsKeyMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: dictionaryType,
                implementationMethod: containsKeyMethod,
                forwardedMethod: dictionaryMethodsType.GetMethod("ContainsKey"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'TryGetValue' method
            MethodDefinition tryGetValueMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.TryGetValue",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyType.Import(module), valueType.Import(module).MakeByReferenceType()]))
            { CilOutParameterIndices = [2] };

            // Add and implement the 'TryGetValue' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2TryGetValue(keyType, valueType).Import(module),
                method: tryGetValueMethod);

            // Create a body for the 'TryGetValue' method
            tryGetValueMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: dictionaryType,
                implementationMethod: tryGetValueMethod,
                forwardedMethod: dictionaryMethodsType.GetMethod("TryGetValue"u8),
                interopReferences: interopReferences,
                module: module);

            // There's two 'Add' and 'Remove' overloads, one from 'IDictionary<,>' and one from 'ICollection<>'.
            // They are always emitted in this relative order in the "Methods" type, so get them in advance here.
            MethodDefinition[] addMethods = dictionaryMethodsType.GetMethods("Add"u8);
            MethodDefinition[] removeMethods = dictionaryMethodsType.GetMethods("Remove"u8);

            // Create the 'Add' method
            MethodDefinition addMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.Add",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [keyType.Import(module), valueType.Import(module)]));

            // Add and implement the 'Add' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2Add(keyType, valueType).Import(module),
                method: addMethod);

            // Create a body for the 'Add' method
            addMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: dictionaryType,
                implementationMethod: addMethod,
                forwardedMethod: addMethods[0],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Remove' method
            MethodDefinition removeMethod = new(
                name: $"System.Collections.Generic.IDictionary<{keyType.FullName},{valueType.FullName}>.Remove",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyType.Import(module)]));

            // Add and implement the 'Remove' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDictionary2Remove(keyType, valueType).Import(module),
                method: removeMethod);

            // Create a body for the 'Remove' method
            removeMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: dictionaryType,
                implementationMethod: removeMethod,
                forwardedMethod: removeMethods[0],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Add' ('KeyValuePair<,>') method
            MethodDefinition addKeyValuePairMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Add",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [keyValuePairType.Import(module)]));

            // Add and implement the 'Add' ('KeyValuePair<,>') method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Add(keyValuePairType).Import(module),
                method: addKeyValuePairMethod);

            // Create a body for the 'Add' ('KeyValuePair<,>') method
            addKeyValuePairMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: collectionType,
                implementationMethod: addKeyValuePairMethod,
                forwardedMethod: addMethods[1],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Remove' ('KeyValuePair<,>') method
            MethodDefinition removeKeyValuePairMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Remove",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyValuePairType.Import(module)]));

            // Add and implement the 'Remove' ('KeyValuePair<,>') method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Remove(keyValuePairType).Import(module),
                method: removeKeyValuePairMethod);

            // Create a body for the 'Remove' ('KeyValuePair<,>') method
            removeKeyValuePairMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: dictionaryType,
                implementationMethod: removeKeyValuePairMethod,
                forwardedMethod: removeMethods[1],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Contains' method
            MethodDefinition containsMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Contains",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyValuePairType.Import(module)]));

            // Add and implement the 'Contains' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Contains(keyValuePairType).Import(module),
                method: containsMethod);

            // Create a body for the 'Contains' method
            containsMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: collectionType,
                implementationMethod: containsMethod,
                forwardedMethod: dictionaryMethodsType.GetMethod("Contains"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'CopyTo' method
            MethodDefinition copyToMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.CopyTo",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        keyValuePairType.MakeSzArrayType().Import(module),
                        module.CorLibTypeFactory.Int32]))
            {
                // Create a body for the 'CopyTo' method. This method is special: we also need to pass a 'WindowsRuntimeObjectReference'
                // for the 'IEnumerable<KeyValuePair<TKey, TValue>>' interface, as it needs to enumerate the key-value pairs. So here we
                // are emitting code manually, to save the current 'WindowsRuntimeObject', resolve the two references, and forward the call.
                CilLocalVariables = { new CilLocalVariable(interopReferences.WindowsRuntimeObject.Import(module).ToReferenceTypeSignature()) },
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                    { Stloc_0 },
                    { Ldloc_0 },
                    { Ldtoken, dictionaryType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Ldloc_0 },
                    { Ldtoken, enumerableType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, dictionaryMethodsType.GetMethod("CopyTo"u8) },
                    { Ret }
                }
            };

            // Add and implement the 'CopyTo' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1CopyTo(keyValuePairType).Import(module),
                method: copyToMethod);

            // Create the 'get_Count' getter method
            MethodDefinition get_CountMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.get_Count",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Int32));

            // Add and implement the 'get_Count' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1get_Count(keyValuePairType).Import(module),
                method: get_CountMethod);

            // Create a body for the 'get_Count' method
            get_CountMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: collectionType,
                implementationMethod: get_CountMethod,
                forwardedMethod: dictionaryMethodsType.GetMethod("Count"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Count' property
            PropertyDefinition countProperty = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Count",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CountMethod))
            { GetMethod = get_CountMethod };

            interfaceImplType.Properties.Add(countProperty);

            // Create the 'get_IsReadOnly' getter method
            MethodDefinition get_IsReadOnlyMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.get_IsReadOnly",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Boolean))
            {
                CilInstructions =
                {
                    { Ldc_I4_0 },
                    { Ret }
                }
            };

            // Add and implement the 'get_IsReadOnly' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1get_IsReadOnly(keyValuePairType).Import(module),
                method: get_IsReadOnlyMethod);

            // Create the 'IsReadOnly' property
            PropertyDefinition isReadOnlyProperty = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.IsReadOnly",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_IsReadOnlyMethod))
            { GetMethod = get_IsReadOnlyMethod };

            interfaceImplType.Properties.Add(isReadOnlyProperty);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="dictionaryType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature dictionaryType,
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
                ns: InteropUtf8NameFactory.TypeNamespace(dictionaryType),
                name: InteropUtf8NameFactory.TypeName(dictionaryType, "Impl"),
                vftblType: vftblType,
                get_IidMethod: get_IidMethod,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: []);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, dictionaryType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="dictionaryComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition dictionaryComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            string runtimeClassName = $"Windows.Foundation.Collections.IMap`2<{keyType},{valueType}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(dictionaryType),
                name: InteropUtf8NameFactory.TypeName(dictionaryType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: dictionaryComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IMap&lt;K, V&gt;</c> interface.
        /// </summary>
        /// <param name="dictionaryType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature keyType = dictionaryType.TypeArguments[0];
            TypeSignature valueType = dictionaryType.TypeArguments[1];

            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IMap`2<{keyType},{valueType}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: dictionaryType,
                proxyTypeMapSourceType: dictionaryType,
                proxyTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                interfaceTypeMapSourceType: dictionaryType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}

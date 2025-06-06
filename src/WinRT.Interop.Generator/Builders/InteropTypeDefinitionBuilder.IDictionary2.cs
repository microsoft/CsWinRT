// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
            hasKeyMethod.CilMethodBody = new CilMethodBody(hasKeyMethod)
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
            lookupMethod.CilMethodBody = new CilMethodBody(lookupMethod)
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
            insertMethod.CilMethodBody = new CilMethodBody(insertMethod)
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
            removeMethod.CilMethodBody = new CilMethodBody(removeMethod)
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
        /// <param name="module">The interop module being built.</param>
        /// <param name="dictionaryMethodsType">The resulting methods type.</param>
        public static void IDictionaryMethods(
            GenericInstanceTypeSignature dictionaryType,
            TypeDefinition mapMethodsType,
            InteropReferences interopReferences,
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
                        keyType.Import(module)]));

            dictionaryMethodsType.Methods.Add(get_ItemMethod);

            // Create a method body for the 'Item' getter method
            get_ItemMethod.CilMethodBody = new CilMethodBody(get_ItemMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2get_Item(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

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
                        valueType.Import(module)]));

            dictionaryMethodsType.Methods.Add(set_ItemMethod);

            // Create a method body for the 'Item' setter method
            set_ItemMethod.CilMethodBody = new CilMethodBody(set_ItemMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IDictionaryMethods2set_Item(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

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
                        valueType.Import(module)]));

            dictionaryMethodsType.Methods.Add(addMethod);

            // Create a method body for the 'Add' method
            addMethod.CilMethodBody = new CilMethodBody(addMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IDictionaryMethods2Add(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

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
                        keyType.Import(module)]));

            dictionaryMethodsType.Methods.Add(removeMethod);

            // Create a method body for the 'Remove' method
            removeMethod.CilMethodBody = new CilMethodBody(removeMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2Remove(keyType, valueType, mapMethodsType).Import(module) },
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

            dictionaryMethodsType.Methods.Add(countMethod);

            // Create a method body for the 'Count' method
            countMethod.CilMethodBody = new CilMethodBody(countMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IDictionaryMethodsCount.Import(module) },
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

            dictionaryMethodsType.Methods.Add(containsKeyMethod);

            // Create a method body for the 'ContainsKey' method
            containsKeyMethod.CilMethodBody = new CilMethodBody(containsKeyMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2ContainsKey(keyType, valueType, mapMethodsType).Import(module) },
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
            {
                // The 'value' parameter is '[out]'
                ParameterDefinitions = { new ParameterDefinition(sequence: 3, name: null, attributes: ParameterAttributes.Out) }
            };

            dictionaryMethodsType.Methods.Add(tryGetValueMethod);

            // Create a method body for the 'TryGetValue' method
            tryGetValueMethod.CilMethodBody = new CilMethodBody(tryGetValueMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IDictionaryMethods2TryGetValue(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

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
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module)]));

            dictionaryMethodsType.Methods.Add(addKeyValuePairMethod);

            // Create a method body for the 'Add' method
            addKeyValuePairMethod.CilMethodBody = new CilMethodBody(addKeyValuePairMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2AddKeyValuePair(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

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
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module)]));

            dictionaryMethodsType.Methods.Add(removeKeyValuePairMethod);

            // Create a method body for the 'Remove' method
            removeKeyValuePairMethod.CilMethodBody = new CilMethodBody(removeKeyValuePairMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2RemoveKeyValuePair(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Clear' method as follows:
            //
            // public static void Clear(WindowsRuntimeObjectReference thisReference)
            MethodDefinition clearMethod = new(
                name: "Clear"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]));

            dictionaryMethodsType.Methods.Add(clearMethod);

            // Create a method body for the 'Clear' method
            clearMethod.CilMethodBody = new CilMethodBody(clearMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IDictionaryMethodsClear.Import(module) },
                    { Ret }
                }
            };

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
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module)]));

            dictionaryMethodsType.Methods.Add(containsMethod);

            // Create a method body for the 'Contains' method
            containsMethod.CilMethodBody = new CilMethodBody(containsMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IDictionaryMethods2Contains(keyType, valueType, mapMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'CopyTo' method as follows:
            //
            // public static void CopyTo(WindowsRuntimeObjectReference thisReference, KeyValuePair<<KEY_TYPE>, <VALUE_TYPE>>[] array, int arrayIndex)
            MethodDefinition copyToMethod = new(
                name: "CopyTo"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        interopReferences.KeyValuePair.MakeGenericValueType(keyType, valueType).Import(module).MakeSzArrayType(),
                        module.CorLibTypeFactory.Int32]));

            dictionaryMethodsType.Methods.Add(copyToMethod);

            // Create a method body for the 'CopyTo' method
            copyToMethod.CilMethodBody = new CilMethodBody(copyToMethod)
            {
                Instructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };
        }
    }
}

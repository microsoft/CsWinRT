// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;

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
                TypeSignature sharedReadOnlyDictionaryType = interopReferences.IDictionary2.MakeGenericInstanceType(
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
    }
}

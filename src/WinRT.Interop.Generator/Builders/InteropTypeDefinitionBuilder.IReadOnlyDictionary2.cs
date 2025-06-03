// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
            TypeSignature sharedReadOnlyDictionaryType = interopReferences.IReadOnlyDictionary2.MakeGenericInstanceType(
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
    }
}

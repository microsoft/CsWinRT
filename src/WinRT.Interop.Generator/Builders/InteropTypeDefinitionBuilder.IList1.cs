// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.IList{T}"/> types.
    /// </summary>
    public static class IList1
    {
        /// <summary>
        /// Creates a new type definition for the vtable for an <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vftblType">The resulting vtable type.</param>
        public static void Vftbl(
            GenericInstanceTypeSignature listType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition vftblType)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // All reference types can share the same vtable type (as it just uses 'void*' for the ABI type)
            if (!elementType.IsValueType)
            {
                vftblType = interopDefinitions.IList1Vftbl;

                return;
            }

            // We can also share vtables for 'KeyValuePair<,>' types, as their ABI type is an interface
            if (elementType.IsKeyValuePairType(interopReferences))
            {
                vftblType = interopDefinitions.IList1Vftbl;

                return;
            }

            // Otherwise, we must construct a new specialized vtable type
            vftblType = WellKnownTypeDefinitionFactory.IList1Vftbl(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "Vftbl"),
                elementType: elementType, // TODO: use ABI type
                interopReferences: interopReferences,
                module: module);

            module.TopLevelTypes.Add(vftblType);
        }
    }
}

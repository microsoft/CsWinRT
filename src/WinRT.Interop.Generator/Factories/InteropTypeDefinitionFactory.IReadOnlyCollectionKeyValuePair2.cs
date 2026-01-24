// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop type definitions.
/// </summary>
internal partial class InteropTypeDefinitionFactory
{
    /// <summary>
    /// Helpers for support types for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class IReadOnlyCollectionKeyValuePair2
    {
        /// <summary>
        /// Creates a new type definition for the forwarder attribute for a <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="readOnlyCollectionType">The <see cref="GenericInstanceTypeSignature"/> for the generic interface type.</param>
        /// <param name="readOnlyDictionaryType">The <see cref="TypeSignature"/> for the corresponding <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.</param>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the corresponding <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="forwarderAttributeType">The resulting marshaller type.</param>
        /// <remarks>
        /// This method can also be used to define the forwarder attribute for <see cref="System.Collections.Generic.ICollection{T}"/> interfaces.
        /// </remarks>
        public static void ForwarderAttribute(
            GenericInstanceTypeSignature readOnlyCollectionType,
            TypeSignature readOnlyDictionaryType,
            TypeSignature readOnlyListType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition forwarderAttributeType)
        {
            // We're declaring an 'internal sealed class' type
            forwarderAttributeType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyCollectionType),
                name: InteropUtf8NameFactory.TypeName(readOnlyCollectionType, "ForwarderAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.DynamicInterfaceCastableForwarderAttribute.Import(module));

            module.TopLevelTypes.Add(forwarderAttributeType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateDefaultConstructor(module, interopReferences.DynamicInterfaceCastableForwarderAttribute_ctor);

            forwarderAttributeType.Methods.Add(ctor);

            // Prepare the jump labels
            CilInstruction ldc_i4_1_afterChecks = new(Ldc_I4_1);

            // Define the 'IsInterfaceImplemented' method as follows:
            //
            // public override bool IsInterfaceImplemented(WindowsRuntimeobject thisObject, out WindowsRuntimeObjectReference interfaceReference)
            MethodDefinition isInterfaceImplementedMethod = new(
                name: "IsInterfaceImplemented"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.Import(module).ToReferenceTypeSignature(),
                        interopReferences.WindowsRuntimeObjectReference.Import(module).MakeByReferenceType()]))
            {
                CilOutParameterIndices = [2],
                CilInstructions =
                {
                    // if (thisObject.TryGetObjectReferenceForInterface(typeof(<READ_ONLY_DICTIONARY_TYPE>), out interfaceReference)) return true;
                    { Ldarg_1 },
                    { Ldtoken, readOnlyDictionaryType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Ldarg_2 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectTryGetObjectReferenceForInterface.Import(module) },
                    { Brtrue_S, ldc_i4_1_afterChecks.CreateLabel() },

                    // return thisObject.TryGetObjectReferenceForInterface(typeof(<READ_ONLY_LIST_TYPE>), out interfaceReference));
                    { Ldarg_1 },
                    { Ldtoken, readOnlyListType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Ldarg_2 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectTryGetObjectReferenceForInterface.Import(module) },
                    { Ret },
                    { ldc_i4_1_afterChecks },
                    { Ret }
                }
            };

            // Add and implement the 'IsInterfaceImplemented' method
            forwarderAttributeType.Methods.Add(isInterfaceImplementedMethod);
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.ICollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class ICollectionKeyValuePair
    {
        /// <summary>
        /// Creates a new type definition for the forwarder attribute for a <see cref="System.Collections.Generic.ICollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="collectionType">The <see cref="GenericInstanceTypeSignature"/> for the generic interface type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="forwarderAttributeType">The resulting marshaller type.</param>
        public static void ForwarderAttribute(
            GenericInstanceTypeSignature collectionType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition forwarderAttributeType)
        {
            GenericInstanceTypeSignature keyValuePairType = (GenericInstanceTypeSignature)collectionType.TypeArguments[0];
            TypeSignature keyType = keyValuePairType.TypeArguments[0];
            TypeSignature valueType = keyValuePairType.TypeArguments[1];

            // We're declaring an 'internal sealed class' type
            forwarderAttributeType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(collectionType),
                name: InteropUtf8NameFactory.TypeName(collectionType, "ForwarderAttribute"),
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
                    // if (thisObject.TryGetObjectReferenceForInterface(typeof(IDictionary<TKey, TValue>), out interfaceReference)) return true;
                    { Ldarg_1 },
                    { Ldtoken, interopReferences.IDictionary2.MakeGenericReferenceType(keyType, valueType).Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Ldarg_2 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectTryGetObjectReferenceForInterface.Import(module) },
                    { Brtrue_S, ldc_i4_1_afterChecks.CreateLabel() },

                    // return thisObject.TryGetObjectReferenceForInterface(typeof(IList<KeyValuePair<TKey, TValue>>), out interfaceReference));
                    { Ldarg_1 },
                    { Ldtoken, interopReferences.IList1.MakeGenericReferenceType(interopReferences.KeyValuePair2.MakeGenericValueType(keyType, valueType)).Import(module).ToTypeDefOrRef() },
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
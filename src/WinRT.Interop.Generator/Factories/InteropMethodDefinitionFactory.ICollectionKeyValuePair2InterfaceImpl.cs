// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropMethodDefinitionFactory"/>
internal partial class InteropMethodDefinitionFactory
{
    /// <summary>
    /// Helpers for interface impl types for <see cref="System.Collections.Generic.ICollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class ICollectionKeyValuePair2InterfaceImpl
    {
        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> for the <c>add_VectorChanged</c> export method.
        /// </summary>
        /// <param name="collectionType">The <see cref="TypeSignature"/> for the vector type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        public static MethodDefinition CopyTo(
            GenericInstanceTypeSignature collectionType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            GenericInstanceTypeSignature keyValuePairType = (GenericInstanceTypeSignature)collectionType.TypeArguments[0];
            TypeSignature keyType = keyValuePairType.TypeArguments[0];
            TypeSignature valueType = keyValuePairType.TypeArguments[1];
            TypeSignature dictionaryType = interopReferences.IDictionary2.MakeGenericReferenceType([keyType, valueType]);
            TypeSignature listType = interopReferences.IList1.MakeGenericReferenceType(keyValuePairType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);

            // Define the 'CopyTo' method as follows:
            //
            // void ICollection<<KEY_VALUE_PAIR_TYPE>>.CopyTo(<KEY_VALUE_PAIR_TYPE>[] array, int arrayIndex)
            MethodDefinition copyToMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.CopyTo",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [
                        keyValuePairType.MakeSzArrayType(),
                        interopReferences.Int32]));

            // Jump labels
            CilInstruction ldloc_0_type2Check = new(Ldloc_0);

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObject' (for 'thisObject')
            //   [1]: 'WindowsRuntimeObjectReference' (for 'interfaceReference')
            CilLocalVariable loc_0_thisObject = new(interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature());
            CilLocalVariable loc_1_interfaceReference = new(interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature());

            // Create a body for the 'CopyTo' method. This method is special: we also need to pass a 'WindowsRuntimeObjectReference'
            // for the 'IEnumerable<KeyValuePair<TKey, TValue>>' interface, as it needs to enumerate the key-value pairs. So here we
            // are emitting code manually, to save the current 'WindowsRuntimeObject', resolve the two references, and forward the call.
            copyToMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisObject, loc_1_interfaceReference },
                Instructions =
                {
                    // WindowsRuntimeObject thisObject = (WindowsRuntimeObject)this;
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject },
                    { Stloc_0 },

                    // if (thisObject.TryGetObjectReferenceForInterface(typeof(<DICTIONARY_TYPE>), out interfaceReference))
                    { Ldloc_0 },
                    { Ldtoken, dictionaryType.ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle },
                    { Callvirt, interopReferences.Typeget_TypeHandle },
                    { Ldloca_S, loc_1_interfaceReference },
                    { Callvirt, interopReferences.WindowsRuntimeObjectTryGetObjectReferenceForInterface },
                    { Brfalse_S, ldloc_0_type2Check.CreateLabel() },

                    // <DICTIONARY_METHODS_TYPE>.CopyTo(interfaceReference, <ARGS>);
                    { Ldloc_1 },
                    { Ldloc_0 },
                    { Ldtoken, enumerableType.ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle },
                    { Callvirt, interopReferences.Typeget_TypeHandle },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, emitState.LookupTypeDefinition(dictionaryType, "Methods").GetMethod("CopyTo"u8) },
                    { Ret },

                    // interfaceReference = thisObject.GetObjectReferenceForInterface(typeof(<INTERFACE_TYPE2>));
                    { ldloc_0_type2Check },
                    { Ldtoken, listType.ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle },
                    { Callvirt, interopReferences.Typeget_TypeHandle },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface },
                    { Stloc_1 },

                    // <LIST_METHODS_TYPE>.CopyTo(interfaceReference, <ARGS>);
                    { Ldloc_1 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, emitState.LookupTypeDefinition(listType, "Methods").GetMethod("CopyTo"u8) },
                    { Ret }
                }
            };

            return copyToMethod;
        }
    }
}
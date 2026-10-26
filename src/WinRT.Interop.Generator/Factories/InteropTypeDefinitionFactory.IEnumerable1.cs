// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <inheritdoc cref="InteropTypeDefinitionFactory"/>
internal partial class InteropTypeDefinitionFactory
{
    /// <summary>
    /// Helpers for support types for <see cref="System.Collections.Generic.IEnumerable{T}"/>.
    /// </summary>
    public static class IEnumerable1
    {
        /// <summary>
        /// Creates a sealed non-generic attribute implementing typed enumeration for one element type.
        /// </summary>
        /// <param name="enumerableType">The constructed generic enumerable interface.</param>
        /// <param name="interopDefinitions">The output definitions.</param>
        /// <param name="interopReferences">The output references.</param>
        /// <param name="module">The output module.</param>
        /// <param name="attributeType">The resulting attribute type.</param>
        public static void EnumerableAttribute(
            GenericInstanceTypeSignature enumerableType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition attributeType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal sealed class' specialized for the closed element type
            attributeType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType, interopReferences.RuntimeContext),
                name: InteropUtf8NameFactory.TypeName(enumerableType, interopDefinitions, "EnumerableAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.DynamicInterfaceCastableEnumerableAttribute);

            module.TopLevelTypes.Add(attributeType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateDefaultConstructor(
                corLibTypeFactory: interopReferences.CorLibTypeFactory,
                constructorMethod: interopReferences.DynamicInterfaceCastableEnumerableAttribute_ctor);

            attributeType.Methods.Add(ctor);

            // Prepare the typed call for the successful interface cast
            CilInstruction callvirt_getEnumerator = new(Callvirt, interopReferences.IEnumerable1GetEnumerator(elementType));

            // Define the 'TryGetEnumerator' method as follows:
            //
            // public override bool TryGetEnumerator(WindowsRuntimeObject thisObject, out IEnumerator enumerator)
            MethodDefinition tryGetEnumeratorMethod = new(
                name: "TryGetEnumerator"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                        interopReferences.IEnumerator.MakeReferenceTypeByReferenceType()]))
            {
                CilOutParameterIndices = [2],
                CilInstructions =
                {
                    // A failed interface cast stores 'null' and returns 'false'
                    { Ldarg_2 },
                    { Ldarg_1 },
                    { Isinst, enumerableType.ToTypeDefOrRef() },
                    { Dup },
                    { Brtrue_S, callvirt_getEnumerator.CreateLabel() },
                    { Stind_Ref },
                    { Ldc_I4_0 },
                    { Ret },

                    // Return 'GetEnumerator()' directly from the cast instance
                    { callvirt_getEnumerator },
                    { Stind_Ref },
                    { Ldc_I4_1 },
                    { Ret }
                }
            };

            attributeType.Methods.Add(tryGetEnumeratorMethod);
        }
    }
}

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

            // Prepare the typed call shared by the null-conditional branch
            CilInstruction callvirt_getEnumerator = new(Callvirt, interopReferences.IEnumerable1GetEnumerator(elementType));

            // Define the 'TryGetEnumeratorCore' method as follows:
            //
            // protected override IEnumerator? TryGetEnumeratorCore(WindowsRuntimeObject thisObject)
            MethodDefinition tryGetEnumeratorCoreMethod = new(
                name: "TryGetEnumeratorCore"u8,
                attributes: MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.IEnumerator.ToReferenceTypeSignature(),
                    parameterTypes: [interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    // return (thisObject as IEnumerable<T>)?.GetEnumerator();
                    { Ldarg_1 },
                    { Isinst, enumerableType.ToTypeDefOrRef() },
                    { Dup },
                    { Brtrue_S, callvirt_getEnumerator.CreateLabel() },
                    { Pop },
                    { Ldnull },
                    { Ret },
                    { callvirt_getEnumerator },
                    { Ret }
                }
            };

            attributeType.Methods.Add(tryGetEnumeratorCoreMethod);
        }
    }
}

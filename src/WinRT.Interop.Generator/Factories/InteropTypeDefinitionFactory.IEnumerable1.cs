// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
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

            // Define the 'IsInterfaceImplemented' method as follows:
            //
            // public override bool IsInterfaceImplemented(WindowsRuntimeObject thisObject)
            MethodDefinition isInterfaceImplementedMethod = new(
                name: "IsInterfaceImplemented"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Boolean,
                    parameterTypes: [interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    // return thisObject is IEnumerable<T>;
                    { Ldarg_1 },
                    { Isinst, enumerableType.ToTypeDefOrRef() },
                    { Ldnull },
                    { Cgt_Un },
                    { Ret }
                }
            };

            attributeType.Methods.Add(isInterfaceImplementedMethod);

            // Define the 'GetEnumeratorCore' method as follows:
            //
            // protected override IEnumerator GetEnumeratorCore(WindowsRuntimeObject thisObject)
            MethodDefinition getEnumeratorCoreMethod = new(
                name: "GetEnumeratorCore"u8,
                attributes: MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.IEnumerator.ToReferenceTypeSignature(),
                    parameterTypes: [interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    // return ((IEnumerable<T>)thisObject).GetEnumerator();
                    { Ldarg_1 },
                    { Castclass, enumerableType.ToTypeDefOrRef() },
                    { Callvirt, interopReferences.IEnumerable1GetEnumerator(elementType) },
                    { Ret }
                }
            };

            attributeType.Methods.Add(getEnumeratorCoreMethod);
        }
    }
}

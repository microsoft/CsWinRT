// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop type definitions.
/// </summary>
internal partial class InteropTypeDefinitionFactory
{
    /// <summary>
    /// Helpers for element marshaller types for <see cref="System.Collections.Generic.IEnumerator{T}"/> types.
    /// </summary>
    /// <remarks>
    /// The resulting element marshaller types can also be reused for other generic collection types.
    /// </remarks>
    public static class IEnumeratorElementMarshaller
    {
        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for an unmanaged value type.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition UnmanagedValueType(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Get the constructed 'IWindowsRuntimeUnmanagedValueTypeElementMarshaller<T, TAbi>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeUnmanagedValueTypeElementMarshaller2
                .MakeGenericReferenceType(elementType, elementAbiType);

            return ElementMarshaller(
                elementType: elementType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeUnmanagedValueTypeElementMarshallerConvertToUnmanaged(elementType, elementAbiType),
                isValueType: true,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a managed value type.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ManagedValueType(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Get the constructed 'IWindowsRuntimeManagedValueTypeElementMarshaller<T, TAbi>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeManagedValueTypeElementMarshaller2
                .MakeGenericReferenceType(elementType, elementAbiType);

            // Get the element marshaller type with the common method implementations
            TypeDefinition elementMarshallerType = ElementMarshaller(
                elementType: elementType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeManagedValueTypeElementMarshallerConvertToUnmanaged(elementType, elementAbiType),
                isValueType: true,
                interopReferences: interopReferences,
                emitState: emitState);

            // Rewriting labels
            CilInstruction nop_dispose = new(Nop);

            // Define the 'Dispose' method as follows:
            //
            // public static void Dispose(<ABI_ELEMENT_TYPE> value)
            MethodDefinition disposeMethod = new(
                name: "Dispose"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(interopReferences.Void, [elementAbiType]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { nop_dispose },
                    { Ret }
                }
            };

            // Add and implement the 'Dispose' method
            elementMarshallerType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeManagedValueTypeElementMarshallerDispose(elementType, elementAbiType),
                method: disposeMethod);

            // Track rewriting the disposal for 'Dispose'
            emitState.TrackDisposeRewrite(
                parameterType: elementType,
                method: disposeMethod,
                marker: nop_dispose);

            return elementMarshallerType;
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition KeyValuePair(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            GenericInstanceTypeSignature elementType = (GenericInstanceTypeSignature)enumeratorType.TypeArguments[0];
            TypeSignature keyType = elementType.TypeArguments[0];
            TypeSignature valueType = elementType.TypeArguments[1];

            // Get the constructed 'IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeKeyValuePairTypeElementMarshaller2
                .MakeGenericReferenceType(keyType, valueType);

            // Specialize if both type arguments are value types (same logic as in the array element marshaller)
            bool isValueType = keyType.IsValueType && valueType.IsValueType;

            return ElementMarshaller(
                elementType: elementType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeKeyValuePairTypeElementMarshallerConvertToUnmanaged(keyType, valueType),
                isValueType: isValueType,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a <see cref="System.Nullable{T}"/> type.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition NullableValueType(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            GenericInstanceTypeSignature elementType = (GenericInstanceTypeSignature)enumeratorType.TypeArguments[0];
            TypeSignature underlyingType = elementType.TypeArguments[0];

            // Get the constructed 'IWindowsRuntimeNullableTypeElementMarshaller<T>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeNullableTypeElementMarshaller1
                .MakeGenericReferenceType(underlyingType);

            return ElementMarshaller(
                elementType: elementType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeNullableTypeElementMarshallerConvertToUnmanaged(underlyingType),
                isValueType: true,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a reference type.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ReferenceType(
            GenericInstanceTypeSignature enumeratorType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // Get the constructed 'IWindowsRuntimeReferenceTypeElementMarshaller<T>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeReferenceTypeElementMarshaller1
                .MakeGenericReferenceType(elementType);

            return ElementMarshaller(
                elementType: elementType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeReferenceTypeElementMarshallerConvertToUnmanaged(elementType),
                isValueType: false,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for some element type.
        /// </summary>
        /// <param name="elementType">The <see cref="TypeSignature"/> for the element type.</param>
        /// <param name="interfaceType">The interface type the element marshaller type should implement.</param>
        /// <param name="convertToUnmanagedInterfaceMethod">The <c>ConvertToUnmanaged</c> interface method being implemented.</param>
        /// <param name="isValueType">Indicates whether the element marshaller type should be emitted as a value type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ElementMarshaller(
            TypeSignature elementType,
            TypeSignature interfaceType,
            MemberReference convertToUnmanagedInterfaceMethod,
            bool isValueType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            // Select the attributes and base type depending on whether we want a value type or not
            (TypeAttributes attributes, ITypeDefOrRef baseType) = isValueType
                ? (TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, interopReferences.ValueType)
                : (TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit, interopReferences.Object.ToTypeDefOrRef());

            // We're declaring an 'internal abstract class' type
            TypeDefinition elementMarshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(elementType),
                name: InteropUtf8NameFactory.TypeName(elementType, "ElementMarshaller"),
                attributes: attributes,
                baseType: baseType)
            {
                Interfaces = { new InterfaceImplementation(interfaceType.ToTypeDefOrRef()) }
            };

            // Rewriting labels
            CilInstruction nop_convertToUnmanaged = new(Nop);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static <RAW_ABI_ELEMENT_TYPE> ConvertToUnmanaged(<ELEMENT_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType.GetRawAbiType(interopReferences),
                    parameterTypes: [elementType]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { nop_convertToUnmanaged },
                    { Ret }
                }
            };

            // Add and implement the 'ConvertToUnmanaged' method
            elementMarshallerType.AddMethodImplementation(
                declaration: convertToUnmanagedInterfaceMethod,
                method: convertToUnmanagedMethod);

            // Track rewriting the native value for 'ConvertToUnmanaged'
            emitState.TrackRawRetValMethodRewrite(
                parameterType: elementType,
                method: convertToUnmanagedMethod,
                marker: nop_convertToUnmanaged);

            return elementMarshallerType;
        }
    }
}
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
    /// Helpers for element marshaller types for SZ array types.
    /// </summary>
    public static class SzArrayElementMarshaller
    {
        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for an unmanaged value type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition UnmanagedValueType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Get the constructed 'IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller2
                .MakeGenericReferenceType([elementType, elementAbiType]);

            return ElementMarshaller(
                arrayType: arrayType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeUnmanagedValueTypeArrayElementMarshallerConvertToUnmanaged(elementType, elementAbiType),
                convertToManagedInterfaceMethod: interopReferences.IWindowsRuntimeUnmanagedValueTypeArrayElementMarshallerConvertToManaged(elementType, elementAbiType),
                isValueType: true,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a managed value type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ManagedValueType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Get the constructed 'IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeManagedValueTypeArrayElementMarshaller2
                .MakeGenericReferenceType([elementType, elementAbiType]);

            // Get the element marshaller type with the common method implementations
            TypeDefinition elementMarshallerType = ElementMarshaller(
                arrayType: arrayType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeManagedValueTypeArrayElementMarshallerConvertToUnmanaged(elementType, elementAbiType),
                convertToManagedInterfaceMethod: interopReferences.IWindowsRuntimeManagedValueTypeArrayElementMarshallerConvertToManaged(elementType, elementAbiType),
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
                declaration: interopReferences.IWindowsRuntimeManagedValueTypeArrayElementMarshallerDispose(elementType, elementAbiType),
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
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition KeyValuePair(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            GenericInstanceTypeSignature elementType = (GenericInstanceTypeSignature)arrayType.BaseType;
            TypeSignature keyType = elementType.TypeArguments[0];
            TypeSignature valueType = elementType.TypeArguments[1];

            // Get the constructed 'IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller2
                .MakeGenericReferenceType([keyType, valueType]);

            // If both the key and the value types are value types, it means the whole marshaller will be specialized.
            // In that case we can emit the element marshaller type as a value type as well, so the whole thing can be
            // fully specialized and inlined. We don't do this if either type is a reference type, because that means
            // the generic instantiation could still be shared between different types, so we prefer to save size there.
            bool isValueType = keyType.IsValueType && valueType.IsValueType;

            return ElementMarshaller(
                arrayType: arrayType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeKeyValuePairTypeArrayElementMarshallerConvertToUnmanaged(keyType, valueType),
                convertToManagedInterfaceMethod: interopReferences.IWindowsRuntimeKeyValuePairTypeArrayElementMarshallerConvertToManaged(keyType, valueType),
                isValueType: isValueType,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a <see cref="System.Nullable{T}"/> type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition NullableValueType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            GenericInstanceTypeSignature elementType = (GenericInstanceTypeSignature)arrayType.BaseType;
            TypeSignature underlyingType = elementType.TypeArguments[0];

            // Get the constructed 'IWindowsRuntimeNullableTypeArrayElementMarshaller<T>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeNullableTypeArrayElementMarshaller1
                .MakeGenericReferenceType([underlyingType]);

            return ElementMarshaller(
                arrayType: arrayType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeNullableTypeArrayElementMarshallerConvertToUnmanaged(underlyingType),
                convertToManagedInterfaceMethod: interopReferences.IWindowsRuntimeNullableTypeArrayElementMarshallerConvertToManaged(underlyingType),
                isValueType: true,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a reference type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ReferenceType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = arrayType.BaseType;

            // Get the constructed 'IWindowsRuntimeReferenceTypeArrayElementMarshaller<T>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeReferenceTypeArrayElementMarshaller1
                .MakeGenericReferenceType([elementType]);

            return ElementMarshaller(
                arrayType: arrayType,
                interfaceType: interfaceType,
                convertToUnmanagedInterfaceMethod: interopReferences.IWindowsRuntimeReferenceTypeArrayElementMarshallerConvertToUnmanaged(elementType),
                convertToManagedInterfaceMethod: interopReferences.IWindowsRuntimeReferenceTypeArrayElementMarshallerConvertToManaged(elementType),
                isValueType: false,
                interopReferences: interopReferences,
                emitState: emitState);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for some element type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interfaceType">The interface type the element marshaller type should implement.</param>
        /// <param name="convertToUnmanagedInterfaceMethod">The <c>ConvertToUnmanaged</c> interface method being implemented.</param>
        /// <param name="convertToManagedInterfaceMethod">The <c>ConvertToManaged</c> interface method being implemented.</param>
        /// <param name="isValueType">Indicates whether the element marshaller type should be emitted as a value type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ElementMarshaller(
            SzArrayTypeSignature arrayType,
            TypeSignature interfaceType,
            MemberReference convertToUnmanagedInterfaceMethod,
            MemberReference convertToManagedInterfaceMethod,
            bool isValueType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState)
        {
            TypeSignature elementType = arrayType.BaseType;

            // Select the attributes and base type depending on whether we want a value type or not
            (TypeAttributes attributes, ITypeDefOrRef baseType) = isValueType
                ? (TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, interopReferences.ValueType)
                : (TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit, interopReferences.Object.ToTypeDefOrRef());

            // We're declaring an 'internal abstract class' type
            TypeDefinition elementMarshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType, interopReferences.RuntimeContext),
                name: InteropUtf8NameFactory.TypeName(arrayType, interopReferences.RuntimeContext, "ElementMarshaller"),
                attributes: attributes,
                baseType: baseType)
            {
                Interfaces = { new InterfaceImplementation(interfaceType.ToTypeDefOrRef()) }
            };

            // Rewriting labels
            CilInstruction nop_convertToUnmanaged = new(Nop);
            CilInstruction nop_convertToManaged = new(Nop);

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

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <ELEMENT_TYPE> ConvertToManaged(<ABI_ELEMENT_TYPE> value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType,
                    parameterTypes: [elementType.GetAbiType(interopReferences)]))
            {
                CilInstructions =
                {
                    { nop_convertToManaged },
                    { Ret }
                }
            };

            // Add and implement the 'ConvertToManaged' method
            elementMarshallerType.AddMethodImplementation(
                declaration: convertToManagedInterfaceMethod,
                method: convertToManagedMethod);

            // Track rewriting the managed value for 'ConvertToManaged'
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: elementType,
                method: convertToManagedMethod,
                marker: nop_convertToManaged,
                parameterIndex: 0);

            return elementMarshallerType;
        }
    }
}
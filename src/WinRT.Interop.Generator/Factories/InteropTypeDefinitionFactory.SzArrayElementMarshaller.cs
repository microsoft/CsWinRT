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
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition UnmanagedValueType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Get the constructed 'IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller2
                .MakeGenericReferenceType(elementType, elementAbiType);

            // We're declaring an 'internal abstract class' type
            TypeDefinition elementMarshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ElementMarshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interfaceType.Import(module).ToTypeDefOrRef()) }
            };

            // Rewriting labels
            CilInstruction nop_convertToUnmanaged = new(Nop);
            CilInstruction nop_convertToManaged = new(Nop);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static <ABI_ELEMENT_TYPE> ConvertToUnmanaged(<ELEMENT_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(elementAbiType.Import(module), elementType.Import(module)))
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
                declaration: interopReferences.IWindowsRuntimeUnmanagedValueTypeArrayElementMarshallerConvertToUnmanaged(elementType, elementAbiType).Import(module),
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
                signature: MethodSignature.CreateStatic(elementType.Import(module), elementAbiType.Import(module)))
            {
                CilInstructions =
                {
                    { nop_convertToManaged },
                    { Ret }
                }
            };

            // Add and implement the 'ConvertToManaged' method
            elementMarshallerType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeUnmanagedValueTypeArrayElementMarshallerConvertToManaged(elementType, elementAbiType).Import(module),
                method: convertToManagedMethod);

            // Track rewriting the managed value for 'ConvertToManaged'
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: elementType,
                method: convertToManagedMethod,
                marker: nop_convertToManaged,
                parameterIndex: 0);

            return elementMarshallerType;
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a managed value type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ManagedValueType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // Get the constructed 'IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeManagedValueTypeArrayElementMarshaller2
                .MakeGenericReferenceType(elementType, elementAbiType);

            // We're declaring an 'internal abstract class' type
            TypeDefinition elementMarshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ElementMarshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interfaceType.Import(module).ToTypeDefOrRef()) }
            };

            // Rewriting labels
            CilInstruction nop_convertToUnmanaged = new(Nop);
            CilInstruction nop_convertToManaged = new(Nop);
            CilInstruction nop_dispose = new(Nop);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static <ABI_ELEMENT_TYPE> ConvertToUnmanaged(<ELEMENT_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(elementAbiType.Import(module), elementType.Import(module)))
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
                declaration: interopReferences.IWindowsRuntimeManagedValueTypeArrayElementMarshallerConvertToUnmanaged(elementType, elementAbiType).Import(module),
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
                signature: MethodSignature.CreateStatic(elementType.Import(module), elementAbiType.Import(module)))
            {
                CilInstructions =
                {
                    { nop_convertToManaged },
                    { Ret }
                }
            };

            // Add and implement the 'ConvertToManaged' method
            elementMarshallerType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeManagedValueTypeArrayElementMarshallerConvertToManaged(elementType, elementAbiType).Import(module),
                method: convertToManagedMethod);

            // Track rewriting the managed value for 'ConvertToManaged'
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: elementType,
                method: convertToManagedMethod,
                marker: nop_convertToManaged,
                parameterIndex: 0);

            // Define the 'Dispose' method as follows:
            //
            // public static void Dispose(<ABI_ELEMENT_TYPE> value)
            MethodDefinition disposeMethod = new(
                name: "Dispose"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(module.CorLibTypeFactory.Void, elementAbiType.Import(module)))
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
                declaration: interopReferences.IWindowsRuntimeManagedValueTypeArrayElementMarshallerDispose(elementType, elementAbiType).Import(module),
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
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition KeyValuePair(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            GenericInstanceTypeSignature elementType = (GenericInstanceTypeSignature)arrayType.BaseType;
            TypeSignature keyType = elementType.TypeArguments[0];
            TypeSignature valueType = elementType.TypeArguments[1];

            // Get the constructed 'IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller2
                .MakeGenericReferenceType(keyType, valueType);

            // We're declaring an 'internal abstract class' type
            TypeDefinition elementMarshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ElementMarshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interfaceType.Import(module).ToTypeDefOrRef()) }
            };

            TypeSignature keyValuePairType = interopReferences.KeyValuePair2.MakeGenericValueType(keyType, valueType);

            // Rewriting labels
            CilInstruction nop_convertToUnmanaged = new(Nop);
            CilInstruction nop_convertToManaged = new(Nop);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(KeyValuePair<<KEY_TYPE>, <VALUE_TYPE>> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToValueTypeSignature(),
                    parameterTypes: [keyValuePairType.Import(module)]))
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
                declaration: interopReferences.IWindowsRuntimeKeyValuePairTypeArrayElementMarshallerConvertToUnmanaged(keyType, valueType).Import(module),
                method: convertToUnmanagedMethod);

            // Track rewriting the native value for 'ConvertToUnmanaged'
            emitState.TrackRawRetValMethodRewrite(
                parameterType: elementType,
                method: convertToUnmanagedMethod,
                marker: nop_convertToUnmanaged);

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static KeyValuePair<<KEY_TYPE>, <VALUE_TYPE>> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: keyValuePairType.Import(module),
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CilInstructions =
                {
                    { nop_convertToManaged },
                    { Ret }
                }
            };

            // Add and implement the 'ConvertToManaged' method
            elementMarshallerType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeKeyValuePairTypeArrayElementMarshallerConvertToManaged(keyType, valueType).Import(module),
                method: convertToManagedMethod);

            // Track rewriting the managed value for 'ConvertToManaged'
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: elementType,
                method: convertToManagedMethod,
                marker: nop_convertToManaged,
                parameterIndex: 0);

            return elementMarshallerType;
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the element marshaller for a reference type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting element marshaller type.</returns>
        public static TypeDefinition ReferenceType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;

            // Get the constructed 'IWindowsRuntimeReferenceTypeArrayElementMarshaller<T>' interface type
            TypeSignature interfaceType = interopReferences
                .IWindowsRuntimeReferenceTypeArrayElementMarshaller1
                .MakeGenericReferenceType(elementType);

            // We're declaring an 'internal abstract class' type
            TypeDefinition elementMarshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ElementMarshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interfaceType.Import(module).ToTypeDefOrRef()) }
            };

            // Rewriting labels
            CilInstruction nop_convertToUnmanaged = new(Nop);
            CilInstruction nop_convertToManaged = new(Nop);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<ELEMENT_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToValueTypeSignature(),
                    parameterTypes: [elementType.Import(module)]))
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
                declaration: interopReferences.IWindowsRuntimeReferenceTypeArrayElementMarshallerConvertToUnmanaged(elementType).Import(module),
                method: convertToUnmanagedMethod);

            // Track rewriting the native value for 'ConvertToUnmanaged'
            emitState.TrackRawRetValMethodRewrite(
                parameterType: elementType,
                method: convertToUnmanagedMethod,
                marker: nop_convertToUnmanaged);

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <ELEMENT_TYPE> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType.Import(module),
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CilInstructions =
                {
                    { nop_convertToManaged },
                    { Ret }
                }
            };

            // Add and implement the 'ConvertToManaged' method
            elementMarshallerType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeReferenceTypeArrayElementMarshallerConvertToManaged(elementType).Import(module),
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
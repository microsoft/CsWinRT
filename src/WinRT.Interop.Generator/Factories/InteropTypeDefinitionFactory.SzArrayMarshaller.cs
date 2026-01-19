// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
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
    /// Helpers for marshaller types for SZ array types.
    /// </summary>
    public static class SzArrayMarshaller
    {
        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for a blittable value type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition BlittableValueType(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;

            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.WindowsRuntimeBlittableValueTypeArrayMarshallerConvertToUnmanaged(elementType),
                convertToManagedMethod: interopReferences.WindowsRuntimeBlittableValueTypeArrayMarshallerConvertToManaged(elementType),
                copyToUnmanagedMethod: interopReferences.WindowsRuntimeBlittableValueTypeArrayMarshallerCopyToUnmanaged(elementType),
                copyToManagedMethod: interopReferences.WindowsRuntimeBlittableValueTypeArrayMarshallerCopyToManaged(elementType),
                freeMethod: interopReferences.WindowsRuntimeBlittableValueTypeArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for an unmanaged value type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="elementMarshallerType">The element marshaller type produced by <see cref="SzArrayElementMarshaller.UnmanagedValueType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition UnmanagedValueType(
            SzArrayTypeSignature arrayType,
            TypeDefinition elementMarshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);
            TypeSignature elementMarshallerTypeSignature = elementMarshallerType.ToTypeSignature();

            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.WindowsRuntimeUnmanagedValueTypeArrayMarshallerConvertToUnmanaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                convertToManagedMethod: interopReferences.WindowsRuntimeUnmanagedValueTypeArrayMarshallerConvertToManaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                copyToUnmanagedMethod: interopReferences.WindowsRuntimeUnmanagedValueTypeArrayMarshallerCopyToUnmanaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                copyToManagedMethod: interopReferences.WindowsRuntimeUnmanagedValueTypeArrayMarshallerCopyToManaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                freeMethod: interopReferences.WindowsRuntimeBlittableValueTypeArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for a managed value type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="elementMarshallerType">The element marshaller type produced by <see cref="SzArrayElementMarshaller.ManagedValueType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition ManagedValueType(
            SzArrayTypeSignature arrayType,
            TypeDefinition elementMarshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);
            TypeSignature elementMarshallerTypeSignature = elementMarshallerType.ToTypeSignature();

            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.WindowsRuntimeManagedValueTypeArrayMarshallerConvertToUnmanaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                convertToManagedMethod: interopReferences.WindowsRuntimeManagedValueTypeArrayMarshallerConvertToManaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                copyToUnmanagedMethod: interopReferences.WindowsRuntimeManagedValueTypeArrayMarshallerCopyToUnmanaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                copyToManagedMethod: interopReferences.WindowsRuntimeManagedValueTypeArrayMarshallerCopyToManaged(elementType, elementAbiType, elementMarshallerTypeSignature),
                freeMethod: interopReferences.WindowsRuntimeManagedValueTypeArrayMarshallerFree(elementType, elementAbiType, elementMarshallerTypeSignature),
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="elementMarshallerType">The element marshaller type produced by <see cref="SzArrayElementMarshaller.KeyValuePair"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition KeyValuePair(
            SzArrayTypeSignature arrayType,
            TypeDefinition elementMarshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            GenericInstanceTypeSignature elementType = (GenericInstanceTypeSignature)arrayType.BaseType;
            TypeSignature keyType = elementType.TypeArguments[0];
            TypeSignature valueType = elementType.TypeArguments[1];
            TypeSignature elementMarshallerTypeSignature = elementMarshallerType.ToTypeSignature();

            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.WindowsRuntimeKeyValuePairTypeArrayMarshallerConvertToUnmanaged(keyType, valueType, elementMarshallerTypeSignature),
                convertToManagedMethod: interopReferences.WindowsRuntimeKeyValuePairTypeArrayMarshallerConvertToManaged(keyType, valueType, elementMarshallerTypeSignature),
                copyToUnmanagedMethod: interopReferences.WindowsRuntimeKeyValuePairTypeArrayMarshallerCopyToUnmanaged(keyType, valueType, elementMarshallerTypeSignature),
                copyToManagedMethod: interopReferences.WindowsRuntimeKeyValuePairTypeArrayMarshallerCopyToManaged(keyType, valueType, elementMarshallerTypeSignature),
                freeMethod: interopReferences.WindowsRuntimeUnknownArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for a reference type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="elementMarshallerType">The element marshaller type produced by <see cref="SzArrayElementMarshaller.ReferenceType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition ReferenceType(
            SzArrayTypeSignature arrayType,
            TypeDefinition elementMarshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementMarshallerTypeSignature = elementMarshallerType.ToTypeSignature();

            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.WindowsRuntimeReferenceTypeArrayMarshallerConvertToUnmanaged(elementType, elementMarshallerTypeSignature),
                convertToManagedMethod: interopReferences.WindowsRuntimeReferenceTypeArrayMarshallerConvertToManaged(elementType, elementMarshallerTypeSignature),
                copyToUnmanagedMethod: interopReferences.WindowsRuntimeReferenceTypeArrayMarshallerCopyToUnmanaged(elementType, elementMarshallerTypeSignature),
                copyToManagedMethod: interopReferences.WindowsRuntimeReferenceTypeArrayMarshallerCopyToManaged(elementType, elementMarshallerTypeSignature),
                freeMethod: interopReferences.WindowsRuntimeUnknownArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for the <see cref="System.Object"/> type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition Object(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.WindowsRuntimeObjectArrayMarshallerConvertToUnmanaged,
                convertToManagedMethod: interopReferences.WindowsRuntimeObjectArrayMarshallerConvertToManaged,
                copyToUnmanagedMethod: interopReferences.WindowsRuntimeObjectArrayMarshallerCopyToUnmanaged,
                copyToManagedMethod: interopReferences.WindowsRuntimeObjectArrayMarshallerCopyToManaged,
                freeMethod: interopReferences.WindowsRuntimeUnknownArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for the <see cref="string"/> type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition String(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.HStringArrayMarshallerConvertToUnmanaged,
                convertToManagedMethod: interopReferences.HStringArrayMarshallerConvertToManaged,
                copyToUnmanagedMethod: interopReferences.HStringArrayMarshallerCopyToUnmanaged,
                copyToManagedMethod: interopReferences.HStringArrayMarshallerCopyToManaged,
                freeMethod: interopReferences.HStringArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for the <see cref="System.Type"/> type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition Type(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.TypeArrayMarshallerConvertToUnmanaged,
                convertToManagedMethod: interopReferences.TypeArrayMarshallerConvertToManaged,
                copyToUnmanagedMethod: interopReferences.TypeArrayMarshallerCopyToUnmanaged,
                copyToManagedMethod: interopReferences.TypeArrayMarshallerCopyToManaged,
                freeMethod: interopReferences.TypeArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a <see cref="TypeDefinition"/> for the marshaller for the <see cref="System.Exception"/> type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        public static TypeDefinition Exception(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            return Marshaller(
                arrayType: arrayType,
                convertToUnmanagedMethod: interopReferences.ExceptionArrayMarshallerConvertToUnmanaged,
                convertToManagedMethod: interopReferences.ExceptionArrayMarshallerConvertToManaged,
                copyToUnmanagedMethod: interopReferences.ExceptionArrayMarshallerCopyToUnmanaged,
                copyToManagedMethod: interopReferences.ExceptionArrayMarshallerCopyToManaged,
                freeMethod: interopReferences.WindowsRuntimeBlittableValueTypeArrayMarshallerFree,
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="convertToUnmanagedMethod"> The <c>ConvertToUnmanaged</c> implementation method to call.</param>
        /// <param name="convertToManagedMethod"> The <c>ConvertToManaged</c> implementation method to call.</param>
        /// <param name="copyToUnmanagedMethod"> The <c>CopyToUnmanaged</c> implementation method to call.</param>
        /// <param name="copyToManagedMethod"> The <c>CopyToManaged</c> implementation method to call.</param>
        /// <param name="freeMethod"> The <c>Free</c> implementation method to call.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <returns>The resulting marshaller type.</returns>
        private static TypeDefinition Marshaller(
            SzArrayTypeSignature arrayType,
            IMethodDescriptor convertToUnmanagedMethod,
            IMethodDescriptor convertToManagedMethod,
            IMethodDescriptor copyToUnmanagedMethod,
            IMethodDescriptor copyToManagedMethod,
            IMethodDescriptor freeMethod,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            TypeSignature elementType = arrayType.BaseType;
            TypeSignature elementAbiType = elementType.GetAbiType(interopReferences);

            // We're declaring an 'internal static class' type
            TypeDefinition marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "Marshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static void ConvertToUnmanaged(ReadOnlySpan<<ELEMENT_TYPE>>, out uint size, out <ABI_ELEMENT_TYPE>* array)
            MethodDefinition convertToUnmanagedForwarderMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.ReadOnlySpan1.MakeGenericValueType(elementType).Import(module),
                        module.CorLibTypeFactory.UInt32.MakeByReferenceType(),
                        elementAbiType.Import(module).MakePointerType().MakeByReferenceType()]))
            {
                CilOutParameterIndices = [2, 3],
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, convertToUnmanagedMethod.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(convertToUnmanagedForwarderMethod);

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <ELEMENT_TYPE>[] ConvertToManaged(uint size, <ABI_ELEMENT_TYPE>* value)
            MethodDefinition convertToManagedForwarderMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: arrayType.Import(module),
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        elementAbiType.Import(module).MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, convertToManagedMethod.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(convertToManagedForwarderMethod);

            // Define the 'CopyToManaged' method as follows:
            //
            // public static void CopyToManaged(uint size, <ABI_ELEMENT_TYPE>* value, Span<<ELEMENT_TYPE>> destination)
            MethodDefinition copyToManagedForwarderMethod = new(
                name: "CopyToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        elementAbiType.Import(module).MakePointerType(),
                        interopReferences.Span1.MakeGenericValueType(elementType).Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, copyToManagedMethod.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(copyToManagedForwarderMethod);

            // Define the 'CopyToUnmanaged' method as follows:
            //
            // public static void CopyToUnmanaged(ReadOnlySpan<<ELEMENT_TYPE>> value, uint size, <ABI_ELEMENT_TYPE>* destination)
            MethodDefinition copyToUnmanagedForwarderMethod = new(
                name: "CopyToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.ReadOnlySpan1.MakeGenericValueType(elementType).Import(module),
                        module.CorLibTypeFactory.UInt32,
                        elementAbiType.Import(module).MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, copyToUnmanagedMethod.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(copyToUnmanagedForwarderMethod);

            // Define the 'Free' method as follows:
            //
            // public static void Free(uint size, <ABI_ELEMENT_TYPE>* destination)
            MethodDefinition freeForwarderMethod = new(
                name: "Free"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        elementAbiType.Import(module).MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, freeMethod.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(freeForwarderMethod);

            return marshallerType;
        }
    }
}
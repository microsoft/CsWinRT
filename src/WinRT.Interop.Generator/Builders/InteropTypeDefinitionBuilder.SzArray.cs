// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for SZ array types.
    /// </summary>
    public static class SzArray
    {
        /// <summary>
        /// Creates the 'IID' property for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="arrayType"/>.</param>
        public static void IID(
            SzArrayTypeSignature arrayType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(arrayType, "ArrayIID"), // TODO
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ArrayMarshaller"), // TODO
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static void ConvertToUnmanaged(ReadOnlySpan<<ELEMENT_TYPE>>, out uint size, out <ABI_ELEMENT_TYPE>* array)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.ReadOnlySpan1.MakeGenericValueType(arrayType.BaseType).Import(module),
                        module.CorLibTypeFactory.UInt32.MakeByReferenceType(),
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType().MakeByReferenceType()])) // TODO
            {
                CilOutParameterIndices = [2, 3],
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <ELEMENT_TYPE>[] ConvertToManaged(uint size, <ABI_ELEMENT_TYPE>* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: arrayType.Import(module),
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()])) // TODO
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(convertToManagedMethod);

            // Define the 'CopyToManaged' method as follows:
            //
            // public static void CopyToManaged(uint size, <ABI_ELEMENT_TYPE>* value, Span<<ELEMENT_TYPE>> destination)
            MethodDefinition copyToManagedMethod = new(
                name: "CopyToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType(), // TODO
                        interopReferences.Span1.MakeGenericValueType(arrayType.BaseType).Import(module)]))
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(copyToManagedMethod);

            // Define the 'CopyToUnmanaged' method as follows:
            //
            // public static void CopyToUnmanaged(ReadOnlySpan<<ELEMENT_TYPE>> value, uint size, <ABI_ELEMENT_TYPE>* destination)
            MethodDefinition copyToUnmanagedMethod = new(
                name: "CopyToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.ReadOnlySpan1.MakeGenericValueType(arrayType.BaseType).Import(module),
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(copyToUnmanagedMethod);

            // Define the 'Free' method as follows:
            //
            // public static void Free(uint size, <ABI_ELEMENT_TYPE>* destination)
            MethodDefinition freeMethod = new(
                name: "Free"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(freeMethod);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeArrayComWrappersCallback</c> interface for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="marshallerType">The type returned by <see cref="Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            SzArrayTypeSignature arrayType,
            TypeDefinition marshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            // We're declaring an 'internal abstract class' type
            callbackType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ComWrappersCallback"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeArrayComWrappersCallback.Import(module)) }
            };

            module.TopLevelTypes.Add(callbackType);

            // Define the 'CreateArray' method as follows:
            //
            // public static Array CreateArray(uint count, void* value)
            MethodDefinition createArrayMethod = new(
                name: "CreateArray"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Array.ToReferenceTypeSignature().Import(module),
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, marshallerType.GetMethod("ConvertToManaged"u8) },
                    { Ret }
                }
            };

            // Add and implement 'CreateArray'
            callbackType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeArrayComWrappersCallbackCreateArray.Import(module),
                method: createArrayMethod);
        }
    }
}

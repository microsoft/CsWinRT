// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
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
    /// Helpers for <see cref="System.Collections.Generic.IList{T}"/> types.
    /// </summary>
    public static class IList1
    {
        /// <summary>
        /// Creates a new type definition for the vtable for an <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vftblType">The resulting vtable type.</param>
        public static void Vftbl(
            GenericInstanceTypeSignature listType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition vftblType)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // All reference types can share the same vtable type (as it just uses 'void*' for the ABI type)
            if (!elementType.IsValueType)
            {
                vftblType = interopDefinitions.IList1Vftbl;

                return;
            }

            // We can also share vtables for 'KeyValuePair<,>' types, as their ABI type is an interface
            if (elementType.IsKeyValuePairType(interopReferences))
            {
                vftblType = interopDefinitions.IList1Vftbl;

                return;
            }

            // Otherwise, we must construct a new specialized vtable type
            vftblType = WellKnownTypeDefinitionFactory.IList1Vftbl(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "Vftbl"),
                elementType: elementType, // TODO: use ABI type
                interopReferences: interopReferences,
                module: module);

            module.TopLevelTypes.Add(vftblType);
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vectorMethodsType">The resulting methods type.</param>
        public static void IVectorMethods(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition vectorMethodsType)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            vectorMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "IVectorMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IVectorMethodsImpl1.MakeGenericInstanceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(vectorMethodsType);

            // Define the 'GetAt' method as follows:
            //
            // public static <TYPE_ARGUMENT> GetAt(WindowsRuntimeObjectReference thisReference, uint index)
            MethodDefinition getAtMethod = new(
                name: "GetAt"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        module.CorLibTypeFactory.UInt32]))
            { NoInlining = true };

            vectorMethodsType.Methods.Add(getAtMethod);

            // Mark the 'GetAt' method as overriding the base method
            vectorMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1GetAt(elementType).Import(module),
                body: getAtMethod));

            // Create a method body for the 'GetAt' method
            getAtMethod.CilMethodBody = new CilMethodBody(getAtMethod)
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };

            // Define the 'SetAt' method as follows:
            //
            // public static void SetAt(WindowsRuntimeObjectReference thisReference, uint index, <TYPE_ARGUMENT> value)
            MethodDefinition setAtMethod = new(
                name: "SetAt"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        module.CorLibTypeFactory.UInt32,
                        elementType.Import(module)]))
            { NoInlining = true };

            vectorMethodsType.Methods.Add(setAtMethod);

            // Mark the 'SetAt' method as overriding the base method
            vectorMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1SetAt(elementType).Import(module),
                body: setAtMethod));

            // Create a method body for the 'SetAt' method
            setAtMethod.CilMethodBody = new CilMethodBody(setAtMethod)
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };

            // Define the 'Append' method as follows:
            //
            // public static void Append(WindowsRuntimeObjectReference thisReference, <TYPE_ARGUMENT> value)
            MethodDefinition appendMethod = new(
                name: "Append"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        elementType.Import(module)]))
            { NoInlining = true };

            vectorMethodsType.Methods.Add(appendMethod);

            // Mark the 'Append' method as overriding the base method
            vectorMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1Append(elementType).Import(module),
                body: appendMethod));

            // Create a method body for the 'Append' method
            appendMethod.CilMethodBody = new CilMethodBody(appendMethod)
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };

            // Define the 'IndexOf' method as follows:
            //
            // public static bool IndexOf(WindowsRuntimeObjectReference thisReference, <TYPE_ARGUMENT> value, out uint index)
            MethodDefinition indexOfMethod = new(
                name: "IndexOf"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        elementType.Import(module),
                        module.CorLibTypeFactory.UInt32.MakeByReferenceType()]))
            {
                NoInlining = true,
                ParameterDefinitions = { new ParameterDefinition(sequence: 3, name: null, attributes: ParameterAttributes.Out) }
            };

            vectorMethodsType.Methods.Add(indexOfMethod);

            // Mark the 'IndexOf' method as overriding the base method
            vectorMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1IndexOf(elementType).Import(module),
                body: indexOfMethod));

            // Create a method body for the 'IndexOf' method
            indexOfMethod.CilMethodBody = new CilMethodBody(indexOfMethod)
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };

            // Define the 'InsertAt' method as follows:
            //
            // public static void InsertAt(WindowsRuntimeObjectReference thisReference, uint index, <TYPE_ARGUMENT> value)
            MethodDefinition insertAtMethod = new(
                name: "InsertAt"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        module.CorLibTypeFactory.UInt32,
                        elementType.Import(module)]))
            { NoInlining = true };

            vectorMethodsType.Methods.Add(insertAtMethod);

            // Mark the 'InsertAt' method as overriding the base method
            vectorMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1InsertAt(elementType).Import(module),
                body: insertAtMethod));

            // Create a method body for the 'InsertAt' method
            insertAtMethod.CilMethodBody = new CilMethodBody(insertAtMethod)
            {
                Instructions = { { Ldnull }, { Throw } } // TODO
            };
        }
    }
}

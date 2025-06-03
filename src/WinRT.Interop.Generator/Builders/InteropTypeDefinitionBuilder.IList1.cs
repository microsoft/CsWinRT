// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
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
        /// Creates the 'IID' property for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="listType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature listType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(listType, "IID"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

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

        /// <summary>
        /// Creates a new type definition for the methods for an <see cref="System.Collections.Generic.IList{T}"/> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vectorMethodsType">The type returned by <see cref="IVectorMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="listMethodsType">The resulting methods type.</param>
        public static void IListMethods(
            GenericInstanceTypeSignature listType,
            TypeDefinition vectorMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition listMethodsType)
        {
            TypeSignature elementType = listType.TypeArguments[0];

            // We're declaring an 'internal static class' type
            listMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "IListMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(listMethodsType);

            // Define the 'Item' getter method as follows:
            //
            // public static <TYPE_ARGUMENT> Item(WindowsRuntimeObjectReference thisReference, int index)
            MethodDefinition get_ItemMethod = new(
                name: "Item"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        module.CorLibTypeFactory.Int32]));

            listMethodsType.Methods.Add(get_ItemMethod);

            // Create a method body for the 'Item' getter method
            get_ItemMethod.CilMethodBody = new CilMethodBody(get_ItemMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListMethods1get_Item(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Item' setter method as follows:
            //
            // public static void Item(WindowsRuntimeObjectReference thisReference, int index, <TYPE_ARGUMENT> value)
            MethodDefinition set_ItemMethod = new(
                name: "Item"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        module.CorLibTypeFactory.Int32,
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(set_ItemMethod);

            // Create a method body for the 'Item' setter method
            set_ItemMethod.CilMethodBody = new CilMethodBody(set_ItemMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IListMethods1set_Item(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Count' method as follows:
            //
            // public static int Count(WindowsRuntimeObjectReference thisReference)
            MethodDefinition countMethod = new(
                name: "Count"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]));

            listMethodsType.Methods.Add(countMethod);

            // Create a method body for the 'Count' method
            countMethod.CilMethodBody = new CilMethodBody(countMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IListMethodsCount.Import(module) },
                    { Ret }
                }
            };

            // Define the 'Add' method as follows:
            //
            // public static void Add(WindowsRuntimeObjectReference thisReference, <TYPE_ARGUMENT> item)
            MethodDefinition addMethod = new(
                name: "Add"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(addMethod);

            // Create a method body for the 'Add' method
            addMethod.CilMethodBody = new CilMethodBody(addMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListMethods1Add(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Contains' method as follows:
            //
            // public static bool Contains(WindowsRuntimeObjectReference thisReference, <TYPE_ARGUMENT> item)
            MethodDefinition containsMethod = new(
                name: "Contains"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(containsMethod);

            // Create a method body for the 'Contains' method
            containsMethod.CilMethodBody = new CilMethodBody(containsMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListMethods1Contains(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'CopyTo' method as follows:
            //
            // public static void CopyTo(WindowsRuntimeObjectReference thisReference, <TYPE_ARGUMENT>[] array, int arrayIndex)
            MethodDefinition copyToMethod = new(
                name: "CopyTo"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        elementType.Import(module).MakeSzArrayType(),
                        module.CorLibTypeFactory.Int32]));

            listMethodsType.Methods.Add(copyToMethod);

            // Create a method body for the 'CopyTo' method
            copyToMethod.CilMethodBody = new CilMethodBody(copyToMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IListMethods1CopyTo(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Remove' method as follows:
            //
            // public static bool Remove(WindowsRuntimeObjectReference thisReference, <TYPE_ARGUMENT> item)
            MethodDefinition removeMethod = new(
                name: "Remove"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(removeMethod);

            // Create a method body for the 'Remove' method
            removeMethod.CilMethodBody = new CilMethodBody(removeMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListMethods1Remove(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'IndexOf' method as follows:
            //
            // public static int IndexOf(WindowsRuntimeObjectReference thisReference, <TYPE_ARGUMENT> item)
            MethodDefinition indexOfMethod = new(
                name: "IndexOf"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(indexOfMethod);

            // Create a method body for the 'IndexOf' method
            indexOfMethod.CilMethodBody = new CilMethodBody(indexOfMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListMethods1IndexOf(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Insert' method as follows:
            //
            // public static void Insert(WindowsRuntimeObjectReference thisReference, int index, <TYPE_ARGUMENT> item)
            MethodDefinition insertMethod = new(
                name: "Insert"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                        module.CorLibTypeFactory.Int32,
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(insertMethod);

            // Create a method body for the 'Insert' method
            insertMethod.CilMethodBody = new CilMethodBody(insertMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, interopReferences.IListMethods1Insert(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'Clear' method as follows:
            //
            // public static void Clear(WindowsRuntimeObjectReference thisReference)
            MethodDefinition clearMethod = new(
                name: "Clear"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]));

            listMethodsType.Methods.Add(clearMethod);

            // Create a method body for the 'Clear' method
            clearMethod.CilMethodBody = new CilMethodBody(clearMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IListMethodsClear.Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vectorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IVectorMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature listType,
            TypeDefinition vectorMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = listType.TypeArguments[0];
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericInstanceType(elementType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeList<<ELEMENT_TYPE>, <IENUMERABLE_INTERFACE>, <IITERABLE_METHODS, <IVECTOR_METHODS>>'
            TypeSignature windowsRuntimeList4Type = interopReferences.WindowsRuntimeList4.MakeGenericInstanceType(
                elementType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToTypeSignature(isValueType: false),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToTypeSignature(isValueType: false),
                vectorMethodsType.ToTypeSignature(isValueType: false));

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: listType,
                nativeObjectBaseType: windowsRuntimeList4Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature listType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            InteropTypeDefinitionBuilder.ComWrappersCallbackType(
                runtimeClassName: listType.FullName, // TODO
                typeSignature: listType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature listType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: listType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="listComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature listType,
            TypeDefinition listComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: listType,
                interfaceComWrappersCallbackType: listComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }
    }
}

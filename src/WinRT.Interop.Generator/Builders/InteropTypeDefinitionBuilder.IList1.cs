// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
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
        /// Creates a new type definition for the interface type for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="listType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="interfaceType">The resulting interface type.</param>
        public static void Interface(
            GenericInstanceTypeSignature listType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceType)
        {
            // We're declaring an 'internal abstract class' type
            interfaceType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "Interface"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeInterface.Import(module)) }
            };

            module.TopLevelTypes.Add(interfaceType);

            // Track the type (it's needed by 'IObservableVector<T>')
            emitState.TrackTypeDefinition(interfaceType, listType, "Interface");

            // Create the public 'IID' property
            WellKnownMemberDefinitionFactory.IID(
                forwardedIidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out MethodDefinition get_IidMethod2,
                out PropertyDefinition iidProperty);

            interfaceType.Properties.Add(iidProperty);

            // Add and implement the 'get_IID' method
            interfaceType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeInterfaceget_IID.Import(module),
                method: get_IidMethod2);
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

            // All reference types can share the same vtable type (as it just uses 'void*' for the ABI type).
            // We can also share vtables for 'KeyValuePair<,>' types, as their ABI type is an interface.
            if (!elementType.IsValueType || elementType.IsConstructedKeyValuePairType(interopReferences))
            {
                vftblType = interopDefinitions.IList1Vftbl;

                return;
            }

            // Otherwise, we must construct a new specialized vtable type
            vftblType = WellKnownTypeDefinitionFactory.IList1Vftbl(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "Vftbl"),
                elementType: elementType.GetAbiType(interopReferences),
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
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vectorMethodsType">The resulting methods type.</param>
        public static void IVectorMethods(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
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
                Interfaces = { new InterfaceImplementation(interopReferences.IVectorMethodsImpl1.MakeGenericReferenceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(vectorMethodsType);

            // Track the type (it's needed by 'IObservableVector<T>')
            emitState.TrackTypeDefinition(vectorMethodsType, listType, "IVectorMethods");

            // Define the 'GetAt' method as follows:
            //
            // public static <TYPE_ARGUMENT> GetAt(WindowsRuntimeObjectReference thisReference, uint index)
            MethodDefinition getAtMethod = new(
                name: "GetAt"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType.Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        module.CorLibTypeFactory.UInt32]))
            { NoInlining = true };

            // Add and implement the 'GetAt' method
            vectorMethodsType.AddMethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1GetAt(elementType).Import(module),
                method: getAtMethod);

            // Create a method body for the 'GetAt' method
            getAtMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        module.CorLibTypeFactory.UInt32,
                        elementType.Import(module)]))
            { NoInlining = true };

            // Add and implement the 'SetAt' method
            vectorMethodsType.AddMethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1SetAt(elementType).Import(module),
                method: setAtMethod);

            // Create a method body for the 'SetAt' method
            setAtMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        elementType.Import(module)]))
            { NoInlining = true };

            // Add and implement the 'Append' method
            vectorMethodsType.AddMethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1Append(elementType).Import(module),
                method: appendMethod);

            // Create a method body for the 'Append' method
            appendMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        elementType.Import(module),
                        module.CorLibTypeFactory.UInt32.MakeByReferenceType()]))
            {
                NoInlining = true,
                CilOutParameterIndices = [3]
            };

            // Add and implement the 'IndexOf' method
            vectorMethodsType.AddMethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1IndexOf(elementType).Import(module),
                method: indexOfMethod);

            // Create a method body for the 'IndexOf' method
            indexOfMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        module.CorLibTypeFactory.UInt32,
                        elementType.Import(module)]))
            { NoInlining = true };

            // Add and implement the 'InsertAt' method
            vectorMethodsType.AddMethodImplementation(
                declaration: interopReferences.IVectorMethodsImpl1InsertAt(elementType).Import(module),
                method: insertAtMethod);

            // Create a method body for the 'InsertAt' method
            insertAtMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        module.CorLibTypeFactory.Int32]));

            listMethodsType.Methods.Add(get_ItemMethod);

            // Create a method body for the 'Item' getter method
            get_ItemMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        module.CorLibTypeFactory.Int32,
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(set_ItemMethod);

            // Create a method body for the 'Item' setter method
            set_ItemMethod.CilMethodBody = new CilMethodBody()
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
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]));

            listMethodsType.Methods.Add(countMethod);

            // Create a method body for the 'Count' method
            countMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(addMethod);

            // Create a method body for the 'Add' method
            addMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(containsMethod);

            // Create a method body for the 'Contains' method
            containsMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        elementType.Import(module).MakeSzArrayType(),
                        module.CorLibTypeFactory.Int32]));

            listMethodsType.Methods.Add(copyToMethod);

            // Create a method body for the 'CopyTo' method
            copyToMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(removeMethod);

            // Create a method body for the 'Remove' method
            removeMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListMethods1Remove(elementType, vectorMethodsType).Import(module) },
                    { Ret }
                }
            };

            // Define the 'RemoveAt' method as follows:
            //
            // public static void RemoveAt(WindowsRuntimeObjectReference thisReference, int index)
            MethodDefinition removeAtMethod = new(
                name: "RemoveAt"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        module.CorLibTypeFactory.Int32]));

            listMethodsType.Methods.Add(removeAtMethod);

            // Create a method body for the 'RemoveAt' method
            removeAtMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IListMethodsRemoveAt.Import(module) },
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
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(indexOfMethod);

            // Create a method body for the 'IndexOf' method
            indexOfMethod.CilMethodBody = new CilMethodBody()
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
                        interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                        module.CorLibTypeFactory.Int32,
                        elementType.Import(module)]));

            listMethodsType.Methods.Add(insertMethod);

            // Create a method body for the 'Insert' method
            insertMethod.CilMethodBody = new CilMethodBody()
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
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]));

            listMethodsType.Methods.Add(clearMethod);

            // Create a method body for the 'Clear' method
            clearMethod.CilMethodBody = new CilMethodBody()
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
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(elementType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeList<<ELEMENT_TYPE>, <IENUMERABLE_INTERFACE>, <IITERABLE_METHODS, <IVECTOR_METHODS>>'
            TypeSignature windowsRuntimeList4Type = interopReferences.WindowsRuntimeList4.MakeGenericReferenceType(
                elementType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToReferenceTypeSignature(),
                vectorMethodsType.ToReferenceTypeSignature());

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
            ComWrappersCallback(
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

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="listMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IListMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature listType,
            TypeDefinition listMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = listType.TypeArguments[0];
            TypeSignature collectionType = interopReferences.ICollection1.MakeGenericReferenceType(elementType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(elementType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(listType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(collectionType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(enumerableType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Get the getter and setter accessor methods for 'IList<T>'
            MethodDefinition[] itemMethods = listMethodsType.GetMethods("Item"u8);

            // Create the 'get_Item' getter method
            MethodDefinition get_ItemMethod = new(
                name: $"System.Collections.Generic.IList<{elementType.FullName}>.get_Item",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(elementType.Import(module), module.CorLibTypeFactory.Int32));

            // Add and implement the 'get_Item' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IList1get_Item(elementType).Import(module),
                method: get_ItemMethod);

            // Create a body for the 'get_Item' method
            get_ItemMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: get_ItemMethod,
                forwardedMethod: itemMethods[0],
                interopReferences: interopReferences,
                module: module);

            // Create the 'set_Item' getter method
            MethodDefinition set_ItemMethod = new(
                name: $"System.Collections.Generic.IList<{elementType.FullName}>.set_Item",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.Int32,
                        elementType.Import(module)]));

            // Add and implement the 'set_Item' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IList1set_Item(elementType).Import(module),
                method: set_ItemMethod);

            // Create a body for the 'set_Item' method
            set_ItemMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: set_ItemMethod,
                forwardedMethod: itemMethods[1],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Item' property
            PropertyDefinition itemProperty = new(
                name: $"System.Collections.Generic.IList<{elementType.FullName}>.Item",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_ItemMethod))
            {
                GetMethod = get_ItemMethod,
                SetMethod = set_ItemMethod
            };

            interfaceImplType.Properties.Add(itemProperty);

            // Create the 'IndexOf' method
            MethodDefinition indexOfMethod = new(
                name: $"System.Collections.Generic.IList<{elementType.FullName}>.IndexOf",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Int32, elementType.Import(module)));

            // Add and implement the 'IndexOf' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IList1get_Item(elementType).Import(module),
                method: indexOfMethod);

            // Create a body for the 'IndexOf' method
            indexOfMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: indexOfMethod,
                forwardedMethod: listMethodsType.GetMethod("IndexOf"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Insert' method
            MethodDefinition insertMethod = new(
                name: $"System.Collections.Generic.IList<{elementType.FullName}>.Insert",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.Int32,
                        elementType.Import(module)]));

            // Add and implement the 'Insert' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IList1Insert(elementType).Import(module),
                method: insertMethod);

            // Create a body for the 'Insert' method
            insertMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: insertMethod,
                forwardedMethod: listMethodsType.GetMethod("Insert"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'RemoveAt' method
            MethodDefinition removeAtMethod = new(
                name: $"System.Collections.Generic.IList<{elementType.FullName}>.RemoveAt",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, module.CorLibTypeFactory.Int32));

            // Add and implement the 'RemoveAt' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IList1RemoveAt(elementType).Import(module),
                method: removeAtMethod);

            // Create a body for the 'RemoveAt' method
            removeAtMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: removeAtMethod,
                forwardedMethod: listMethodsType.GetMethod("RemoveAt"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'get_Count' getter method
            MethodDefinition get_CountMethod = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.get_Count",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Int32));

            // Add and implement the 'get_Count' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1get_Count(elementType).Import(module),
                method: get_CountMethod);

            // Create a body for the 'get_Count' method
            get_CountMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: get_CountMethod,
                forwardedMethod: listMethodsType.GetMethod("Count"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Count' property
            PropertyDefinition countProperty = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.Count",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CountMethod))
            { GetMethod = get_CountMethod };

            interfaceImplType.Properties.Add(countProperty);

            // Create the 'get_IsReadOnly' getter method
            MethodDefinition get_IsReadOnlyMethod = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.get_IsReadOnly",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Boolean));

            // Add and implement the 'get_IsReadOnly' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1get_IsReadOnly(elementType).Import(module),
                method: get_IsReadOnlyMethod);

            // Create a body for the 'get_IsReadOnly' method
            get_IsReadOnlyMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldc_I4_0 },
                    { Ret }
                }
            };

            // Create the 'IsReadOnly' property
            PropertyDefinition isReadOnlyProperty = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.IsReadOnly",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_IsReadOnlyMethod))
            { GetMethod = get_IsReadOnlyMethod };

            interfaceImplType.Properties.Add(isReadOnlyProperty);

            // Create the 'Add' method
            MethodDefinition addMethod = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.Add",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, elementType.Import(module)));

            // Add and implement the 'Add' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Add(elementType).Import(module),
                method: addMethod);

            // Create a body for the 'Add' method
            addMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: addMethod,
                forwardedMethod: listMethodsType.GetMethod("Add"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Clear' method
            MethodDefinition clearMethod = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.Clear",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void));

            // Add and implement the 'Clear' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Clear(elementType).Import(module),
                method: clearMethod);

            // Create a body for the 'Clear' method
            clearMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: clearMethod,
                forwardedMethod: listMethodsType.GetMethod("Clear"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Contains' method
            MethodDefinition containsMethod = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.Contains",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Boolean, elementType.Import(module)));

            // Add and implement the 'Contains' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Contains(elementType).Import(module),
                method: containsMethod);

            // Create a body for the 'Contains' method
            containsMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: containsMethod,
                forwardedMethod: listMethodsType.GetMethod("Contains"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'CopyTo' method
            MethodDefinition copyToMethod = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.CopyTo",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        elementType.MakeSzArrayType().Import(module),
                        module.CorLibTypeFactory.Int32]));

            // Add and implement the 'CopyTo' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1CopyTo(elementType).Import(module),
                method: copyToMethod);

            // Create a body for the 'CopyTo' method
            copyToMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: copyToMethod,
                forwardedMethod: listMethodsType.GetMethod("CopyTo"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Remove' method
            MethodDefinition removeMethod = new(
                name: $"System.Collections.Generic.ICollection<{elementType.FullName}>.Remove",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Boolean, elementType.Import(module)));

            // Add and implement the 'Remove' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Remove(elementType).Import(module),
                method: removeMethod);

            // Create a body for the 'Remove' method
            removeMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: listType,
                implementationMethod: removeMethod,
                forwardedMethod: listMethodsType.GetMethod("Remove"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'IEnumerable<T>.GetEnumerator' method
            MethodDefinition enumerable1GetEnumeratorMethod = new(
                name: $"System.Collections.Generic.IEnumerable<{elementType.FullName}>.GetEnumerator",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator1.MakeGenericReferenceType(elementType).Import(module)));

            // Add and implement the 'IEnumerable<T>.GetEnumerator' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumerable1GetEnumerator(elementType).Import(module),
                method: enumerable1GetEnumeratorMethod);

            // Create a method body for the 'IEnumerable<T>.GetEnumerator' method
            enumerable1GetEnumeratorMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: enumerableType,
                implementationMethod: enumerable1GetEnumeratorMethod,
                forwardedMethod: emitState.LookupTypeDefinition(enumerableType, "IEnumerableMethods").GetMethod("GetEnumerator"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'IEnumerable.GetEnumerator' method
            MethodDefinition enumerableGetEnumeratorMethod = new(
                name: "System.Collections.IEnumerable.GetEnumerator"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator.Import(module).ToReferenceTypeSignature()));

            // Add and implement the 'IEnumerable.GetEnumerator' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumerableGetEnumerator.Import(module),
                method: enumerableGetEnumeratorMethod);

            // Create a method body for the 'IEnumerable.GetEnumerator' method
            enumerableGetEnumeratorMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Callvirt, interopReferences.IEnumerable1GetEnumerator(elementType).Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature listType,
            TypeDefinition vftblType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType, "Impl"),
                vftblType: vftblType,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: []);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, listType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="listComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature listType,
            TypeDefinition listComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IVector`1<{listType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(listType),
                name: InteropUtf8NameFactory.TypeName(listType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: listComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IVector&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="listType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IList{T}"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature listType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IVector`1<{listType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: listType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: listType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}
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
    /// Helpers for <see cref="System.Collections.Generic.IReadOnlyList{T}"/> types.
    /// </summary>
    public static class IReadOnlyList1
    {
        /// <summary>
        /// Creates a new type definition for the vtable for an <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vftblType">The resulting vtable type.</param>
        public static void Vftbl(
            GenericInstanceTypeSignature readOnlyListType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition vftblType)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // Same logic as with 'IList1.Vftbl' (i.e. share for all reference types)
            if (!elementType.IsValueType || elementType.IsConstructedKeyValuePairType(interopReferences))
            {
                vftblType = interopDefinitions.IReadOnlyList1Vftbl;

                return;
            }

            // Otherwise, we must construct a new specialized vtable type
            vftblType = WellKnownTypeDefinitionFactory.IReadOnlyList1Vftbl(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "Vftbl"),
                elementType: elementType.GetAbiType(interopReferences),
                interopReferences: interopReferences);

            module.TopLevelTypes.Add(vftblType);
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vectorViewMethodsType">The resulting methods type.</param>
        public static void IVectorViewMethods(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition vectorViewMethodsType)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            vectorViewMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "IVectorViewMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IVectorViewMethods1.MakeGenericReferenceType(elementType).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(vectorViewMethodsType);

            // Track the type
            emitState.TrackTypeDefinition(vectorViewMethodsType, readOnlyListType, "IVectorViewMethods");

            // Define the 'GetAt' method
            MethodDefinition getAtMethod = InteropMethodDefinitionFactory.IVectorViewMethods.GetAt(
                readOnlyListType: readOnlyListType,
                vftblType: vftblType,
                interopReferences: interopReferences,
                emitState: emitState);

            // Add and implement the 'GetAt' method
            vectorViewMethodsType.AddMethodImplementation(
                declaration: interopReferences.IVectorViewMethods1GetAt(elementType),
                method: getAtMethod);
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <see cref="System.Collections.Generic.IReadOnlyList{T}"/> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="vectorViewMethodsType">The type returned by <see cref="IVectorViewMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="readOnlyListMethodsType">The resulting methods type.</param>
        public static void IReadOnlyListMethods(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition vectorViewMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition readOnlyListMethodsType)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // We're declaring an 'internal static class' type
            readOnlyListMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "IReadOnlyListMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(readOnlyListMethodsType);

            // Define the 'Item' getter method as follows:
            //
            // public static <TYPE_ARGUMENT> Item(WindowsRuntimeObjectReference thisReference, int index)
            MethodDefinition get_ItemMethod = new(
                name: "Item"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                        interopReferences.Int32]));

            readOnlyListMethodsType.Methods.Add(get_ItemMethod);

            // Create a method body for the 'Item' method
            get_ItemMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IReadOnlyListMethods1get_Item(elementType, vectorViewMethodsType) },
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
                    returnType: interopReferences.Int32,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

            readOnlyListMethodsType.Methods.Add(countMethod);

            // Create a method body for the 'Count' method
            countMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IReadOnlyListMethodsCount },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature readOnlyListType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(elementType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeReadOnlyList<<ELEMENT_TYPE>, <IENUMERABLE_INTERFACE>, <IITERABLE_METHODS>, <IVECTORVIEW_METHODS>>'
            TypeSignature windowsRuntimeReadOnlyList4Type = interopReferences.WindowsRuntimeReadOnlyList4.MakeGenericReferenceType(
                elementType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToReferenceTypeSignature(),
                emitState.LookupTypeDefinition(readOnlyListType, "IVectorViewMethods").ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: readOnlyListType,
                nativeObjectBaseType: windowsRuntimeReadOnlyList4Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature readOnlyListType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: readOnlyListType.FullName, // TODO
                typeSignature: readOnlyListType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: readOnlyListType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="readOnlyListComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition readOnlyListComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: readOnlyListType,
                interfaceComWrappersCallbackType: readOnlyListComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);

            // Track the type (it may be needed to marshal parameters or return values)
            emitState.TrackTypeDefinition(marshallerType, readOnlyListType, "Marshaller");
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="readOnlyListMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IReadOnlyListMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition readOnlyListMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];
            TypeSignature readOnlyCollectionType = interopReferences.IReadOnlyCollection1.MakeGenericReferenceType(elementType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(elementType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor) },
                Interfaces =
                {
                    new InterfaceImplementation(readOnlyListType.ToTypeDefOrRef()),
                    new InterfaceImplementation(readOnlyCollectionType.ToTypeDefOrRef()),
                    new InterfaceImplementation(enumerableType.ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable)
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'get_Item' getter method
            MethodDefinition get_ItemMethod = new(
                name: $"System.Collections.Generic.IReadOnlyList<{elementType.FullName}>.get_Item",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(elementType, interopReferences.Int32));

            // Add and implement the 'get_Item' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyList1get_Item(elementType),
                method: get_ItemMethod);

            // Create a body for the 'get_Item' method
            get_ItemMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: readOnlyListType,
                implementationMethod: get_ItemMethod,
                forwardedMethod: readOnlyListMethodsType.GetMethod("Item"u8),
                interopReferences: interopReferences);

            // Create the 'Item' property
            PropertyDefinition itemProperty = new(
                name: $"System.Collections.Generic.IReadOnlyList<{elementType.FullName}>.Item",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_ItemMethod))
            { GetMethod = get_ItemMethod };

            interfaceImplType.Properties.Add(itemProperty);

            // Create the 'get_Count' getter method
            MethodDefinition get_CountMethod = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<{elementType.FullName}>.get_Count",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.Int32));

            // Add and implement the 'get_Count' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyCollection1get_Count(elementType),
                method: get_CountMethod);

            // Create a body for the 'get_Count' method
            get_CountMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: readOnlyListType,
                implementationMethod: get_CountMethod,
                forwardedMethod: readOnlyListMethodsType.GetMethod("Count"u8),
                interopReferences: interopReferences);

            // Create the 'Count' property
            PropertyDefinition countProperty = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<{elementType.FullName}>.Count",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CountMethod))
            { GetMethod = get_CountMethod };

            interfaceImplType.Properties.Add(countProperty);

            // Create the 'IEnumerable<T>.GetEnumerator' method
            MethodDefinition enumerable1GetEnumeratorMethod = new(
                name: $"System.Collections.Generic.IEnumerable<{elementType.FullName}>.GetEnumerator",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator1.MakeGenericReferenceType(elementType)));

            // Add and implement the 'IEnumerable<T>.GetEnumerator' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumerable1GetEnumerator(elementType),
                method: enumerable1GetEnumeratorMethod);

            // Create a method body for the 'IEnumerable<T>.GetEnumerator' method
            enumerable1GetEnumeratorMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: enumerableType,
                implementationMethod: enumerable1GetEnumeratorMethod,
                forwardedMethod: emitState.LookupTypeDefinition(enumerableType, "IEnumerableMethods").GetMethod("GetEnumerator"u8),
                interopReferences: interopReferences);

            // Create the 'IEnumerable.GetEnumerator' method
            MethodDefinition enumerableGetEnumeratorMethod = new(
                name: "System.Collections.IEnumerable.GetEnumerator"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator.ToReferenceTypeSignature()));

            // Add and implement the 'IEnumerable.GetEnumerator' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumerableGetEnumerator,
                method: enumerableGetEnumeratorMethod);

            // Create a method body for the 'IEnumerable.GetEnumerator' method
            enumerableGetEnumeratorMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Callvirt, interopReferences.IEnumerable1GetEnumerator(elementType) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition vftblType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "Impl"),
                vftblType: vftblType,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: []);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, readOnlyListType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="readOnlyListComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition readOnlyListComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IVectorView`1<{readOnlyListType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: readOnlyListComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IVectorView`1<{readOnlyListType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: readOnlyListType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: readOnlyListType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
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
    /// Helpers for <see cref="System.Collections.Generic.IEnumerable{T}"/> types.
    /// </summary>
    public static class IEnumerable1
    {
        /// <summary>
        /// Creates a new type definition for the interface type for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="enumerableType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="interfaceType">The resulting interface type.</param>
        public static void Interface(
            GenericInstanceTypeSignature enumerableType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceType)
        {
            // We're declaring an 'internal abstract class' type
            interfaceType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "Interface"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeInterface) }
            };

            module.TopLevelTypes.Add(interfaceType);

            // Track the type (it's needed by 'IReadOnlyList<T>')
            emitState.TrackTypeDefinition(interfaceType, enumerableType, "Interface");

            // Create the public 'IID' property
            WellKnownMemberDefinitionFactory.IID(
                forwardedIidMethod: get_IidMethod,
                interopReferences: interopReferences,
                out MethodDefinition get_IidMethod2,
                out PropertyDefinition iidProperty);

            interfaceType.Properties.Add(iidProperty);

            // Add and implement the 'get_IID' method
            interfaceType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeInterfaceget_IID,
                method: get_IidMethod2);
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="iterableMethodsType">The resulting methods type.</param>
        public static void IIterableMethods(
            GenericInstanceTypeSignature enumerableType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition iterableMethodsType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal static class' type
            iterableMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "IIterableMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IIterableMethodsImpl1.MakeGenericReferenceType(elementType).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(iterableMethodsType);

            // Track the type (it's needed by 'IList<T>' and 'IReadOnlyList<T>')
            emitState.TrackTypeDefinition(iterableMethodsType, enumerableType, "IIterableMethods");

            // Define the 'First' method as follows:
            //
            // public static IEnumerator<<TYPE_ARGUMENT>> First(WindowsRuntimeObjectReference thisReference)
            MethodDefinition firstMethod = new(
                name: "First"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.IEnumerator1.MakeGenericReferenceType(elementType),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            { NoInlining = true };

            // Add and implement the 'First' method
            iterableMethodsType.AddMethodImplementation(
                declaration: interopReferences.IIterableMethodsImpl1First(elementType),
                method: firstMethod);

            // Get the generated 'ConvertToManaged' method to marshal the 'IEnumerator<T>' instance to managed
            MethodDefinition convertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.IEnumerator1.MakeGenericReferenceType(elementType),
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'void*' (the enumerator pointer that was retrieved)
            //   [3]: 'IEnumerator<<TYPE_ARGUMENT>>' (the marshalled enumerator)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());
            CilLocalVariable loc_1_thisPtr = new(interopReferences.Void.MakePointerType());
            CilLocalVariable loc_2_enumeratorPtr = new(interopReferences.Void.MakePointerType());
            CilLocalVariable loc_3_enumerator = new(interopReferences.IEnumerator1.MakeGenericReferenceType(elementType));

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction ldloc_2_tryStart = new(Ldloc_2);
            CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
            CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

            // Create a method body for the 'First' method
            firstMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_enumeratorPtr, loc_3_enumerator },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue },
                    { Stloc_0 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldloca_S, loc_2_enumeratorPtr },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.IEnumerable1Vftbl.GetField("First"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IEnumerable1FirstImpl(interopReferences).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose },
                    { Endfinally },
                    { nop_finallyEnd },

                    // '.try/.finally' code to marshal the enumerator
                    { ldloc_2_tryStart },
                    { Call, convertToManagedMethod },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_finallyEnd.CreateLabel() },
                    { ldloc_2_finallyStart },
                    { Call, interopReferences.WindowsRuntimeUnknownMarshallerFree },
                    { Endfinally },
                    { ldloc_3_finallyEnd },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloca_s_0_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerEnd = nop_finallyEnd.CreateLabel()
                    },
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloc_2_tryStart.CreateLabel(),
                        TryEnd = ldloc_2_finallyStart.CreateLabel(),
                        HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                        HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <see cref="System.Collections.Generic.IEnumerable{T}"/> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="iterableMethodsType">The type returned by <see cref="IIterableMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="enumerableMethodsType">The resulting methods type.</param>
        public static void IEnumerableMethods(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition iterableMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition enumerableMethodsType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal static class' type
            enumerableMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "IEnumerableMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(enumerableMethodsType);

            // Track the type (it's needed by interface implementations)
            emitState.TrackTypeDefinition(enumerableMethodsType, enumerableType, "IEnumerableMethods");

            // Define the 'GetEnumerator' method as follows:
            //
            // public static IEnumerator<<TYPE_ARGUMENT>> GetEnumerator(WindowsRuntimeObjectReference thisReference)
            MethodDefinition getEnumeratorMethod = new(
                name: "GetEnumerator"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.IEnumerator1.MakeGenericReferenceType(elementType),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

            enumerableMethodsType.Methods.Add(getEnumeratorMethod);

            // Create a method body for the 'GetEnumerator' method
            getEnumeratorMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, iterableMethodsType.GetMethod("First"u8) },
                    { Ret }
                },
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="iterableMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIterableMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition iterableMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            // The 'NativeObject' is deriving from 'WindowsRuntimeEnumerable<<ELEMENT_TYPE>, <IITERABLE_METHODS>>'
            TypeSignature windowsRuntimeEnumerable1Type = interopReferences.WindowsRuntimeEnumerable2.MakeGenericReferenceType(
                enumerableType.TypeArguments[0],
                iterableMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: enumerableType,
                nativeObjectBaseType: windowsRuntimeEnumerable1Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="enumerableType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature enumerableType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: enumerableType.FullName, // TODO
                typeSignature: enumerableType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="enumerableType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: enumerableType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="enumerableComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="enumerableType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition enumerableComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: enumerableType,
                interfaceComWrappersCallbackType: enumerableComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);

            // Track the type (it may be needed to marshal parameters or return values)
            emitState.TrackTypeDefinition(marshallerType, enumerableType, "Marshaller");
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="iterableMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIterableMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition iterableMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor) },
                Interfaces =
                {
                    new InterfaceImplementation(enumerableType.ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable)
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

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
                forwardedMethod: iterableMethodsType.GetMethod("First"u8),
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
        /// Creates a new type definition for the implementation of the vtable for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature enumerableType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // Define the 'First' method
            MethodDefinition firstMethod = InteropMethodDefinitionFactory.IEnumerable1Impl.First(
                enumerableType: enumerableType,
                interopReferences: interopReferences,
                emitState: emitState);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "Impl"),
                vftblType: interopDefinitions.IEnumerable1Vftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [firstMethod]);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, enumerableType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="enumerableComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition enumerableComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IIterable`1<{enumerableType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: enumerableComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature enumerableType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IIterable`1<{enumerableType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: enumerableType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: enumerableType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}
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
    /// Helpers for <see cref="System.Collections.Generic.IEnumerator{T}"/> types.
    /// </summary>
    public static class IEnumerator1
    {
        /// <summary>
        /// Creates a new type definition for the methods for an <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="iteratorMethodsType">The resulting methods type.</param>
        public static void IIteratorMethods(
            GenericInstanceTypeSignature enumeratorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition iteratorMethodsType)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            iteratorMethodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "IIteratorMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IIteratorMethodsImpl1.MakeGenericReferenceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(iteratorMethodsType);

            // Define the 'Current' method as follows:
            //
            // public static <TYPE_ARGUMENT> Current(WindowsRuntimeObjectReference thisReference)
            MethodDefinition currentMethod = new(
                name: "Current"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: enumeratorType.TypeArguments[0].Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]))
            { NoInlining = true };

            // Add and implement the 'Current' method
            iteratorMethodsType.AddMethodImplementation(
                declaration: interopReferences.IIteratorMethodsImpl1Current(elementType).Import(module),
                method: currentMethod);

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'void*' (the native value that was retrieved)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature().Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_currentNative = enumeratorType.TypeArguments[0].IsValueType ? new(enumeratorType.TypeArguments[0].Import(module)) : new(module.CorLibTypeFactory.Void.MakePointerType());

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);

            // Create a method body for the 'Current' method
            currentMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_currentNative },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldloca_S, loc_2_currentNative },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.IEnumerator1Vftbl.GetField("get_Current"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IEnumerator1CurrentImpl(interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },
                    { nop_finallyEnd }
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
                    }
                }
            };

            // If the value is blittable, return it directly
            if (enumeratorType.TypeArguments[0].IsValueType) // TODO
            {
                _ = currentMethod.CilMethodBody.Instructions.Add(Ldloc_2);
                _ = currentMethod.CilMethodBody.Instructions.Add(Ret);
            }
            else
            {
                CilInstructionCollection instructions = currentMethod.CilMethodBody.Instructions;

                // Declare an additional variable:
                //   [3]: '<TYPE_ARGUMENT>' (for the marshalled value)
                CilLocalVariable loc_3_current = new(enumeratorType.TypeArguments[0].Import(module));

                currentMethod.CilMethodBody.LocalVariables.Add(loc_3_current);

                // Jump labels for the 'try/finally' blocks
                CilInstruction ldloc_2_tryStart = new(Ldloc_2);
                CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
                CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

                // We need to marshal the native value to a managed object
                instructions.Add(ldloc_2_tryStart);
                _ = instructions.Add(Call, interopReferences.HStringMarshallerConvertToManaged.Import(module));
                _ = instructions.Add(Stloc_3);
                _ = instructions.Add(Leave_S, ldloc_3_finallyEnd.CreateLabel());
                instructions.Add(ldloc_2_finallyStart);
                _ = instructions.Add(Call, interopReferences.HStringMarshallerFree.Import(module));
                _ = instructions.Add(Endfinally);
                instructions.Add(ldloc_3_finallyEnd);
                _ = instructions.Add(Ret);

                // Register the 'try/finally'
                currentMethod.CilMethodBody.ExceptionHandlers.Add(new CilExceptionHandler
                {
                    HandlerType = CilExceptionHandlerType.Finally,
                    TryStart = ldloc_2_tryStart.CreateLabel(),
                    TryEnd = ldloc_2_finallyStart.CreateLabel(),
                    HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                    HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                });
            }

            // Define the 'HasCurrent' method as follows:
            //
            // public static bool HasCurrent(WindowsRuntimeObjectReference thisReference)
            MethodDefinition hasCurrentMethod = new(
                name: "HasCurrent"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IIteratorMethodsHasCurrent.Import(module) },
                    { Ret }
                }
            };

            iteratorMethodsType.Methods.Add(hasCurrentMethod);

            // Define the 'MoveNext' method as follows:
            //
            // public static bool MoveNext(WindowsRuntimeObjectReference thisReference)
            MethodDefinition moveNextMethod = new(
                name: "MoveNext"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IIteratorMethodsMoveNext.Import(module) },
                    { Ret }
                }
            };

            iteratorMethodsType.Methods.Add(moveNextMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="iteratorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIteratorMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition iteratorMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            // The 'NativeObject' is deriving from 'WindowsRuntimeEnumerator<<ELEMENT_TYPE>, <IITERATOR_METHODS>>'
            TypeSignature windowsRuntimeEnumerator2Type = interopReferences.WindowsRuntimeEnumerator2.MakeGenericReferenceType(
                enumeratorType.TypeArguments[0],
                iteratorMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: enumeratorType,
                nativeObjectBaseType: windowsRuntimeEnumerator2Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="enumeratorType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature enumeratorType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: enumeratorType.FullName, // TODO
                typeSignature: enumeratorType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="enumeratorType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: enumeratorType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="enumeratorComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="enumeratorType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition enumeratorComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: enumeratorType,
                interfaceComWrappersCallbackType: enumeratorComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);

            // Track the type (it's needed by 'IEnumerable<T>')
            emitState.TrackTypeDefinition(marshallerType, enumeratorType, "Marshaller");
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="iteratorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIteratorMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition iteratorMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(enumeratorType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerator.Import(module)),
                    new InterfaceImplementation(interopReferences.IDisposable.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'IEnumerator<T>.Current' getter method
            MethodDefinition get_IEnumerator1CurrentMethod = new(
                name: $"System.Collections.Generic.IEnumerator<{elementType.FullName}>.get_Current",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(elementType.Import(module)));

            // Add and implement the 'IEnumerator<T>.Current' get accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumerator1get_Current(elementType).Import(module),
                method: get_IEnumerator1CurrentMethod);

            // Create a method body for the 'IEnumerator<T>.Current' property
            get_IEnumerator1CurrentMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: enumeratorType,
                implementationMethod: get_IEnumerator1CurrentMethod,
                forwardedMethod: iteratorMethodsType.GetMethod("Current"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'IEnumerator<T>.Current' property
            PropertyDefinition enumerator1CurrentProperty = new(
                name: $"System.Collections.Generic.IEnumerator<{elementType.FullName}>.Current",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_IEnumerator1CurrentMethod))
            {
                GetMethod = get_IEnumerator1CurrentMethod
            };

            interfaceImplType.Properties.Add(enumerator1CurrentProperty);

            // Create the 'IEnumerator.Current' getter method
            MethodDefinition get_IEnumeratorCurrentMethod = new(
                name: "System.Collections.IEnumerator.get_Current"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Object));

            // Add and implement the 'IEnumerator.Current' get accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumeratorget_Current.Import(module),
                method: get_IEnumeratorCurrentMethod);

            // Create a method body for the 'IEnumerator.Current' property
            get_IEnumeratorCurrentMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Callvirt, interopReferences.IEnumerator1get_Current(elementType).Import(module) },
                }
            };

            // If the element type is a value type, we need to box it
            if (elementType.IsValueType)
            {
                _ = get_IEnumeratorCurrentMethod.CilMethodBody.Instructions.Add(Box, elementType.Import(module).ToTypeDefOrRef());
            }

            // Add the return
            _ = get_IEnumeratorCurrentMethod.CilMethodBody.Instructions.Add(Ret);

            // Create the 'IEnumerator.Current' property
            PropertyDefinition enumeratorCurrentProperty = new(
                name: "System.Collections.IEnumerator.Current"u8,
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_IEnumeratorCurrentMethod))
            {
                GetMethod = get_IEnumeratorCurrentMethod
            };

            interfaceImplType.Properties.Add(enumeratorCurrentProperty);

            // Define the 'System.IEnumerator.MoveNext' method
            MethodDefinition moveNextMethod = new(
                name: "System.IEnumerator.MoveNext"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Boolean));

            // Add and implement the 'MoveNext' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumeratorMoveNext.Import(module),
                method: moveNextMethod);

            // Create a method body for the 'MoveNext' method
            moveNextMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: enumeratorType,
                implementationMethod: moveNextMethod,
                forwardedMethod: iteratorMethodsType.GetMethod("MoveNext"u8),
                interopReferences: interopReferences,
                module: module);

            // Define the 'System.IEnumerator.Reset' method
            MethodDefinition resetMethod = new(
                name: "System.IEnumerator.Reset"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void));

            // Add and implement the 'Reset' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IEnumeratorReset.Import(module),
                method: resetMethod);

            // Create a method body for the 'Reset' method
            resetMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Newobj, interopReferences.NotSupportedException_ctor.Import(module) },
                    { Throw }
                }
            };

            // Define the 'System.IDisposable.Dispose' method
            MethodDefinition disposeMethod = new(
                name: "System.IDisposable.Dispose"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void));

            // And and implement the 'Dispose' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IDisposableDispose.Import(module),
                method: disposeMethod);

            // Create a method body for the 'Dispose' method
            disposeMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions = { { Ret } }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature enumeratorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // Define the 'Current' method
            MethodDefinition currentMethod = InteropMethodDefinitionFactory.IEnumerator1Impl.Current(
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            // Define the 'get_HasCurrent' method
            MethodDefinition hasCurrentMethod = InteropMethodDefinitionFactory.IEnumerator1Impl.HasCurrentOrMoveNext(
                nameUtf8: "get_HasCurrent"u8,
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            // Define the 'MoveNext' method
            MethodDefinition moveNextMethod = InteropMethodDefinitionFactory.IEnumerator1Impl.HasCurrentOrMoveNext(
                nameUtf8: "MoveNext"u8,
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            // Define the 'GetMany' method
            MethodDefinition getManyMethod = InteropMethodDefinitionFactory.IEnumerator1Impl.GetMany(
                enumeratorType: enumeratorType,
                interopReferences: interopReferences,
                module: module);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "Impl"),
                vftblType: interopDefinitions.IEnumerator1Vftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [
                    currentMethod,
                    hasCurrentMethod,
                    moveNextMethod,
                    getManyMethod]);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, enumeratorType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="enumeratorComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition enumeratorComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IIterator`1<{enumeratorType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: enumeratorComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IIterator`1<{enumeratorType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: enumeratorType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: enumeratorType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}
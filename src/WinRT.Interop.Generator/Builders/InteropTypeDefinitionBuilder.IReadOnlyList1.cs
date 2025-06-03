// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    /// Helpers for <see cref="System.Collections.Generic.IReadOnlyList{T}"/> types.
    /// </summary>
    public static class IReadOnlyList1
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="readOnlyListType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature readOnlyListType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "IID"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

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

            // All reference types can share the same vtable type (as it just uses 'void*' for the ABI type)
            if (!elementType.IsValueType)
            {
                vftblType = interopDefinitions.IReadOnlyList1Vftbl;

                return;
            }

            // We can also share vtables for 'KeyValuePair<,>' types, as their ABI type is an interface
            if (elementType.IsKeyValuePairType(interopReferences))
            {
                vftblType = interopDefinitions.IReadOnlyList1Vftbl;

                return;
            }

            // Otherwise, we must construct a new specialized vtable type
            vftblType = WellKnownTypeDefinitionFactory.IReadOnlyList1Vftbl(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "Vftbl"),
                elementType: elementType, // TODO: use ABI type
                interopReferences: interopReferences,
                module: module);

            module.TopLevelTypes.Add(vftblType);
        }

        /// <summary>
        /// Creates a new type definition for the methods for an <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vectorViewMethodsType">The resulting methods type.</param>
        public static void IVectorViewMethods(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition vftblType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition vectorViewMethodsType)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            vectorViewMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "IVectorViewMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IVectorViewMethods1.MakeGenericInstanceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(vectorViewMethodsType);

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

            vectorViewMethodsType.Methods.Add(getAtMethod);

            // Mark the 'GetAt' method as overriding the base method
            vectorViewMethodsType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IVectorViewMethods1GetAt(elementType).Import(module),
                body: getAtMethod));

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: '<ABI_TYPE_ARGUMENT>' (the ABI type for the type argument)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true).Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_result = new(elementType.Import(module)); // TODO: use ABI type

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction nop_implementation = new(Nop);

            // Create a method body for the 'GetAt' method
            getAtMethod.CilMethodBody = new CilMethodBody(getAtMethod)
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_result },
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
                    { Ldarg_1 },
                    { Ldloca_S, loc_2_result },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, vftblType.GetField("GetAt"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IReadOnlyList1GetAtImpl(elementType, interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },
                    { nop_finallyEnd },

                    // Implementation to return the marshalled result
                    { nop_implementation }
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
            if (elementType.IsValueType) // TODO, share with all methods returning a value (eg. 'Current')
            {
                getAtMethod.CilMethodBody.Instructions.ReplaceRange(nop_implementation, [
                    new CilInstruction(Ldloc_2),
                    new CilInstruction(Ret)]);
            }
            else
            {
                // Declare an additional variable:
                //   [3]: '<TYPE_ARGUMENT>' (for the marshalled value)
                CilLocalVariable loc_3_current = new(elementType.Import(module));

                getAtMethod.CilMethodBody.LocalVariables.Add(loc_3_current);

                // Jump labels for the 'try/finally' blocks
                CilInstruction ldloc_2_tryStart = new(Ldloc_2);
                CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
                CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

                // We need to marshal the native value to a managed object
                getAtMethod.CilMethodBody.Instructions.ReplaceRange(nop_implementation, [
                    ldloc_2_tryStart,
                    new(Call, interopReferences.HStringMarshallerConvertToManaged.Import(module)),
                    new(Stloc_3),
                    new(Leave_S, ldloc_3_finallyEnd.CreateLabel()),
                    ldloc_2_finallyStart,
                    new(Call, interopReferences.HStringMarshallerFree.Import(module)),
                    new(Endfinally),
                    ldloc_3_finallyEnd,
                    new(Ret)]);

                // Register the 'try/finally'
                getAtMethod.CilMethodBody.ExceptionHandlers.Add(new CilExceptionHandler
                {
                    HandlerType = CilExceptionHandlerType.Finally,
                    TryStart = ldloc_2_tryStart.CreateLabel(),
                    TryEnd = ldloc_2_finallyStart.CreateLabel(),
                    HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                    HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                });
            }
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
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(readOnlyListMethodsType);

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

            readOnlyListMethodsType.Methods.Add(get_ItemMethod);

            // Create a method body for the 'Item' method
            get_ItemMethod.CilMethodBody = new CilMethodBody(get_ItemMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, interopReferences.IReadOnlyListMethods1get_Item(elementType, vectorViewMethodsType).Import(module) },
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

            readOnlyListMethodsType.Methods.Add(countMethod);

            // Create a method body for the 'Count' method
            countMethod.CilMethodBody = new CilMethodBody(countMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IReadOnlyListMethodsCount.Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="readOnlyListMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IReadOnlyListMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition readOnlyListMethodsType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = readOnlyListType.TypeArguments[0];
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericInstanceType(elementType);

            // The 'NativeObject' is deriving from 'WindowsRuntimeReadOnlyList<<ELEMENT_TYPE>, <IENUMERABLE_INTERFACE>, <IITERABLE_METHODS, <IREADONLYLIST_METHODS>>'
            TypeSignature windowsRuntimeReadOnlyList4Type = interopReferences.WindowsRuntimeReadOnlyList4.MakeGenericInstanceType(
                elementType,
                emitState.LookupTypeDefinition(enumerableType, "Interface").ToTypeSignature(isValueType: false),
                emitState.LookupTypeDefinition(enumerableType, "IIterableMethods").ToTypeSignature(isValueType: false),
                readOnlyListMethodsType.ToTypeSignature(isValueType: false));

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
            InteropTypeDefinitionBuilder.ComWrappersCallbackType(
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
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition readOnlyListComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "Marshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Prepare the external types we need in the implemented methods
            TypeSignature readOnlyListType2 = readOnlyListType.Import(module);
            TypeSignature windowsRuntimeObjectReferenceValueType = interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToTypeSignature(isValueType: true);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<READONLYLIST_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: windowsRuntimeObjectReferenceValueType,
                    parameterTypes: [readOnlyListType2]));

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Reference the instantiated 'ConvertToUnmanaged' method for the marshaller
            MethodSpecification windowsRuntimeInterfaceMarshallerConvertToUnmanaged =
                interopReferences.WindowsRuntimeInterfaceMarshallerConvertToUnmanaged
                .MakeGenericInstanceMethod(readOnlyListType);

            // Create a method body for the 'ConvertToUnmanaged' method
            convertToUnmanagedMethod.CilMethodBody = new CilMethodBody(convertToUnmanagedMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, get_IidMethod },
                    { Call, windowsRuntimeInterfaceMarshallerConvertToUnmanaged.Import(module) },
                    { Ret }
                }
            };

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <READONLYLIST_TYPE> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: readOnlyListType2,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            marshallerType.Methods.Add(convertToManagedMethod);

            // Construct a descriptor for 'WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<<READONLYLIST_CALLBACK_TYPE>>(void*)'
            IMethodDescriptor windowsRuntimeUnsealedObjectMarshallerConvertToManaged =
                interopReferences.WindowsRuntimeUnsealedObjectMarshallerConvertToManaged
                .Import(module)
                .MakeGenericInstanceMethod(readOnlyListComWrappersCallbackType.ToTypeSignature(isValueType: false));

            // Create a method body for the 'ConvertToManaged' method
            convertToManagedMethod.CilMethodBody = new CilMethodBody(convertToManagedMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, windowsRuntimeUnsealedObjectMarshallerConvertToManaged },
                    { Ret }
                }
            };
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
            TypeSignature readOnlyCollectionType = interopReferences.IReadOnlyCollection1.MakeGenericInstanceType(elementType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericInstanceType(elementType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(readOnlyListType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(readOnlyCollectionType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(enumerableType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'get_Item' getter method
            MethodDefinition get_ItemMethod = new(
                name: $"System.Collections.Generic.IReadOnlyList<{elementType.FullName}>.get_Item",
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(elementType.Import(module), module.CorLibTypeFactory.Int32));

            interfaceImplType.Methods.Add(get_ItemMethod);

            // Mark the 'get_Item' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IReadOnlyList1get_Item(elementType).Import(module),
                body: get_ItemMethod));

            // Create a body for the 'get_Item' method
            get_ItemMethod.CilMethodBody = new CilMethodBody(get_ItemMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                    { Ldtoken, readOnlyListType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Ldarg_1 },
                    { Call, readOnlyListMethodsType.GetMethod("Item"u8) },
                    { Ret }
                }
            };

            // Create the 'Item' property
            PropertyDefinition itemProperty = new(
                name: $"System.Collections.Generic.IReadOnlyList<{elementType.FullName}>.Item",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.CreateInstance(elementType.Import(module), module.CorLibTypeFactory.Int32))
            { GetMethod = get_ItemMethod };

            interfaceImplType.Properties.Add(itemProperty);

            // Create the 'get_Count' getter method
            MethodDefinition get_CountMethod = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<{elementType.FullName}>.get_Count",
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Int32));

            interfaceImplType.Methods.Add(get_CountMethod);

            // Mark the 'get_Count' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IReadOnlyCollection1get_Count(elementType).Import(module),
                body: get_CountMethod));

            // Create a body for the 'get_Item' method
            get_CountMethod.CilMethodBody = new CilMethodBody(get_CountMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                    { Ldtoken, readOnlyListType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Call, readOnlyListMethodsType.GetMethod("Count"u8) },
                    { Ret }
                }
            };

            // Create the 'Count' property
            PropertyDefinition countProperty = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<{elementType.FullName}>.Count",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.CreateInstance(module.CorLibTypeFactory.Int32))
            { GetMethod = get_CountMethod };

            interfaceImplType.Properties.Add(countProperty);

            // Create the 'IEnumerable<T>.GetEnumerator' method
            MethodDefinition enumerable1GetEnumeratorMethod = new(
                name: $"System.Collections.Generic.IEnumerable<{elementType.FullName}>.GetEnumerator",
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module)));

            interfaceImplType.Methods.Add(enumerable1GetEnumeratorMethod);

            // Mark the 'IEnumerable<T>.GetEnumerator' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumerable1GetEnumerator(elementType).Import(module),
                body: enumerable1GetEnumeratorMethod));

            // Create a method body for the 'IEnumerable<T>.GetEnumerator' method
            enumerable1GetEnumeratorMethod.CilMethodBody = new CilMethodBody(enumerable1GetEnumeratorMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Castclass, interopReferences.WindowsRuntimeObject.Import(module) },
                    { Ldtoken, enumerableType.Import(module).ToTypeDefOrRef() },
                    { Call, interopReferences.TypeGetTypeFromHandle.Import(module) },
                    { Callvirt, interopReferences.Typeget_TypeHandle.Import(module) },
                    { Callvirt, interopReferences.WindowsRuntimeObjectGetObjectReferenceForInterface.Import(module) },
                    { Call, emitState.LookupTypeDefinition(enumerableType, "IEnumerableMethods").GetMethod("GetEnumerator"u8) },
                    { Ret }
                }
            };

            // Create the 'IEnumerable.GetEnumerator' method
            MethodDefinition enumerableGetEnumeratorMethod = new(
                name: "System.Collections.IEnumerable.GetEnumerator"u8,
                attributes: MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(interopReferences.IEnumerator.Import(module).ToTypeSignature(isValueType: false)));

            interfaceImplType.Methods.Add(enumerableGetEnumeratorMethod);

            // Mark the 'IEnumerable.GetEnumerator' method as implementing the interface method
            interfaceImplType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.IEnumerableGetEnumerator.Import(module),
                body: enumerableGetEnumeratorMethod));

            // Create a method body for the 'IEnumerable.GetEnumerator' method
            enumerableGetEnumeratorMethod.CilMethodBody = new CilMethodBody(enumerableGetEnumeratorMethod)
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
        /// Creates a new type definition for the implementation of the vtable for some <c>IVectorView&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="readOnlyListType">The <see cref="GenericInstanceTypeSignature"/> for the <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="readOnlyListType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature readOnlyListType,
            TypeDefinition vftblType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // We're declaring an 'internal static class' type
            implType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType, "Impl"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(implType);

            // The vtable field looks like this:
            //
            // [FixedAddressValueType]
            // private static readonly <VTABLE_TYPE> Vftbl;
            FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, vftblType.ToTypeSignature(isValueType: true))
            {
                CustomAttributes = { new CustomAttribute(interopReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
            };

            implType.Fields.Add(vftblField);

            // TODO

            // Create the static constructor to initialize the vtable
            MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

            // Initialize the enumerator vtable
            cctor.CilMethodBody = new CilMethodBody(cctor)
            {
                Instructions =
                {
                    { Ldsflda, vftblField },
                    { Conv_U },
                    { Call, interopReferences.IInspectableImplget_Vtable.Import(module) },
                    { Ldobj, interopDefinitions.IInspectableVftbl },
                    { Stobj, interopDefinitions.IInspectableVftbl },
                    { Ret }
                }
            };

            // Create the public 'IID' property
            WellKnownMemberDefinitionFactory.IID(
                forwardedIidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out MethodDefinition get_IidMethod2,
                out PropertyDefinition iidProperty);

            implType.Methods.Add(get_IidMethod2);
            implType.Properties.Add(iidProperty);

            // Create the 'Vtable' property
            WellKnownMemberDefinitionFactory.Vtable(
                vftblField: vftblField,
                corLibTypeFactory: module.CorLibTypeFactory,
                out PropertyDefinition vtableProperty,
                out MethodDefinition get_VtableMethod);

            implType.Properties.Add(vtableProperty);
            implType.Methods.Add(get_VtableMethod);
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

            ProxyType(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyListType),
                name: InteropUtf8NameFactory.TypeName(readOnlyListType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: readOnlyListComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }
    }
}

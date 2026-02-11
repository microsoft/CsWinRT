// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Resolvers;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

#pragma warning disable IDE0008, IDE0042

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
        /// The thread-local list to build COM interface entries.
        /// </summary>
        [ThreadStatic]
        private static List<InteropInterfaceEntryInfo>? entriesList;

        /// <summary>
        /// Creates a new type definition for the marshaller for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            TypeSignature elementType = arrayType.BaseType;

            // Emit the right marshaller based on the element type. We special case all different
            // kinds of element types because some need specialized marshallers, and some need
            // a generated element marshaller type. The marshaller itself just forwards all calls.
            if (elementType.IsBlittable(interopReferences))
            {
                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.BlittableValueType(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else if (elementType.IsConstructedKeyValuePairType(interopReferences))
            {
                TypeDefinition elementMarshallerType = InteropTypeDefinitionFactory.SzArrayElementMarshaller.KeyValuePair(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module);

                module.TopLevelTypes.Add(elementMarshallerType);

                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.KeyValuePair(
                    arrayType: arrayType,
                    elementMarshallerType: elementMarshallerType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else if (elementType.IsManagedValueType(interopReferences))
            {
                TypeDefinition elementMarshallerType = InteropTypeDefinitionFactory.SzArrayElementMarshaller.ManagedValueType(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module);

                module.TopLevelTypes.Add(elementMarshallerType);

                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.ManagedValueType(
                    arrayType: arrayType,
                    elementMarshallerType: elementMarshallerType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else if (elementType.IsValueType)
            {
                TypeDefinition elementMarshallerType = InteropTypeDefinitionFactory.SzArrayElementMarshaller.UnmanagedValueType(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module);

                module.TopLevelTypes.Add(elementMarshallerType);

                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.UnmanagedValueType(
                    arrayType: arrayType,
                    elementMarshallerType: elementMarshallerType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else if (elementType.IsTypeOfObject())
            {
                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.Object(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else if (elementType.IsTypeOfString())
            {
                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.String(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else if (elementType.IsTypeOfType(interopReferences))
            {
                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.Type(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else if (elementType.IsTypeOfException(interopReferences))
            {
                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.Exception(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
            else
            {
                TypeDefinition elementMarshallerType = InteropTypeDefinitionFactory.SzArrayElementMarshaller.ReferenceType(
                    arrayType: arrayType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module);

                module.TopLevelTypes.Add(elementMarshallerType);

                marshallerType = InteropTypeDefinitionFactory.SzArrayMarshaller.ReferenceType(
                    arrayType: arrayType,
                    elementMarshallerType: elementMarshallerType,
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(marshallerType);
            }
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeArrayComWrappersCallback</c> interface for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="marshallerType">The type returned by <see cref="Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallback(
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
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeArrayComWrappersCallback) }
            };

            module.TopLevelTypes.Add(callbackType);

            // Define the 'CreateArray' method as follows:
            //
            // public static Array CreateArray(uint count, void* value)
            MethodDefinition createArrayMethod = new(
                name: "CreateArray"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Array.ToReferenceTypeSignature(),
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
                declaration: interopReferences.IWindowsRuntimeArrayComWrappersCallbackCreateArray,
                method: createArrayMethod);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for the 'IReferenceArray`1&lt;T&gt;' instantiation for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Marshaller"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ArrayImpl(
            SzArrayTypeSignature arrayType,
            TypeDefinition marshallerType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // Define the 'get_Value' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Value(void* thisPtr, uint* size, <ABI_ELEMENT_TYPE>** result)
            MethodDefinition valueMethod = new(
                name: "get_Value"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32.MakePointerType(),
                        arrayType.BaseType.GetAbiType(interopReferences).MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences) }
            };

            // Jump labels
            CilInstruction ldc_i4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged);
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);

            // Declare 2 local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'WindowsRuntimeObjectReferenceValue' to use to marshal the delegate
            CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);
            CilLocalVariable loc_1_referenceValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature());

            // Create a method body for the 'get_Value' method
            valueMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_hresult, loc_1_referenceValue },
                Instructions =
                {
                    // Return 'E_POINTER' if either argument is 'null'
                    { Ldarg_1 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Beq_S, ldc_i4_e_pointer.CreateLabel() },
                    { Ldarg_2 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { ldc_i4_e_pointer },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(arrayType) },
                    { Newobj, interopReferences.ReadOnlySpan1_ctor(arrayType) },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, marshallerType.GetMethod("ConvertToUnmanaged"u8) },
                    { Ldc_I4_0 },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // 'catch' code
                    { call_catchStartMarshalException },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [0]
                    { ldloc_0_returnHResult },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Exception,
                        TryStart = ldarg_0_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception
                    }
                }
            };

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "Impl"),
                vftblType: interopDefinitions.IReferenceArrayVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [valueMethod]);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="vtableTypes">The vtable types implemented by <paramref name="arrayType"/>.</param>
        /// <param name="implType">The <see cref="TypeDefinition"/> instance returned by <see cref="Impl"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IReferenceArray`1&lt;T&gt;' interface.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="interfaceEntriesType">The resulting interface entries type.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            SzArrayTypeSignature arrayType,
            TypeSignatureEquatableSet vtableTypes,
            TypeDefinition implType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition interfaceEntriesType,
            out TypeDefinition interfaceEntriesImplType)
        {
            // Reuse the same list, to minimize allocations (same as for user-defined types)
            List<InteropInterfaceEntryInfo> entriesList = SzArray.entriesList ??= [];

            // It's not guaranteed that the list is empty, so we must always reset it first
            entriesList.Clear();

            // Add the entry for the 'IReferenceArray<T>' implementation first
            entriesList.Add(InteropInterfaceEntriesResolver.Create(get_IidMethod, implType.GetMethod("get_Vtable"u8)));

            // Add all entries for explicitly implemented interfaces
            entriesList.AddRange(InteropInterfaceEntriesResolver.EnumerateMetadataInterfaceEntries(
                vtableTypes: vtableTypes,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                emitState: emitState,
                useWindowsUIXamlProjections: useWindowsUIXamlProjections));

            var propertyValueImpl = InteropImplTypeResolver.GetSzArrayTypeImpl(arrayType, interopReferences);

            // Add the entry for the 'IPropertyValue' implementation as well
            entriesList.Add(InteropInterfaceEntriesResolver.Create(propertyValueImpl.get_IID, propertyValueImpl.get_Vtable));

            // Add the built-in native interfaces at the end
            entriesList.AddRange(InteropInterfaceEntriesResolver.EnumerateNativeInterfaceEntries(
                vtableTypes: vtableTypes,
                interopReferences: interopReferences));

            // Get or create the interface entries type for this SZ array type (we reuse them based on number of entries)
            interfaceEntriesType = interopDefinitions.SzArrayInterfaceEntries(entriesList.Count);

            InteropTypeDefinitionBuilder.InterfaceEntriesImpl(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "InterfaceEntriesImpl"),
                entriesFieldType: interfaceEntriesType,
                interopReferences: interopReferences,
                module: module,
                implType: out interfaceEntriesImplType,
                implTypes: CollectionsMarshal.AsSpan(entriesList));
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="arrayInterfaceEntriesType">The <see cref="TypeDefinition"/> for the interface entries type returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="arrayInterfaceEntriesImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="arrayComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallback"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IReferenceArray`1&lt;T&gt;' interface.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            SzArrayTypeSignature arrayType,
            TypeDefinition arrayInterfaceEntriesType,
            TypeDefinition arrayInterfaceEntriesImplType,
            TypeDefinition arrayComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute);

            module.TopLevelTypes.Add(marshallerType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateDefaultConstructor(module, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor);

            marshallerType.Methods.Add(ctor);

            // Determine which 'CreateComInterfaceFlags' flags we use for the marshalled CCW
            CreateComInterfaceFlags flags = arrayType.IsTrackerSupportRequired(interopReferences)
                ? CreateComInterfaceFlags.TrackerSupport
                : CreateComInterfaceFlags.None;

            // Define the 'GetOrCreateComInterfaceForObject' method as follows:
            //
            // public static void* GetOrCreateComInterfaceForObject(object value)
            MethodDefinition getOrCreateComInterfaceForObjectMethod = new(
                name: "GetOrCreateComInterfaceForObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void.MakePointerType(),
                    parameterTypes: [module.CorLibTypeFactory.Object]))
            {
                CilInstructions =
                {
                    { Ldarg_1 },
                    { CilInstruction.CreateLdcI4((int)flags) },
                    { Call, interopReferences.WindowsRuntimeComWrappersMarshalGetOrCreateComInterfaceForObject },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(getOrCreateComInterfaceForObjectMethod);

            // Define the 'ComputeVtables' method as follows:
            //
            // public static ComInterfaceEntry* ComputeVtables(out int count)
            MethodDefinition computeVtablesMethod = new(
                name: "ComputeVtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.ComInterfaceEntry.MakePointerType(),
                    parameterTypes: [module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
            {
                CilOutParameterIndices = [1],
                CilInstructions =
                {
                    { Ldarg_1 },
                    { CilInstruction.CreateLdcI4(arrayInterfaceEntriesType.Fields.Count) },
                    { Stind_I4 },
                    { Call, arrayInterfaceEntriesImplType.GetMethod("get_Vtables"u8) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(computeVtablesMethod);

            // Import the 'UnboxToManaged<TCallback>' method for the array
            IMethodDescriptor windowsRuntimeArrayMarshallerUnboxToManagedDescriptor =
                interopReferences.WindowsRuntimeArrayMarshallerUnboxToManaged
                .MakeGenericInstanceMethod(arrayComWrappersCallbackType.ToReferenceTypeSignature());

            // Define the 'CreateObject' method as follows:
            //
            // public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
            MethodDefinition createObjectMethod = new(
                name: "CreateObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Object,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.CreatedWrapperFlags.MakeByReferenceType()]))
            {
                CilOutParameterIndices = [2],
                CilInstructions =
                {
                    // Set the 'wrapperFlags' to 'CreatedWrapperFlags.NonWrapping'
                    { Ldarg_2 },
                    { CilInstruction.CreateLdcI4((int)CreatedWrapperFlags.NonWrapping) },
                    { Stind_I4 },

                    // Forward to 'WindowsRuntimeArrayMarshaller'
                    { Ldarg_1 },
                    { Call, get_IidMethod },
                    { Call, windowsRuntimeArrayMarshallerUnboxToManagedDescriptor },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(createObjectMethod);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="comWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance for the marshaller attribute type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            SzArrayTypeSignature arrayType,
            TypeDefinition comWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition proxyType)
        {
            // This is a proxy for Windows Runtime arrays, so we also need to emit the '[WindowsRuntimeMappedMetadata]'
            // attribute, so that during 'TypeName' marshalling we can detect whether the type is a metadata type. Note
            // that arrays with element types that are not Windows Runtime types will still have entries in the marshalling
            // type map (as they're treated the same as normal user-defined types), so this allows us to distinguish them.
            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType),
                mappedMetadata: "Windows.Foundation.FoundationContract",
                runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(arrayType, useWindowsUIXamlProjections),
                metadataTypeName: null,
                mappedType: arrayType,
                referenceType: null,
                comWrappersMarshallerAttributeType: comWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                proxyType: out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="Proxy"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            SzArrayTypeSignature arrayType,
            TypeDefinition proxyType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(arrayType, useWindowsUIXamlProjections),
                metadataTypeName: null,
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: arrayType,
                marshallingTypeMapSourceType: arrayType,
                marshallingTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                metadataTypeMapSourceType: null,
                metadataTypeMapProxyType: null,
                interfaceTypeMapSourceType: null,
                interfaceTypeMapProxyType: null,
                interopReferences: interopReferences,
                module: module);
        }
    }
}
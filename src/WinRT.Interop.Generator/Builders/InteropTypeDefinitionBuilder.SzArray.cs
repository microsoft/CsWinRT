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
using WindowsRuntime.InteropGenerator.Helpers;
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
                        arrayType.BaseType.GetAbiType(interopReferences).Import(module).MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Jump labels
            CilInstruction ldc_i4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);

            // Declare 2 local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'WindowsRuntimeObjectReferenceValue' to use to marshal the delegate
            CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);
            CilLocalVariable loc_1_referenceValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature().Import(module));

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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(arrayType).Import(module) },
                    { Newobj, interopReferences.ReadOnlySpan1_ctor(arrayType).Import(module) },
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
                        ExceptionType = interopReferences.Exception.Import(module)
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
        /// <param name="implType">The <see cref="TypeDefinition"/> instance returned by <see cref="Impl"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IReferenceArray`1&lt;T&gt;' interface.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            SzArrayTypeSignature arrayType,
            TypeDefinition implType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition interfaceEntriesImplType)
        {
            var listImpl = InteropImplTypeResolver.GetCustomMappedOrManuallyProjectedTypeImpl(
                type: interopReferences.IList.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                useWindowsUIXamlProjections: useWindowsUIXamlProjections);

            var enumerableImpl = InteropImplTypeResolver.GetCustomMappedOrManuallyProjectedTypeImpl(
                type: interopReferences.IEnumerable.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                useWindowsUIXamlProjections: useWindowsUIXamlProjections);

            var list1Impl = InteropImplTypeResolver.GetGenericInstanceTypeImpl(
                type: interopReferences.IList1.MakeGenericReferenceType(arrayType.BaseType),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                emitState: emitState);

            var enumerable1Impl = InteropImplTypeResolver.GetGenericInstanceTypeImpl(
                type: interopReferences.IEnumerable1.MakeGenericReferenceType(arrayType.BaseType),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                emitState: emitState);

            var readOnlyList1Impl = InteropImplTypeResolver.GetGenericInstanceTypeImpl(
                type: interopReferences.IReadOnlyList1.MakeGenericReferenceType(arrayType.BaseType),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                emitState: emitState);

            var propertyValueImpl = InteropImplTypeResolver.GetSzArrayTypeImpl(arrayType, interopReferences);

            InteropTypeDefinitionBuilder.InterfaceEntriesImpl(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "InterfaceEntriesImpl"),
                entriesFieldType: interopDefinitions.IReferenceArrayInterfaceEntries,
                interopReferences: interopReferences,
                module: module,
                implType: out interfaceEntriesImplType,
                implTypes: [
                    (get_IidMethod, implType.GetMethod("get_Vtable"u8)),
                    (listImpl.get_IID, listImpl.get_Vtable),
                    (enumerableImpl.get_IID, enumerableImpl.get_Vtable),
                    (list1Impl.get_IID, list1Impl.get_Vtable),
                    (enumerable1Impl.get_IID, enumerable1Impl.get_Vtable),
                    (readOnlyList1Impl.get_IID, readOnlyList1Impl.get_Vtable),
                    (propertyValueImpl.get_IID, propertyValueImpl.get_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IStringable, interopReferences.IStringableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IWeakReferenceSource, interopReferences.IWeakReferenceSourceImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IMarshal, interopReferences.IMarshalImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IAgileObject, interopReferences.IAgileObjectImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IInspectable, interopReferences.IInspectableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IUnknown, interopReferences.IUnknownImplget_Vtable)]);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="arrayInterfaceEntriesImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="arrayComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallback"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IReferenceArray`1&lt;T&gt;' interface.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            SzArrayTypeSignature arrayType,
            TypeDefinition arrayInterfaceEntriesImplType,
            TypeDefinition arrayComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute.Import(module));

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
                    { Call, interopReferences.WindowsRuntimeComWrappersMarshalGetOrCreateComInterfaceForObject.Import(module) },
                    { Ret }
                }
            };

            // Add and implement the 'GetOrCreateComInterfaceForObject' method
            marshallerType.Methods.Add(getOrCreateComInterfaceForObjectMethod);

            // Define the 'ComputeVtables' method as follows:
            //
            // public static ComInterfaceEntry* ComputeVtables(out int count)
            MethodDefinition computeVtablesMethod = new(
                name: "ComputeVtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.ComInterfaceEntry.Import(module).MakePointerType(),
                    parameterTypes: [module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
            {
                CilOutParameterIndices = [1],
                CilInstructions =
                {
                    { Ldarg_1 },
                    { CilInstruction.CreateLdcI4(interopDefinitions.IReferenceArrayInterfaceEntries.Fields.Count) },
                    { Stind_I4 },
                    { Call, arrayInterfaceEntriesImplType.GetMethod("get_Vtables"u8) },
                    { Ret }
                }
            };

            // Add and implement the 'ComputeVtables' method
            marshallerType.Methods.Add(computeVtablesMethod);

            // Import the 'UnboxToManaged<TCallback>' method for the array
            IMethodDescriptor windowsRuntimeArrayMarshallerUnboxToManagedDescriptor = interopReferences.WindowsRuntimeArrayMarshallerUnboxToManaged
                .Import(module)
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
                        interopReferences.CreatedWrapperFlags.Import(module).MakeByReferenceType()]))
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

            // Add and implement the 'CreateObject' method
            marshallerType.Methods.Add(createObjectMethod);
        }

        /// <summary>
        /// Creates the type map attributes for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="Proxy(TypeSignature, TypeDefinition, InteropReferences, ModuleDefinition, bool, out TypeDefinition)"/>.</param>
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
                proxyTypeMapSourceType: arrayType,
                proxyTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                interfaceTypeMapSourceType: null,
                interfaceTypeMapProxyType: null,
                interopReferences: interopReferences,
                module: module);
        }
    }
}
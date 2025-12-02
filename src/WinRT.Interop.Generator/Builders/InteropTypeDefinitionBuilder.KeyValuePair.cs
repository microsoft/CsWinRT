// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using AsmResolver;
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
    /// Helpers for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class KeyValuePair
    {
        /// <summary>
        /// Creates a new type definition for the <c>KeyValuePairMethods</c> type to contain shared accessor
        /// methods for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="methodsType">The resulting methods type.</param>
        public static void Methods(
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition methodsType)
        {
            // We're declaring an 'internal static class' type
            methodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(interopReferences.KeyValuePair.ToReferenceTypeSignature()),
                name: InteropUtf8NameFactory.TypeName(interopReferences.KeyValuePair.ToReferenceTypeSignature(), "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(methodsType);
        }

        /// <summary>
        /// Gets or creates the accessor methods for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="methodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="keyAccessorMethod">The resulting accessor method for the key.</param>
        /// <param name="valueAccessorMethod">The resulting accessor method for the value.</param>
        public static void Accessors(
            GenericInstanceTypeSignature keyValuePairType,
            TypeDefinition methodsType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out MethodDefinition keyAccessorMethod,
            out MethodDefinition valueAccessorMethod)
        {
            TypeSignature keyType = keyValuePairType.TypeArguments[0];
            TypeSignature valueType = keyValuePairType.TypeArguments[1];

            // Prepare the names of the accessor methods, to define or look them up
            Utf8String get_KeyMethodName = $"get_Key({InteropUtf8NameFactory.TypeName(keyType)})";
            Utf8String get_ValueMethodName = $"get_Value({InteropUtf8NameFactory.TypeName(valueType)})";

            // Get or define the 'get_Key' accessor method
            if (!methodsType.TryGetMethod(get_KeyMethodName, out keyAccessorMethod!))
            {
                keyAccessorMethod = InteropMethodDefinitionFactory.KeyValuePairMethods.get_KeyOrValue(
                    keyValuePairType: keyValuePairType,
                    keyOrValueType: keyType,
                    vftblType: interopDefinitions.IKeyValuePairVftbl,
                    vftblMethodName: "get_Key"u8,
                    accessorMethodName: get_KeyMethodName,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module);

                methodsType.Methods.Add(keyAccessorMethod);
            }

            // Same thing for the 'get_Value' accessor method
            if (!methodsType.TryGetMethod(get_ValueMethodName, out valueAccessorMethod!))
            {
                valueAccessorMethod = InteropMethodDefinitionFactory.KeyValuePairMethods.get_KeyOrValue(
                    keyValuePairType: keyValuePairType,
                    keyOrValueType: valueType,
                    vftblType: interopDefinitions.IKeyValuePairVftbl,
                    vftblMethodName: "get_Value"u8,
                    accessorMethodName: get_ValueMethodName,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module);

                methodsType.Methods.Add(valueAccessorMethod);
            }
        }

        /// <summary>
        /// Creates a new type definition for the marshaller for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="keyValuePairType"/>.</param>
        /// <param name="keyAccessorMethod">The accessor method for the key.</param>
        /// <param name="valueAccessorMethod">The accessor method for the value.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            TypeSignature keyValuePairType,
            MethodDefinition get_IidMethod,
            MethodDefinition keyAccessorMethod,
            MethodDefinition valueAccessorMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(keyValuePairType),
                name: InteropUtf8NameFactory.TypeName(keyValuePairType, "Marshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Track the type (it may be needed to marshal parameters or return values)
            emitState.TrackTypeDefinition(marshallerType, keyValuePairType, "Marshaller");

            // Determine which 'CreateComInterfaceFlags' flags we use for the marshalled CCW
            CreateComInterfaceFlags flags = keyValuePairType.IsTrackerSupportRequired(interopReferences)
                ? CreateComInterfaceFlags.TrackerSupport
                : CreateComInterfaceFlags.None;

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<KEY_VALUE_PAIR_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                    parameterTypes: [keyValuePairType]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Box, keyValuePairType.ToTypeDefOrRef() },
                    { CilInstruction.CreateLdcI4((int)flags) },
                    { Call, get_IidMethod },
                    { Call, interopReferences.WindowsRuntimeValueTypeMarshallerConvertToUnmanagedUnsafe },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Declare the local variables:
            //   [0]: '<KEY_VALUE_PAIR_TYPE>' (for the failure path, initialized to 'default')
            CilLocalVariable loc_0_default = new(keyValuePairType);

            // Jump labels
            CilInstruction ldarg_0_marshal = new(Ldarg_0);

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <KEY_VALUE_PAIR_TYPE> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: keyValuePairType,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CilLocalVariables = { loc_0_default },
                CilInstructions =
                {
                    // if (value is null)
                    { Ldarg_0 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, ldarg_0_marshal.CreateLabel() },

                    // return default
                    { Ldloca_S, loc_0_default },
                    { Initobj, keyValuePairType.ToTypeDefOrRef() },
                    { Ldloc_0 },
                    { Ret },

                    // Marshal the 'KeyValuePair<,>' value
                    { ldarg_0_marshal },
                    { Call, keyAccessorMethod },
                    { Ldarg_0 },
                    { Call, valueAccessorMethod },
                    { Newobj, interopReferences.KeyValuePair2_ctor(keyValuePairType) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(convertToManagedMethod);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature keyValuePairType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // Define the 'get_Key' method
            MethodDefinition get_KeyMethod = InteropMethodDefinitionFactory.IKeyValuePair2Impl.get_Key(
                keyValuePairType: keyValuePairType,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);

            // Define the 'get_Value' method
            MethodDefinition get_ValueMethod = InteropMethodDefinitionFactory.IKeyValuePair2Impl.get_Value(
                keyValuePairType: keyValuePairType,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(keyValuePairType),
                name: InteropUtf8NameFactory.TypeName(keyValuePairType, "Impl"),
                vftblType: interopDefinitions.IKeyValuePairVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [get_KeyMethod, get_ValueMethod]);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="keyValuePairTypeImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ImplType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void InterfaceEntriesImplType(
            GenericInstanceTypeSignature keyValuePairType,
            TypeDefinition keyValuePairTypeImplType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            InterfaceEntriesImpl(
                ns: InteropUtf8NameFactory.TypeNamespace(keyValuePairType),
                name: InteropUtf8NameFactory.TypeName(keyValuePairType, "InterfaceEntriesImpl"),
                entriesFieldType: interopDefinitions.IKeyValuePairInterfaceEntries,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                implTypes: [
                    (get_IidMethod, keyValuePairTypeImplType.GetMethod("get_Vtable"u8)),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IStringable, interopReferences.IStringableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IWeakReferenceSource, interopReferences.IWeakReferenceSourceImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IMarshal, interopReferences.IMarshalImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IAgileObject, interopReferences.IAgileObjectImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IInspectable, interopReferences.IInspectableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IUnknown, interopReferences.IUnknownImplget_Vtable)]);
        }
    }
}
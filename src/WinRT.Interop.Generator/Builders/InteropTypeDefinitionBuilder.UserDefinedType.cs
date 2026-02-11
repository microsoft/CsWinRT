// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Resolvers;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for user-defined types (exposed as CCWs).
    /// </summary>
    public static class UserDefinedType
    {
        /// <summary>
        /// The thread-local list to build COM interface entries.
        /// </summary>
        [ThreadStatic]
        private static List<InteropInterfaceEntryInfo>? entriesList;

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for a user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="vtableTypes">The vtable types implemented by <paramref name="userDefinedType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="interfaceEntriesType">The resulting interface entries type.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            TypeSignature userDefinedType,
            TypeSignatureEquatableSet vtableTypes,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition interfaceEntriesType,
            out TypeDefinition interfaceEntriesImplType)
        {
            // Reuse the same list, to minimize allocations (since we always need to build the entries at runtime here)
            List<InteropInterfaceEntryInfo> entriesList = UserDefinedType.entriesList ??= [];

            // It's not guaranteed that the list is empty, so we must always reset it first
            entriesList.Clear();

            // Add all entries for explicitly implemented interfaces
            entriesList.AddRange(InteropInterfaceEntriesResolver.EnumerateMetadataInterfaceEntries(
                vtableTypes: vtableTypes,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                emitState: emitState,
                useWindowsUIXamlProjections: useWindowsUIXamlProjections));

            // Add the built-in native interfaces at the end
            entriesList.AddRange(InteropInterfaceEntriesResolver.EnumerateNativeInterfaceEntries(
                vtableTypes: vtableTypes,
                interopReferences: interopReferences));

            // Get or create the interface entries type for this user-defined type (we reuse them based on number of entries)
            interfaceEntriesType = interopDefinitions.UserDefinedInterfaceEntries(entriesList.Count);

            InteropTypeDefinitionBuilder.InterfaceEntriesImpl(
                ns: "WindowsRuntime.Interop.UserDefinedTypes"u8,
                name: InteropUtf8NameFactory.TypeName(userDefinedType, "InterfaceEntriesImpl"),
                entriesFieldType: interfaceEntriesType,
                interopReferences: interopReferences,
                module: module,
                implType: out interfaceEntriesImplType,
                implTypes: CollectionsMarshal.AsSpan(entriesList));
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="interfaceEntriesType">The <see cref="TypeDefinition"/> for the interface entries type returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="interfaceEntriesImplType">The <see cref="TypeDefinition"/> for the interface entries implementation type returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            TypeSignature userDefinedType,
            TypeDefinition interfaceEntriesType,
            TypeDefinition interfaceEntriesImplType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: "WindowsRuntime.Interop.UserDefinedTypes"u8,
                name: InteropUtf8NameFactory.TypeName(userDefinedType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute);

            module.TopLevelTypes.Add(marshallerType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateDefaultConstructor(module, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor);

            marshallerType.Methods.Add(ctor);

            // The 'ComputeVtables' method returns the 'ComWrappers.ComInterfaceEntry*' type
            PointerTypeSignature computeVtablesReturnType = interopReferences.ComInterfaceEntry.MakePointerType();

            // Define the 'ComputeVtables' method as follows:
            //
            // public static ComInterfaceEntry* ComputeVtables(out int count)
            MethodDefinition computeVtablesMethod = new(
                name: "ComputeVtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: computeVtablesReturnType,
                    parameterTypes: [module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
            {
                CilOutParameterIndices = [1],
                CilInstructions =
                {
                    { Ldarg_1 },
                    { CilInstruction.CreateLdcI4(interfaceEntriesType.Fields.Count) },
                    { Stind_I4 },
                    { Call, interfaceEntriesImplType.GetMethod("get_Vtables"u8) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(computeVtablesMethod);

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
                    { CilInstruction.CreateLdcI4((int)CreateComInterfaceFlags.TrackerSupport) },
                    { Call, interopReferences.WindowsRuntimeComWrappersMarshalGetOrCreateComInterfaceForObject },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(getOrCreateComInterfaceForObjectMethod);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="comWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            TypeSignature userDefinedType,
            TypeDefinition comWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition proxyType)
        {
            TypeDefinition userDefinedTypeDefinition = userDefinedType.Resolve()!;

            // If the user-defined type has '[WindowsRuntimeClassName]', then it means it's using a custom runtime
            // class name, which we want to preserve. In this case, just emit '[WindowsRuntimeMappedType]' on the
            // proxy, so the runtime lookup will find the original type and read the name from the attribute on it.
            if (userDefinedTypeDefinition.HasCustomAttribute(interopReferences.WindowsRuntimeClassNameAttribute))
            {
                InteropTypeDefinitionBuilder.Proxy(
                    ns: InteropUtf8NameFactory.TypeNamespace(userDefinedType),
                    name: InteropUtf8NameFactory.TypeName(userDefinedType),
                    mappedMetadata: null,
                    runtimeClassName: null,
                    metadataTypeName: null,
                    mappedType: userDefinedType,
                    referenceType: null,
                    comWrappersMarshallerAttributeType: comWrappersMarshallerAttributeType,
                    interopReferences: interopReferences,
                    module: module,
                    out proxyType);
            }
            else
            {
                // Get the most derived Windows Runtime interface for the type, to use for the runtime class name
                if (!WindowsRuntimeTypeAnalyzer.TryGetMostDerivedWindowsRuntimeInterfaceType(
                    type: userDefinedType,
                    interopReferences: interopReferences,
                    interfaceType: out TypeSignature? interfaceType))
                {
                    // We should always find at least one Windows Runtime interface, or the user-defined type wouldn't have
                    // been added to the set of exposed types during discovery. However, let's validate that here too.
                    throw WellKnownInteropExceptions.PrimaryWindowsRuntimeInterfaceNotFoundError(userDefinedType);
                }

                // Otherwise, we'll use the runtime class name of the first implemented Windows Runtime interface
                InteropTypeDefinitionBuilder.Proxy(
                    ns: InteropUtf8NameFactory.TypeNamespace(userDefinedType),
                    name: InteropUtf8NameFactory.TypeName(userDefinedType),
                    mappedMetadata: null,
                    runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(interfaceType, useWindowsUIXamlProjections),
                    metadataTypeName: null,
                    mappedType: null,
                    referenceType: null,
                    comWrappersMarshallerAttributeType: comWrappersMarshallerAttributeType,
                    interopReferences: interopReferences,
                    module: module,
                    out proxyType);
            }
        }

        /// <summary>
        /// Creates the type map attributes for some user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="Proxy"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            TypeSignature userDefinedType,
            TypeDefinition proxyType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: null,
                metadataTypeName: null,
                externalTypeMapTargetType: null,
                externalTypeMapTrimTargetType: null,
                marshallingTypeMapSourceType: userDefinedType,
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
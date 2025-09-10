// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
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
        /// The number of default, always present COM interface entries.
        /// We always append some default slots to all user-defined types.
        /// </summary>
        private const int NumberOfDefaultComInterfaceEntries = 6;

        /// <summary>
        /// The thread-local list to build COM interface entries.
        /// </summary>
        [ThreadStatic]
        private static List<(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable)>? entriesList;

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for a user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="vtableTypes">The vtable types implemented by <paramref name="userDefinedType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            TypeSignature userDefinedType,
            TypeSignatureEquatableSet vtableTypes,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceEntriesImplType)
        {
            // Reuse the same list, to minimize allocations (since we always need to build the entries at runtime here)
            List<(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable)> entriesList = UserDefinedType.entriesList ??= [];

            // It's not guaranteed that the list is empty, so we must always reset it first
            entriesList.Clear();

            // Append all entries for the type (which we share for all matching user-defined types)
            foreach (TypeSignature typeSignature in vtableTypes)
            {
                // There's two main scenarios we must handle here. For generic types (i.e. generic interfaces), their
                // marshalling code will be in 'WinRT.Interop.dll', and produced at build time by this same executable.
                // For non-generic ones, their marshalling code will instead usually be in the declaring assembly. The
                // only exception is for custom-mapped interface types: their ABI types are in 'WinRT.Runtime.dll'.
                if (typeSignature is GenericInstanceTypeSignature)
                {
                    TypeDefinition typeDefinition = emitState.LookupTypeDefinition(typeSignature, "Impl");

                    // Add the entry from the ABI type in 'WinRT.Interop.dll'
                    entriesList.Add((
                        get_IID: typeDefinition.GetMethod("get_IID"u8),
                        get_Vtable: typeDefinition.GetMethod("get_Vtable"u8)));
                }
                else if (typeSignature.IsCustomMappedWindowsRuntimeInterfaceType(interopReferences))
                {
                    TypeReference typeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference($"ABI.{typeSignature.Namespace}", $"{typeSignature.Name}Impl");

                    // Add the entry from the ABI type in 'WinRT.Runtime.dll'
                    entriesList.Add((
                        get_IID: typeReference.CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(WellKnownTypeSignatureFactory.InGuid(interopReferences))),
                        get_Vtable: typeReference.CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(interopReferences.CorLibTypeFactory.IntPtr))));
                }
                else
                {
                    TypeReference typeReference = typeSignature.Resolve()!.Module!.CreateTypeReference($"ABI.{typeSignature.Namespace}", $"{typeSignature.Name}Impl");

                    // Add the entry from the ABI type in the same declaring assembly
                    entriesList.Add((
                        get_IID: typeReference.CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(WellKnownTypeSignatureFactory.InGuid(interopReferences))),
                        get_Vtable: typeReference.CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(interopReferences.CorLibTypeFactory.IntPtr))));
                }
            }

            // Add the default entries at the end
            entriesList.AddRange(
                (interopReferences.IStringableImplget_IID, interopReferences.IStringableImplget_Vtable),
                (interopReferences.IWeakReferenceSourceImplget_IID, interopReferences.IWeakReferenceSourceImplget_Vtable),
                (interopReferences.IMarshalImplget_IID, interopReferences.IMarshalImplget_Vtable),
                (interopReferences.IAgileObjectImplget_IID, interopReferences.IAgileObjectImplget_Vtable),
                (interopReferences.IInspectableImplget_IID, interopReferences.IInspectableImplget_Vtable),
                (interopReferences.IUnknownImplget_IID, interopReferences.IUnknownImplget_Vtable));

            InteropTypeDefinitionBuilder.InterfaceEntriesImpl(
                ns: "WindowsRuntime.Interop.UserDefinedTypes"u8,
                name: InteropUtf8NameFactory.TypeName(userDefinedType, "InterfaceEntriesImpl"),
                entriesFieldType: interopDefinitions.UserDefinedInterfaceEntries(NumberOfDefaultComInterfaceEntries + vtableTypes.Count),
                interopReferences: interopReferences,
                module: module,
                implType: out interfaceEntriesImplType,
                implTypes: CollectionsMarshal.AsSpan(entriesList));
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="vtableTypes">The vtable types implemented by <paramref name="userDefinedType"/>.</param>
        /// <param name="interfaceEntriesImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            TypeSignature userDefinedType,
            TypeSignatureEquatableSet vtableTypes,
            TypeDefinition interfaceEntriesImplType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: "WindowsRuntime.Interop.UserDefinedTypes"u8,
                name: InteropUtf8NameFactory.TypeName(userDefinedType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute.Import(module));

            module.TopLevelTypes.Add(marshallerType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module);

            marshallerType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Call, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor.Import(module));

            // The 'ComputeVtables' method returns the 'ComWrappers.ComInterfaceEntry*' type
            PointerTypeSignature computeVtablesReturnType = interopReferences.ComInterfaceEntry.Import(module).MakePointerType();

            // Retrieve the cached COM interface entries type, as we need the number of fields
            TypeDefinition interfaceEntriesType = interopDefinitions.UserDefinedInterfaceEntries(NumberOfDefaultComInterfaceEntries + vtableTypes.Count);

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

            // Add and implement the 'ComputeVtables' method
            marshallerType.AddMethodImplementation(
                declaration: interopReferences.WindowsRuntimeComWrappersMarshallerAttributeComputeVtables.Import(module),
                method: computeVtablesMethod);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="comWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            TypeSignature userDefinedType,
            TypeDefinition comWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(userDefinedType),
                name: InteropUtf8NameFactory.TypeName(userDefinedType),
                runtimeClassName: "", // TODO
                comWrappersMarshallerAttributeType: comWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
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
                externalTypeMapTargetType: null,
                externalTypeMapTrimTargetType: null,
                proxyTypeMapSourceType: userDefinedType,
                proxyTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                interfaceTypeMapSourceType: null,
                interfaceTypeMapProxyType: null,
                interopReferences: interopReferences,
                module: module);
        }
    }
}

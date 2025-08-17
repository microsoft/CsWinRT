// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

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
        private static List<(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable)>? entriesList;

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for a user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="vtableTypes">The vtable types implemented by <paramref name="userDefinedType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            TypeSignature userDefinedType,
            TypeSignatureEquatableSet vtableTypes,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceEntriesImplType)
        {
            // We always append some default slots to all user-defined types (see below)
            const int NumberOfDefaultEntries = 6;

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
                if (typeSignature is GenericInstanceTypeSignature genericTypeSignature)
                {
                    // TODO
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
                entriesFieldType: interopDefinitions.UserDefinedInterfaceEntries(NumberOfDefaultEntries + vtableTypes.Count),
                interopReferences: interopReferences,
                module: module,
                implType: out interfaceEntriesImplType,
                implTypes: CollectionsMarshal.AsSpan(entriesList));
        }
    }
}

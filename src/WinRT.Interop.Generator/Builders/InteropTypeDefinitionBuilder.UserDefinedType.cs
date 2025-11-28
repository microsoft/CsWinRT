// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
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
        private static List<InterfaceEntryInfo>? entriesList;

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for a user-defined type.
        /// </summary>
        /// <param name="userDefinedType">The <see cref="TypeSignature"/> for the user-defined type.</param>
        /// <param name="vtableTypes">The vtable types implemented by <paramref name="userDefinedType"/>.</param>
        /// <param name="args">The arguments for this invocation.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            TypeSignature userDefinedType,
            TypeSignatureEquatableSet vtableTypes,
            InteropGeneratorArgs args,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceEntriesImplType)
        {
            // Reuse the same list, to minimize allocations (since we always need to build the entries at runtime here)
            List<InterfaceEntryInfo> entriesList = UserDefinedType.entriesList ??= [];

            // It's not guaranteed that the list is empty, so we must always reset it first
            entriesList.Clear();

            // Append all entries for the type (which we share for all matching user-defined types)
            foreach (TypeSignature typeSignature in vtableTypes)
            {
                // There's multiple scenarios we must handle here. For generic types (i.e. generic interfaces), their
                // marshalling code will be in 'WinRT.Interop.dll', and produced at build time by this same executable.
                if (typeSignature is GenericInstanceTypeSignature)
                {
                    TypeDefinition implTypeDefinition = emitState.LookupTypeDefinition(typeSignature, "Impl");
                    MethodDefinition get_VtableMethod = implTypeDefinition.GetMethod("get_Vtable"u8);

                    // The IID will be in the generated '<InterfaceIIDs>' type in 'WinRT.Interop.dll'
                    Utf8String get_IIDMethodName = $"get_IID_{InteropUtf8NameFactory.TypeName(typeSignature)}";
                    MethodDefinition get_IIDMethod = interopDefinitions.InterfaceIIDs.GetMethod(get_IIDMethodName);

                    // Add the entry from the ABI type in 'WinRT.Interop.dll'
                    entriesList.Add(new WindowsRuntimeInterfaceEntryInfo(get_IIDMethod, get_VtableMethod));
                }
                else if (typeSignature.IsCustomMappedWindowsRuntimeInterfaceType(interopReferences))
                {
                    // For (non-generic) custom mapped types, their ABI types are in 'WinRT.Runtime.dll', so we use those directly
                    TypeReference typeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference($"ABI.{typeSignature.Namespace}", $"{typeSignature.Name}Impl");
                    MemberReference get_VtableMethod = typeReference.CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(interopReferences.CorLibTypeFactory.IntPtr));

                    // For custom-mapped types, the IID is in 'WellKnownInterfaceIIDs' in 'WinRT.Runtime.dll'
                    MemberReference get_IIDMethod = WellKnownInterfaceIIDs.get_IID(
                        interfaceType: typeSignature,
                        interopReferences: interopReferences,
                        useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

                    // Add the entry from the ABI type in 'WinRT.Runtime.dll'
                    entriesList.Add(new WindowsRuntimeInterfaceEntryInfo(get_IIDMethod, get_VtableMethod));
                }
                else
                {
                    // We always need to resolve the user-defined types in all cases below, so just do it once first
                    TypeDefinition interfaceType = typeSignature.Resolve()!;

                    // For '[GeneratedComInterface]', we need to retrieve and use the generated vtable from the COM generators
                    if (interfaceType.IsGeneratedComInterfaceType)
                    {
                        // Ignore interfaces we can't retrieve information for (this should never happen, interfaces are filtered during discovery)
                        if (!interfaceType.TryGetInterfaceInformationType(interopReferences, out TypeSignature? interfaceInformationType))
                        {
                            continue;
                        }

                        // Add the entry from the 'InterfaceInformation' type, which contains the generated info we need
                        entriesList.Add(new ComInterfaceEntryInfo(interfaceInformationType));
                    }
                    else
                    {
                        // Finally, we have the base scenario of simple non-generic projected Windows Runtime interface types. In this
                        // case, the marshalling code will just be in the declaring assembly of each of these projected interface types.
                        TypeReference ImplTypeReference = interfaceType.DeclaringModule!.CreateTypeReference($"ABI.{typeSignature.Namespace}", $"{typeSignature.Name}Impl");
                        MemberReference get_VtableMethod = ImplTypeReference.CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(interopReferences.CorLibTypeFactory.IntPtr));

                        // For normal projected types, the IID is in the generated 'InterfaceIIDs' type in the containing assembly
                        string get_IIDMethodName = $"get_IID_{typeSignature.FullName.Replace('.', '_')}";
                        TypeSignature get_IIDMethodReturnType = WellKnownTypeSignatureFactory.InGuid(interopReferences);
                        TypeReference interfaceIIDsTypeReference = interfaceType.DeclaringModule!.CreateTypeReference("ABI"u8, "InterfaceIIDs"u8);
                        MemberReference get_IIDMethod = interfaceIIDsTypeReference.CreateMemberReference(get_IIDMethodName, MethodSignature.CreateStatic(get_IIDMethodReturnType));

                        // Add the entry from the ABI type in the same declaring assembly
                        entriesList.Add(new WindowsRuntimeInterfaceEntryInfo(get_IIDMethod, get_VtableMethod));
                    }
                }
            }

            // Add the default entries at the end
            entriesList.AddRange(
                new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IStringable, interopReferences.IStringableImplget_Vtable),
                new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IWeakReferenceSource, interopReferences.IWeakReferenceSourceImplget_Vtable),
                new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IMarshal, interopReferences.IMarshalImplget_Vtable),
                new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IAgileObject, interopReferences.IAgileObjectImplget_Vtable),
                new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IInspectable, interopReferences.IInspectableImplget_Vtable),
                new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IUnknown, interopReferences.IUnknownImplget_Vtable));

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
                    { Call, interopReferences.WindowsRuntimeComWrappersMarshalGetOrCreateComInterfaceForObject.Import(module) },
                    { Ret }
                }
            };

            // Add and implement the 'GetOrCreateComInterfaceForObject' method
            marshallerType.Methods.Add(getOrCreateComInterfaceForObjectMethod);
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

        /// <summary>
        /// An <see cref="InterfaceEntryInfo"/> type for Windows Runtime types.
        /// </summary>
        /// <param name="get_IID">The <see cref="IMethodDefOrRef"/> value to get the interface IID.</param>
        /// <param name="get_Vtable">The <see cref="IMethodDefOrRef"/> value to get the interface vtable.</param>
        private sealed class WindowsRuntimeInterfaceEntryInfo(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable) : InterfaceEntryInfo
        {
            /// <inheritdoc/>
            public override void LoadIID(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
            {
                _ = instructions.Add(Call, get_IID.Import(module));
                _ = instructions.Add(Ldobj, interopReferences.Guid.Import(module));
            }

            /// <inheritdoc/>
            public override void LoadVtable(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
            {
                _ = instructions.Add(Call, get_Vtable.Import(module));
            }
        }

        /// <summary>
        /// An <see cref="InterfaceEntryInfo"/> type for COM types.
        /// </summary>
        /// <param name="interfaceInformationType">The <c>InterfaceInformation</c> type for the current interface.</param>
        private sealed class ComInterfaceEntryInfo(TypeSignature interfaceInformationType) : InterfaceEntryInfo
        {
            /// <inheritdoc/>
            public override void LoadIID(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
            {
                _ = instructions.Add(Constrained, interfaceInformationType.Import(module).ToTypeDefOrRef());
                _ = instructions.Add(Call, interopReferences.IIUnknownInterfaceTypeget_Iid.Import(module));
            }

            /// <inheritdoc/>
            public override void LoadVtable(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
            {
                _ = instructions.Add(Constrained, interfaceInformationType.Import(module).ToTypeDefOrRef());
                _ = instructions.Add(Call, interopReferences.IIUnknownInterfaceTypeget_ManagedVirtualMethodTable.Import(module));
            }
        }
    }
}
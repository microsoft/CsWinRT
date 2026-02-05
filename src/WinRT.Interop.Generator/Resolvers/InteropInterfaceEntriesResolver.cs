// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for CCW interface entries for some managed type to exposed to native code.
/// </summary>
internal static class InteropInterfaceEntriesResolver
{
    /// <summary>
    /// Enumerates all <see cref="InteropInterfaceEntryInfo"/> values from a given source set of vtable types
    /// </summary>
    /// <param name="vtableTypes">The vtable types to use as source.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    public static IEnumerable<InteropInterfaceEntryInfo> EnumerateInterfaceEntries(
        TypeSignatureEquatableSet vtableTypes,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        InteropGeneratorEmitState emitState,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        // Append all entries for the type (which we share for all matching user-defined types)
        foreach (TypeSignature typeSignature in vtableTypes)
        {
            // Handle generic types first, and then custom-mapped and manually projected types.
            // These require special handling, because their ABI types are in different locations.
            if (typeSignature is GenericInstanceTypeSignature genericTypeSignature)
            {
                (IMethodDefOrRef get_IIDMethod, IMethodDefOrRef get_VtableMethod) = InteropImplTypeResolver.GetGenericInstanceTypeImpl(
                    type: genericTypeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState);

                yield return new WindowsRuntimeInterfaceEntryInfo(get_IIDMethod, get_VtableMethod);
            }
            else if (typeSignature.IsCustomMappedWindowsRuntimeInterfaceType(interopReferences) || typeSignature.IsManuallyProjectedWindowsRuntimeInterfaceType(interopReferences))
            {
                (IMethodDefOrRef get_IIDMethod, IMethodDefOrRef get_VtableMethod) = InteropImplTypeResolver.GetCustomMappedOrManuallyProjectedTypeImpl(
                    type: typeSignature,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: useWindowsUIXamlProjections);

                yield return new WindowsRuntimeInterfaceEntryInfo(get_IIDMethod, get_VtableMethod);
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

                    // Get the IID of the interface (same as above, this is pre-validated)
                    if (!interfaceType.TryGetGuidAttribute(interopReferences, out Guid interfaceId))
                    {
                        continue;
                    }

                    yield return new ComInterfaceEntryInfo(interfaceId, interfaceInformationType);
                }
                else
                {
                    // This is the common case for all normally projected, non-generic Windows Runtime types
                    (IMethodDefOrRef get_IIDMethod, IMethodDefOrRef get_VtableMethod) = InteropImplTypeResolver.GetProjectedTypeImpl(
                        type: interfaceType,
                        interopReferences: interopReferences);

                    yield return new WindowsRuntimeInterfaceEntryInfo(get_IIDMethod, get_VtableMethod);
                }
            }
        }
    }

    /// <summary>
    /// An <see cref="InteropInterfaceEntryInfo"/> type for Windows Runtime types.
    /// </summary>
    /// <param name="get_IID">The <see cref="IMethodDefOrRef"/> value to get the interface IID.</param>
    /// <param name="get_Vtable">The <see cref="IMethodDefOrRef"/> value to get the interface vtable.</param>
    private sealed class WindowsRuntimeInterfaceEntryInfo(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable) : InteropInterfaceEntryInfo
    {
        /// <inheritdoc/>
        public override bool IsIMarshalInterface => false;

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
    /// An <see cref="InteropInterfaceEntryInfo"/> type for COM types.
    /// </summary>
    /// <param name="iid">The IID of the interface type.</param>
    /// <param name="interfaceInformationType">The <c>InterfaceInformation</c> type for the current interface.</param>
    private sealed class ComInterfaceEntryInfo(Guid iid, TypeSignature interfaceInformationType) : InteropInterfaceEntryInfo
    {
        /// <inheritdoc/>
        public override bool IsIMarshalInterface => iid == WellKnownInterfaceIIDs.IID_IMarshal;

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

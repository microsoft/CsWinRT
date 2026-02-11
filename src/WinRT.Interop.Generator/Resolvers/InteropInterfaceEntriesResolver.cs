// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for CCW interface entries for some managed type to be exposed to native code.
/// </summary>
internal static class InteropInterfaceEntriesResolver
{
    /// <summary>
    /// The number of default, always present COM interface entries (returned by <see cref="EnumerateNativeInterfaceEntries"/>).
    /// </summary>
    public const int NumberOfNativeComInterfaceEntries = 6;

    /// <summary>
    /// Creates an <see cref="InteropInterfaceEntryInfo"/> instance with a provided set of methods.
    /// </summary>
    /// <param name="get_IID">The <see cref="IMethodDefOrRef"/> value to get the interface IID.</param>
    /// <param name="get_Vtable">The <see cref="IMethodDefOrRef"/> value to get the interface vtable.</param>
    /// <returns>The resulting <see cref="InteropInterfaceEntryInfo"/> instance.</returns>
    public static InteropInterfaceEntryInfo Create(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable)
    {
        return new WindowsRuntimeInterfaceEntryInfo(get_IID, get_Vtable);
    }

    /// <summary>
    /// Enumerates all <see cref="InteropInterfaceEntryInfo"/> values from a given source set of vtable types.
    /// </summary>
    /// <param name="vtableTypes">The vtable types to use as source.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    public static IEnumerable<InteropInterfaceEntryInfo> EnumerateMetadataInterfaceEntries(
        TypeSignatureEquatableSet vtableTypes,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        InteropGeneratorEmitState emitState,
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
                    emitState: emitState);

                yield return new WindowsRuntimeInterfaceEntryInfo(get_IIDMethod, get_VtableMethod);
            }
            else if (typeSignature.IsCustomMappedWindowsRuntimeInterfaceType(interopReferences) ||
                     typeSignature.IsManuallyProjectedWindowsRuntimeInterfaceType(interopReferences))
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

                    // If we find the special 'IMarshal' interface, ignore it here. We want to use this
                    // later to replace our built-in 'IMarshal' implementation in its own vtable slot.
                    if (interfaceId == WellKnownInterfaceIIDs.IID_IMarshal)
                    {
                        continue;
                    }

                    yield return new ComInterfaceEntryInfo(interfaceInformationType);
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
    /// Enumerates all <see cref="InteropInterfaceEntryInfo"/> values for native interfaces.
    /// </summary>
    /// <param name="vtableTypes">The vtable types to use as source.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    public static IEnumerable<InteropInterfaceEntryInfo> EnumerateNativeInterfaceEntries(
        TypeSignatureEquatableSet vtableTypes,
        InteropReferences interopReferences)
    {
        // Get the entry info for 'IMarshal', either user-provided or the built-in one
        if (!TryGetUserDefinedIMarshalInterfaceImplementation(
            vtableTypes: vtableTypes,
            interopReferences: interopReferences,
            interfaceEntryInfo: out InteropInterfaceEntryInfo? marshalInterfaceEntryInfo))
        {
            marshalInterfaceEntryInfo = new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IMarshal, interopReferences.IMarshalImplget_Vtable);
        }

        // Prepare the set of all built-in native interface implementations. These always follow the vtable slots for
        // user-defined interfaces implemented by exposed types. 'IUnknown' in particular must always be the last one.
        yield return new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IStringable, interopReferences.IStringableImplget_Vtable);
        yield return new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IWeakReferenceSource, interopReferences.IWeakReferenceSourceImplget_Vtable);
        yield return marshalInterfaceEntryInfo;
        yield return new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IAgileObject, interopReferences.IAgileObjectImplget_Vtable);
        yield return new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IInspectable, interopReferences.IInspectableImplget_Vtable);
        yield return new WindowsRuntimeInterfaceEntryInfo(interopReferences.WellKnownInterfaceIIDsget_IID_IUnknown, interopReferences.IUnknownImplget_Vtable);
    }

    /// <summary>
    /// Tries to get the <see cref="InteropInterfaceEntryInfo"/> value for a user-defined <c>IMarshal</c> interface.
    /// </summary>
    /// <param name="vtableTypes">The vtable types to use as source.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="interfaceEntryInfo">The resulting <see cref="InteropInterfaceEntryInfo"/> value for <c>IMarshal</c>, if found.</param>
    /// <returns>Whether <paramref name="interfaceEntryInfo"/> was found.</returns>
    private static bool TryGetUserDefinedIMarshalInterfaceImplementation(
        TypeSignatureEquatableSet vtableTypes,
        InteropReferences interopReferences,
        [NotNullWhen(true)] out InteropInterfaceEntryInfo? interfaceEntryInfo)
    {
        foreach (TypeSignature typeSignature in vtableTypes)
        {
            // Ignore generic interfaces ('IMarshal' isn't generic)
            if (typeSignature is GenericInstanceTypeSignature)
            {
                continue;
            }

            // Ignore all custom-mapped and special interfaces as well
            if (typeSignature.IsCustomMappedWindowsRuntimeInterfaceType(interopReferences) ||
                typeSignature.IsManuallyProjectedWindowsRuntimeInterfaceType(interopReferences))
            {
                continue;
            }

            // Resolve the user-defined interface type (same as above)
            TypeDefinition interfaceType = typeSignature.Resolve()!;

            // We only care about '[GeneratedComInterface]' types
            if (!interfaceType.IsGeneratedComInterfaceType)
            {
                continue;
            }

            // Get the IID of the interface (same as above)
            if (!interfaceType.TryGetGuidAttribute(interopReferences, out Guid interfaceId))
            {
                continue;
            }

            // Make sure that this is the 'IMarshal' implementation (we might not find one at all)
            if (interfaceId != WellKnownInterfaceIIDs.IID_IMarshal)
            {
                continue;
            }

            // Only get the information type now that we know we do need it
            if (!interfaceType.TryGetInterfaceInformationType(interopReferences, out TypeSignature? interfaceInformationType))
            {
                continue;
            }

            interfaceEntryInfo = new ComInterfaceEntryInfo(interfaceInformationType);

            return true;
        }

        interfaceEntryInfo = null;

        return false;
    }

    /// <summary>
    /// An <see cref="InteropInterfaceEntryInfo"/> type for Windows Runtime types.
    /// </summary>
    /// <param name="get_IID">The <see cref="IMethodDefOrRef"/> value to get the interface IID.</param>
    /// <param name="get_Vtable">The <see cref="IMethodDefOrRef"/> value to get the interface vtable.</param>
    private sealed class WindowsRuntimeInterfaceEntryInfo(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable) : InteropInterfaceEntryInfo
    {
        /// <inheritdoc/>
        public override void LoadIID(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
        {
            _ = instructions.Add(Call, get_IID);
            _ = instructions.Add(Ldobj, interopReferences.Guid);
        }

        /// <inheritdoc/>
        public override void LoadVtable(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
        {
            _ = instructions.Add(Call, get_Vtable);
        }
    }

    /// <summary>
    /// An <see cref="InteropInterfaceEntryInfo"/> type for COM types.
    /// </summary>
    /// <param name="interfaceInformationType">The <c>InterfaceInformation</c> type for the current interface.</param>
    private sealed class ComInterfaceEntryInfo(TypeSignature interfaceInformationType) : InteropInterfaceEntryInfo
    {
        /// <inheritdoc/>
        public override void LoadIID(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
        {
            _ = instructions.Add(Constrained, interfaceInformationType.ToTypeDefOrRef());
            _ = instructions.Add(Call, interopReferences.IIUnknownInterfaceTypeget_Iid);
        }

        /// <inheritdoc/>
        public override void LoadVtable(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module)
        {
            _ = instructions.Add(Constrained, interfaceInformationType.ToTypeDefOrRef());
            _ = instructions.Add(Call, interopReferences.IIUnknownInterfaceTypeget_ManagedVirtualMethodTable);
        }
    }
}

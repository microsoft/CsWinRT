// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// A discovery helper type for interop types.
/// </summary>
internal static partial class InteropTypeDiscovery
{
    /// <summary>
    /// A thread-local <see cref="TypeSignatureEquatableSet.Builder"/> instance that can be reused by discovery logic.
    /// </summary>
    [ThreadStatic]
    private static TypeSignatureEquatableSet.Builder? TypeSignatures;

    /// <summary>
    /// Tries to track an exposed user-defined type.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> for the type to analyze.</param>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <remarks>
    /// This method expects <paramref name="typeDefinition"/> to either be non-generic, or
    /// to have <paramref name="typeSignature"/> be a fully constructed signature for it.
    /// </remarks>
    public static bool TryTrackExposedUserDefinedType(
        TypeDefinition typeDefinition,
        TypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences)
    {
        // We can skip all projected Windows Runtime types early, as they don't need CCW support
        if (typeDefinition.IsProjectedWindowsRuntimeType)
        {
            return false;
        }

        // We'll need to look up attributes and enumerate interfaces across the entire type
        // hierarchy for this type, so make sure that we can resolve all types from it first.
        if (!typeDefinition.IsTypeHierarchyFullyResolvable(out ITypeDefOrRef? failedResolutionBaseType))
        {
            WellKnownInteropExceptions.UserDefinedTypeNotFullyResolvedWarning(failedResolutionBaseType, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);

            return false;
        }

        // We only want to process non-generic user-defined types that are potentially exposed to Windows Runtime
        if (!typeDefinition.IsPossiblyWindowsRuntimeExposedType || typeDefinition.IsWindowsRuntimeManagedOnlyType(interopReferences))
        {
            return false;
        }

        // Reuse the thread-local builder to track all implemented interfaces for the current type
        TypeSignatureEquatableSet.Builder interfaces = TypeSignatures ??= new TypeSignatureEquatableSet.Builder();

        // Since we're reusing the builder for all types, make sure to clear it first
        interfaces.Clear();

        // We want to explicitly track whether the type implements any projected Windows Runtime
        // interfaces, as we are only interested in such types. We want to also gather all
        // implemented '[GeneratedComInterface]' interfaces, but if a type only implements
        // those, we will ignore it. Such types should be marshalled via 'ComWrappers' directly.
        bool hasAnyProjectedWindowsRuntimeInterfaces = false;

        // Gather all implemented Windows Runtime interfaces for the current type
        foreach (TypeSignature interfaceSignature in typeSignature.EnumerateAllInterfaces())
        {
            // Make sure we can resolve the interface type fully, which we should always be able to do.
            // This can really only fail for some constructed generics, for invalid type arguments.
            if (!interfaceSignature.IsFullyResolvable(out TypeDefinition? interfaceDefinition))
            {
                WellKnownInteropExceptions.InterfaceImplementationTypeNotResolvedWarning(interfaceSignature, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);

                continue;
            }

            // Check for projected Windows Runtime interfaces first
            if (interfaceSignature.IsWindowsRuntimeType(interopReferences))
            {
                hasAnyProjectedWindowsRuntimeInterfaces = true;

                interfaces.Add(interfaceSignature);

                // If the current interface is generic, also make sure that it's tracked. This is needed
                // to fully cover all possible constructed generic interface types that might be needed.
                // For instance, consider this case:
                //
                // class A<T> : IEnumerable<T>;
                // class B : A<int>;
                //
                // While processing 'B', we'll discover the constructed 'IEnumerable<int>' interface.
                // This interface would not have been discovered when processing 'A<T>', as it's not
                // in the 'TypeSpec' metadata table, and only appears as unconstructed on 'A<T>'.
                // So the discovery logic for generic instantiations below would otherwise miss it.
                if (interfaceSignature is GenericInstanceTypeSignature constructedSignature)
                {
                    discoveryState.TrackGenericInterfaceType(constructedSignature, interopReferences);
                }
            }
            else if (interfaceDefinition.IsGeneratedComInterfaceType)
            {
                // We can only gather this type if we can find the generated 'InterfaceInformation' type.
                // If we can't find it, we can't add the interface to the list of interface entries. We
                // should warn if that's the (unlikely) case, so users can at least know that something
                // is wrong. Otherwise we'd just silently ignore these types, resulting in runtime failures.
                if (!interfaceDefinition.TryGetInterfaceInformationType(interopReferences, out _))
                {
                    WellKnownInteropExceptions.GeneratedComInterfaceImplementationTypeNotFoundWarning(interfaceDefinition, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);

                    continue;
                }

                // Ensure we can get the '[GuidAttribute]' from the interface. We need this at compile time
                // so we can check against some specific IID which might affect how we construct the COM
                // interface entries. For instance, we need to check whether 'IMarshal' is implemented.
                if (!interfaceDefinition.TryGetGuidAttribute(interopReferences, out Guid iid))
                {
                    WellKnownInteropExceptions.GeneratedComInterfaceGuidAttributeNotFoundWarning(interfaceDefinition, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);

                    continue;
                }

                // Validate that the current interface isn't trying to implement a reserved interface.
                // For instance, it's not allowed to try to explicitly implement 'IUnknown' or 'IInspectable'.
                if (WellKnownInterfaceIIDs.ReservedIIDsMap.TryGetValue(iid, out string? interfaceName))
                {
                    throw WellKnownInteropExceptions.GeneratedComInterfaceReservedGuidError(interfaceDefinition, typeDefinition, iid, interfaceName);
                }

                // Also track all '[GeneratedComInterface]' interfaces too, and filter them later (below)
                interfaces.Add(interfaceSignature);
            }
        }

        // If the user-defined type implements at least a Windows Runtime interface, then it's considered exposed.
        // We don't want to handle marshalling code for types with only '[GeneratedComInterface]' interfaces.
        if (hasAnyProjectedWindowsRuntimeInterfaces)
        {
            discoveryState.TrackUserDefinedType(typeSignature, interfaces.ToEquatableSet());
        }

        return true;
    }
}
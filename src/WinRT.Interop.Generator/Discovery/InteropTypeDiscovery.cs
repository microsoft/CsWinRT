// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// A discovery helper type for interop types.
/// </summary>
internal static partial class InteropTypeDiscovery
{
    /// <summary>
    /// A pool of <see cref="TypeSignatureEquatableSet.Builder"/> instances that can be reused by discovery logic.
    /// </summary>
    private static readonly ConcurrentBag<TypeSignatureEquatableSet.Builder> TypeSignatureBuilderPool = [];

    /// <summary>
    /// Tries to track a given composable Windows Runtime type.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> for the type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    public static void TryTrackTypeHierarchyType(
        TypeDefinition typeDefinition,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState)
    {
        // We only care about projected Windows Runtime classes
        if (!typeDefinition.IsProjectedWindowsRuntimeClassType)
        {
            return;
        }

        // Ignore types that don't have another base class
        if (!typeDefinition.HasBaseType(out ITypeDefOrRef? baseType))
        {
            return;
        }

        // We need to resolve the base type to be able to look up attributes on it
        if (!baseType.IsFullyResolvable(out _))
        {
            WellKnownInteropExceptions.WindowsRuntimeClassTypeNotResolvedWarning(baseType, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);

            return;
        }

        // If the base type is also a projected Windows Runtime type, track it
        if (baseType.IsProjectedWindowsRuntimeType)
        {
            discoveryState.TrackTypeHierarchyEntry(typeDefinition.FullName, baseType.FullName);
        }
    }

    /// <summary>
    /// Tries to track an exposed user-defined type.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> for the type to analyze.</param>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module currently being analyzed.</param>
    /// <remarks>
    /// This method expects <paramref name="typeDefinition"/> to either be non-generic, or
    /// to have <paramref name="typeSignature"/> be a fully constructed signature for it.
    /// </remarks>
    public static void TryTrackExposedUserDefinedType(
        TypeDefinition typeDefinition,
        TypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Ignore types that should explicitly be excluded
        if (TypeExclusions.IsExcluded(typeDefinition, interopReferences))
        {
            return;
        }

        // Ignore all type definitions with generic parameters where we don't have constructed
        // generic type signature. We can track these separately when we see them as instantiated.
        if (typeDefinition.HasGenericParameters && typeSignature is not GenericInstanceTypeSignature)
        {
            return;
        }

        // We can skip all projected Windows Runtime types early, as they don't need CCW support
        if (typeDefinition.IsProjectedWindowsRuntimeType)
        {
            return;
        }

        // We'll need to look up attributes and enumerate interfaces across the entire type
        // hierarchy for this type, so make sure that we can resolve all types from it first.
        if (!typeDefinition.IsTypeHierarchyFullyResolvable(out ITypeDefOrRef? failedResolutionBaseType))
        {
            WellKnownInteropExceptions.UserDefinedTypeNotFullyResolvedWarning(failedResolutionBaseType, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);

            return;
        }

        // We only want to process non-generic user-defined types that are potentially exposed to Windows Runtime
        if (!typeDefinition.IsPossiblyWindowsRuntimeExposedType || typeDefinition.IsWindowsRuntimeManagedOnlyType(interopReferences))
        {
            return;
        }

        // Check if this is the first time that this user-defined type has been seen, and stop immediately if not.
        // If the type has been seen before, it means that it either has already been fully processed, or that it
        // is currently being processed (possibly by another thread, if multi-threading discovery is enabled). The
        // reason for this check is not so much to improve performance (although it does avoid some repeated work),
        // but most importantly to avoid stack overflows due to infinite recursion in cases where user-defined types
        // implement interfaces that then transitively required the same user-defined type to be tracked.
        //
        // For instance, consider a scenario where 'List<int>' is being discovered. While processing the implemented
        // interfaces, 'IList<int>' will also be discovered. This will then require 'ReadOnlyCollection<int>' to be
        // tracked, because it is used by the fallback code for the CCW implementation method of 'IVector<T>.GetView'.
        // However, 'ReadOnlyCollection<int>' itself will also implement 'IList<int>', which would then require tracking
        // 'ReadOnlyCollection<int>' itself again too, etc. That would just recurse forever without this check, because
        // all those interfaces would keep being discovered before the initial processing of the type has finished.
        if (!discoveryState.TryMarkUserDefinedType(typeSignature))
        {
            return;
        }

        // Reuse a builder to track all implemented interfaces for the current type, or create a new one.
        // Note, we can't just have a single thread-local builder that we reuse here, as this method might
        // be called recursively in some scenarios. For instance, consider a type that implements a generic
        // interface such as 'IList<T>' (some constructed instantiation of it). As part of the interface
        // discovery logic, we'll also be tracking the additional 'ReadOnlyCollection<T>' type, as that's
        // needed from the 'IListAdapter<T>.GetView' method. That type will itself be analyzed here just
        // like any other user-define type, and so on. So the pool is needed to avoid creating conflicts.
        if (!TypeSignatureBuilderPool.TryTake(out TypeSignatureEquatableSet.Builder? interfaces))
        {
            interfaces = new TypeSignatureEquatableSet.Builder();
        }

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

            // Enumerate both the current interface, as well as all covariant combinations derived from it.
            // This is because we want entries in the vtable to match what users expect in the .NET world.
            // For instance, consider this scenario:
            //
            // class A : IDisposable;
            // class B : A, IEnumerable<string>
            // class C<T> : IEnumerable<T>;
            //
            // If we see e.g. a 'C<B>' instantiation, we want the following vtables being generated:
            // - 'IEnumerable<IEnumerable<string>>'
            // - 'IEnumerable<IEnumerable<object>>'
            // - 'IEnumerable<IEnumerable>'
            // - 'IEnumerable<IDisposable>'
            foreach (TypeSignature covariantInterfaceSignature in WindowsRuntimeTypeAnalyzer.EnumerateCovariantInterfaceTypes(interfaceSignature, interopReferences).Concat([interfaceSignature]))
            {
                // Check for projected Windows Runtime interfaces first
                if (covariantInterfaceSignature.IsWindowsRuntimeType(interopReferences))
                {
                    hasAnyProjectedWindowsRuntimeInterfaces = true;

                    interfaces.Add(covariantInterfaceSignature);

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
                    if (covariantInterfaceSignature is GenericInstanceTypeSignature constructedSignature)
                    {
                        TryTrackWindowsRuntimeGenericInterfaceTypeInstance(
                            typeSignature: constructedSignature,
                            args: args,
                            discoveryState,
                            interopReferences: interopReferences,
                            module: module);
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
                    interfaces.Add(covariantInterfaceSignature);
                }
            }
        }

        // If the user-defined type implements at least a Windows Runtime interface, then it's considered exposed.
        // We don't want to handle marshalling code for types with only '[GeneratedComInterface]' interfaces.
        if (hasAnyProjectedWindowsRuntimeInterfaces)
        {
            discoveryState.TrackUserDefinedType(typeSignature, interfaces.ToEquatableSet());
        }

        // Return the builder to the pool for reuse
        TypeSignatureBuilderPool.Add(interfaces);
    }
}
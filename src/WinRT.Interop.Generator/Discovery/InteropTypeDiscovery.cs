// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Factories;
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
    /// A pool of <see cref="HashSet{T}"/> instances used to validate duplicate IIDs.
    /// </summary>
    private static readonly ConcurrentBag<HashSet<Guid>> IidHashSetPool = [];

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
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
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
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Ignore types that should explicitly be excluded
        if (TypeExclusions.IsExcluded(typeSignature, interopReferences))
        {
            return;
        }

        // Ignore SZ array types, we can't handle them from here and they have dedicated logic below
        if (typeSignature is SzArrayTypeSignature)
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

        // Use the same logic to also retrieve the set to use to validate unique IIDs in custom interfaces
        if (!IidHashSetPool.TryTake(out HashSet<Guid>? iids))
        {
            iids = [];
        }

        // Clear this set as well, since we might've retrieved one from the shared pool
        iids.Clear();

        // We want to explicitly track whether the type implements any projected Windows Runtime
        // interfaces, as we are only interested in such types. We want to also gather all
        // implemented '[GeneratedComInterface]' interfaces, but if a type only implements
        // those, we will ignore it. Such types should be marshalled via 'ComWrappers' directly.
        bool hasAnyProjectedWindowsRuntimeInterfaces = false;

        // First gather any special '[exclusiveto]' interface for types in authored components. In practice this can
        // never fail, since there's no way an authored type would have more than 128 '[exclusiveto]' interfaces.
        if (!TryAddComponentExclusiveToInterfaceTypes(
            typeDefinition: typeDefinition,
            typeSignature: typeSignature,
            interfaces: interfaces,
            args: args,
            interopDefinitions: interopDefinitions,
            interopReferences: interopReferences))
        {
            goto FinalizeUserDefinedType;
        }

        // Up until this point we could have only gathered '[exclusiveto]' interfaces, which are by definition Windows Runtime
        // interfaces. So we can just check if we have any interfaces in our set to determine if the type should be exposed.
        hasAnyProjectedWindowsRuntimeInterfaces = interfaces.Count > 0;

        // Gather all implemented Windows Runtime interfaces for the current type
        foreach (TypeSignature interfaceSignature in typeSignature.EnumerateAllInterfaces(interopReferences))
        {
            // Make sure we can resolve the interface type fully, which we should always be able to do.
            // This can really only fail for some constructed generics, for invalid type arguments.
            if (!interfaceSignature.IsFullyResolvable(out TypeDefinition? interfaceDefinition))
            {
                WellKnownInteropExceptions.InterfaceImplementationTypeNotResolvedWarning(interfaceSignature, typeSignature).LogOrThrow(args.TreatWarningsAsErrors);

                continue;
            }

            // Check if the current interface is a Windows Runtime interface. We compute this here so that later
            // we can still include the current interface even if it is an '[exclusiveto]' one, while filtering
            // out all other '[exclusiveto]' interfaces that might show up as part of the covariant expansion.
            // The reason for this is that we want overridable interfaces to be in the CCW interface entries,
            // while we don't want them to appear if they're just a type argument for a generic interface.
            bool isInterfaceWindowsRuntime = interfaceSignature.IsWindowsRuntimeType(interopReferences);

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
            foreach (TypeSignature covariantInterfaceSignature in WindowsRuntimeTypeAnalyzer.EnumerateCovarianceExpandedInterfaceTypes(interfaceSignature, interopReferences).Concat([interfaceSignature]))
            {
                // Check for projected Windows Runtime interfaces first. We want to explicitly ignore
                // '[exclusiveto]' interfaces too, which might still show up as part of the covariant
                // expansion. However, those would then either fail to resolve or just result in
                // unnecessary binary size increase, since nobody would ever use them from here.
                // We also have an additional check to include overridable interfaces (see notes above).
                if (covariantInterfaceSignature.IsNotExclusiveToWindowsRuntimeType(interopReferences) ||
                    (isInterfaceWindowsRuntime && SignatureComparer.IgnoreVersion.Equals(covariantInterfaceSignature, interfaceSignature)))
                {
                    hasAnyProjectedWindowsRuntimeInterfaces = true;

                    // Try to track the current interface, stop if we exceeded the maximum limit
                    if (!TryAddExposedInterfaceType(
                        typeSignature: typeSignature,
                        interfaceType: covariantInterfaceSignature,
                        interfaces: interfaces,
                        args: args))
                    {
                        goto FinalizeUserDefinedType;
                    }

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
                            interopDefinitions: interopDefinitions,
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

                    // Ensure that this is the first interface we see implemented on this type with this IID
                    if (!iids.Add(iid))
                    {
                        WellKnownInteropExceptions.GeneratedComInterfaceDuplicateIidWarning(interfaceDefinition, typeDefinition, iid).LogOrThrow(args.TreatWarningsAsErrors);
                    }

                    // Validate that the current interface isn't trying to implement a reserved interface.
                    // For instance, it's not allowed to try to explicitly implement 'IUnknown' or 'IInspectable'.
                    if (WellKnownInterfaceIIDs.ReservedIIDsMap.TryGetValue(iid, out string? interfaceName))
                    {
                        throw WellKnownInteropExceptions.GeneratedComInterfaceReservedGuidError(interfaceDefinition, typeDefinition, iid, interfaceName);
                    }

                    // Also track all '[GeneratedComInterface]' interfaces too, and filter them later (below)
                    if (!TryAddExposedInterfaceType(
                        typeSignature: typeSignature,
                        interfaceType: covariantInterfaceSignature,
                        interfaces: interfaces,
                        args: args))
                    {
                        break;
                    }
                }
            }
        }

    FinalizeUserDefinedType:

        // If the user-defined type implements at least a Windows Runtime interface, then it's considered exposed.
        // We don't want to handle marshalling code for types with only '[GeneratedComInterface]' interfaces.
        if (hasAnyProjectedWindowsRuntimeInterfaces)
        {
            discoveryState.TrackUserDefinedType(typeSignature, interfaces.ToEquatableSet());
        }

        // Return the builder and set to the pool for reuse
        TypeSignatureBuilderPool.Add(interfaces);
        IidHashSetPool.Add(iids);
    }

    /// <summary>
    /// Tries to track an exposed SZ array type (which may or may not have a Windows Runtime element type).
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> for the type to analyze.</param>
    /// <param name="typeSignature">The <see cref="SzArrayTypeSignature"/> for the SZ array type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module currently being analyzed.</param>
    /// <remarks>
    /// This method expects <paramref name="typeDefinition"/> to either be non-generic, or
    /// to have <paramref name="typeSignature"/> be a fully constructed signature for it.
    /// </remarks>
    public static void TryTrackExposedSzArrayType(
        TypeDefinition typeDefinition,
        SzArrayTypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Ignore types that should explicitly be excluded
        if (TypeExclusions.IsExcluded(typeSignature, interopReferences))
        {
            return;
        }

        // We'll need to look up attributes and enumerate interfaces across the entire type
        // hierarchy for this type, so make sure that we can resolve all types from it first.
        if (!typeDefinition.IsTypeHierarchyFullyResolvable(out ITypeDefOrRef? failedResolutionBaseType))
        {
            WellKnownInteropExceptions.ArrayTypeElementTypeNotFullyResolvedWarning(failedResolutionBaseType, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);

            return;
        }

        // If the element type is a managed only type, ignore the array type. It is true that the array itself
        // would still implement some Windows Runtime interfaces, however we assume that if a user has chosen
        // to block marshalling for a given type, it means they also wouldn't want code to handle arrays of it.
        if (typeDefinition.IsWindowsRuntimeManagedOnlyType(interopReferences))
        {
            return;
        }

        // Recursion check (see additional notes above)
        if (!discoveryState.TryMarkSzArrayType(typeSignature))
        {
            return;
        }

        // Get or create a builder (see additional notes above)
        if (!TypeSignatureBuilderPool.TryTake(out TypeSignatureEquatableSet.Builder? interfaces))
        {
            interfaces = new TypeSignatureEquatableSet.Builder();
        }

        // Make sure to clear the builder first (see additional notes above)
        interfaces.Clear();

        // Gather all implemented Windows Runtime interfaces for the current type. Note that for
        // SZ arrays we are guaranteed to have at least some, due to the enumerable interfaces
        // that the runtime automatically implements on them.
        foreach (TypeSignature interfaceSignature in typeSignature.EnumerateAllInterfaces(interopReferences))
        {
            // Validate that we can resolve the interface. In this case we should be pretty confident
            // that this won't possibly fail, since we expect to only see well-known interfaces here.
            if (!interfaceSignature.IsFullyResolvable(out TypeDefinition? interfaceDefinition))
            {
                WellKnownInteropExceptions.InterfaceImplementationTypeNotResolvedWarning(interfaceSignature, typeSignature).LogOrThrow(args.TreatWarningsAsErrors);

                continue;
            }

            // Enumerate the current interface and the covariant combinations (see additional notes above)
            foreach (TypeSignature covariantInterfaceSignature in WindowsRuntimeTypeAnalyzer.EnumerateCovarianceExpandedInterfaceTypes(interfaceSignature, interopReferences).Concat([interfaceSignature]))
            {
                // Track all interfaces except '[exclusiveto]' ones (see additional notes above). We don't need to care about
                // overridable interfaces here, since those can only apply to classes, and SZ arrays will never have any.
                if (covariantInterfaceSignature.IsNotExclusiveToWindowsRuntimeType(interopReferences))
                {
                    // Try to track the current interface (same validation as above)
                    if (!TryAddExposedInterfaceType(
                        typeSignature: typeSignature,
                        interfaceType: covariantInterfaceSignature,
                        interfaces: interfaces,
                        args: args))
                    {
                        goto FinalizeSzArrayType;
                    }

                    // Make sure that any discovered interfaces are also tracked (see additional notes above)
                    if (covariantInterfaceSignature is GenericInstanceTypeSignature constructedSignature)
                    {
                        TryTrackWindowsRuntimeGenericInterfaceTypeInstance(
                            typeSignature: constructedSignature,
                            args: args,
                            discoveryState,
                            interopDefinitions: interopDefinitions,
                            interopReferences: interopReferences,
                            module: module);
                    }
                }
            }
        }

    FinalizeSzArrayType:

        // If the array is a valid Windows Runtime type, track is specifically as such.
        // This is because in this case we'll require additional, specialized marshalling.
        if (typeSignature.IsWindowsRuntimeType(interopReferences))
        {
            discoveryState.TrackSzArrayType(typeSignature, interfaces.ToEquatableSet());
        }
        else
        {
            // Track the array as a user-defined type. Note that for SZ arrays that don't have an element type
            // that is a Windows Runtime type, they're effectively just like any other normal user-defined type.
            // That is, some 'Foo[]' type will behave conceptually the same as some 'List<Foo>' instantiation.
            discoveryState.TrackUserDefinedType(typeSignature, interfaces.ToEquatableSet());
        }

        // Return the builder to the pool for reuse
        TypeSignatureBuilderPool.Add(interfaces);
    }

    /// <summary>
    /// Tries to add any exclusive interfaces for authored types from Windows Runtime components written in C#.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> for the type to analyze.</param>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the type to analyze.</param>
    /// <param name="interfaces">The set of interfaces being populated.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>Whether the new interfaces could be added.</returns>
    private static bool TryAddComponentExclusiveToInterfaceTypes(
        TypeDefinition typeDefinition,
        TypeSignature typeSignature,
        TypeSignatureEquatableSet.Builder interfaces,
        InteropGeneratorArgs args,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences)
    {
        // Runtime class types from authored components can't be generic, so we can filter those
        // out immediately. Only the Windows SDK is allowed to define generic types. And even then,
        // those can only be either generic interfaces or generic delegates, and never classes.
        if (!typeDefinition.IsClass || typeDefinition.HasGenericParameters || typeDefinition.IsDelegate)
        {
            return true;
        }

        // If the type is not from an authored component, we can stop immediately
        if (typeDefinition.DeclaringModule is not { Assembly.IsWindowsRuntimeComponentAssembly: true })
        {
            return true;
        }

        // We're going to need the 'WinRT.Component.dll' assembly, so make sure it was generated and passed in
        if (interopDefinitions.WindowsRuntimeComponentModule is null)
        {
            throw WellKnownInteropExceptions.EnsureWindowsRuntimeComponentModuleError();
        }

        Utf8String @namespace = InteropUtf8NameFactory.TypeNamespace(typeSignature);

        // Go over all declared types in the component .dll to try to find those for '[exclusiveto]' interfaces
        foreach (TypeDefinition componentType in interopDefinitions.WindowsRuntimeComponentModule.TopLevelTypes)
        {
            // We know that the ones we might want will only be in the ABI namespace for this type,
            // so if that's not the case we can quickly ignore all other ones to speed up the search.
            if (componentType.Namespace != @namespace)
            {
                continue;
            }

            // Try to lookup '[WindowsRuntimeExclusiveToInterface]' from the authored type (there can be any number of them)
            if (componentType.IsClass && componentType.TryGetCustomAttribute(interopReferences.WindowsRuntimeExclusiveToInterfaceAttribute, out CustomAttribute? customAttribute))
            {
                // Validate that we can resolve the target type for the attribute. We can safely ignore failures here
                // since this would realistically never happen, since that .dll is being compiled from C# (not from IL).
                if (customAttribute.Signature is not { FixedArguments: [{ Element: TypeSignature runtimeClassSignature }, ..] })
                {
                    continue;
                }

                // We might have any number of types with this attribute in the same ABI namespace (since it could contain any
                // number of authored types). So if the target type doesn't match, we just ignore it and continue iterating.
                if (!SignatureComparer.IgnoreVersion.Equals(runtimeClassSignature, typeSignature))
                {
                    continue;
                }

                // We use a naming convention to map each generated '[exclusiveto]' interface to its IID, so we need to validate
                // that the name for this type is in fact the one we expect, before actually tracking it for code generation.
                if (!componentType.Namespace!.AsSpan().StartsWith("ABI."u8) || !componentType.Name!.AsSpan().EndsWith("Impl"u8))
                {
                    WellKnownInteropExceptions.ComponentTypeExclusiveToInterfaceInvalidFullNameError(componentType, typeDefinition).LogOrThrow(args.TreatWarningsAsErrors);
                }

                // Try to track the current implementation type. Note that this is not actually an interface (since we don't
                // actually need a managed interface type for this scenario). The emit code will handle this as appropriate.
                if (!TryAddExposedInterfaceType(
                    typeSignature: typeSignature,
                    interfaceType: componentType.ToTypeSignature(),
                    interfaces: interfaces,
                    args: args))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Tries to add a new tracked exposed Windows Runtime interface type.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the type to analyze.</param>
    /// <param name="interfaceType">The <see cref="TypeSignature"/> for the interface type to try to add.</param>
    /// <param name="interfaces">The set of interfaces being populated.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>Whether the new interface could be added.</returns>
    private static bool TryAddExposedInterfaceType(
        TypeSignature typeSignature,
        TypeSignature interfaceType,
        TypeSignatureEquatableSet.Builder interfaces,
        InteropGeneratorArgs args)
    {
        // If the set already contains the current interface, we can just skip it
        // and tell the user that we successfully "added" it, as it's already there.
        if (interfaces.Contains(interfaceType))
        {
            return true;
        }

        // Warn if we already have 128 items, as we know at this point the new interface we
        // would be going to add is one we never saw before, so it'd be successfully added.
        if (interfaces.Count == 128)
        {
            WellKnownInteropExceptions.ExceededNumberOfExposedWindowsRuntimeInterfaceTypesWarning(typeSignature).LogOrThrow(args.TreatWarningsAsErrors);

            return false;
        }

        _ = interfaces.Add(interfaceType);

        return true;
    }
}
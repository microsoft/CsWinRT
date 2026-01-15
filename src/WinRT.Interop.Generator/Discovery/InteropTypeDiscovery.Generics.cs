// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="InteropTypeDiscovery"/>
internal partial class InteropTypeDiscovery
{
    /// <summary>
    /// Tries to track a constructed generic type.
    /// </summary>
    /// <param name="typeSignature">The <see cref="GenericInstanceTypeSignature"/> for the constructed type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module currently being analyzed.</param>
    public static void TryTrackGenericTypeInstance(
        GenericInstanceTypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Filter all constructed generic type signatures we have. We don't care about generic type
        // definitions (eg. 'TypedEventHandler`1<!0, !1>') for the purposes of marshalling code.
        if (!typeSignature.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
        {
            return;
        }

        // Ignore types that are not fully resolvable (this likely means a .dll is missing)
        if (!typeSignature.IsFullyResolvable(out TypeDefinition? typeDefinition))
        {
            // Log a warning the first time we fail to resolve this generic instantiation in this module
            if (discoveryState.TrackFailedResolutionType(typeSignature, module))
            {
                WellKnownInteropExceptions.GenericTypeSignatureNotResolvedError(typeSignature, module).LogOrThrow(args.TreatWarningsAsErrors);
            }

            return;
        }

        // If the current type signature represents a Windows Runtime type, track it
        if (typeSignature.IsWindowsRuntimeType(interopReferences))
        {
            TryTrackWindowsRuntimeGenericTypeInstance(
                typeDefinition,
                typeSignature,
                args,
                discoveryState,
                interopReferences,
                module);
        }
        else
        {
            // Otherwise, try to track information for some constructed managed type
            TryTrackManagedGenericTypeInstance(
                typeDefinition,
                typeSignature,
                args,
                discoveryState,
                interopReferences);
        }
    }

    /// <summary>
    /// Tries to track an SZ array type.
    /// </summary>
    /// <param name="typeSignature">The <see cref="SzArrayTypeSignature"/> for the SZ array type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module currently being analyzed.</param>
    public static void TryTrackSzArrayType(
        SzArrayTypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Filter all constructed generic type signatures we have. We don't care about
        // generic type definitions (eg. '!0[]') for the purposes of marshalling code.
        if (!typeSignature.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
        {
            return;
        }

        // Ignore types that are not fully resolvable (this likely means a .dll is missing)
        if (!typeSignature.IsFullyResolvable(out _))
        {
            // Log a warning the first time we fail to resolve this SZ array in this module
            if (discoveryState.TrackFailedResolutionType(typeSignature, module))
            {
                WellKnownInteropExceptions.SzArrayTypeSignatureNotResolvedError(typeSignature, module).LogOrThrow(args.TreatWarningsAsErrors);
            }

            return;
        }

        // Ignore array types that are not Windows Runtime types
        if (!typeSignature.IsWindowsRuntimeType(interopReferences))
        {
            return;
        }

        // Track all SZ array types, as we'll need to emit marshalling code for them
        discoveryState.TrackSzArrayType(typeSignature);
    }

    /// <summary>
    /// Tries to track a constructed generic Windows Runtime type.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> for the type to analyze.</param>
    /// <param name="typeSignature">The <see cref="GenericInstanceTypeSignature"/> for the constructed type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void TryTrackWindowsRuntimeGenericTypeInstance(
        TypeDefinition typeDefinition,
        GenericInstanceTypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Gather all 'KeyValuePair<,>' instances
        if (typeSignature.IsValueType && typeSignature.IsConstructedKeyValuePairType(interopReferences))
        {
            discoveryState.TrackKeyValuePairType(typeSignature);

            return;
        }

        // Gather all Windows Runtime delegate types. We want to gather all projected delegate types, plus
        // any custom-mapped ones (e.g. 'EventHandler<TEventArgs>' and 'EventHandler<TSender, TEventArgs>').
        // The filtering is already done above, so here we can rely the type will be of one of those kinds.
        if (typeDefinition.IsDelegate)
        {
            discoveryState.TrackGenericDelegateType(typeSignature);

            return;
        }

        // Track all projected Windows Runtime generic interfaces
        if (typeDefinition.IsInterface)
        {
            discoveryState.TrackGenericInterfaceType(typeSignature, interopReferences);

            // We also want to crawl base interfaces
            foreach (TypeSignature interfaceSignature in typeSignature.EnumerateAllInterfaces())
            {
                // Filter out just constructed generic interfaces, since we only care about those here.
                // The non-generic ones are only useful when gathering interfaces for user-defined types.
                if (interfaceSignature is not GenericInstanceTypeSignature constructedSignature)
                {
                    continue;
                }

                if (!interfaceSignature.IsFullyResolvable(out _))
                {
                    // Also log a warning the first time we fail to resolve one of the recursively discovered generic
                    // instantiations from this module. The enumeration also yields back interfaces that couldn't be
                    // resolved, as that step is performed after yielding. This is done so we can have our own logic
                    // to log warnings or throw errors from here while we're processing interfaces in this module.
                    if (discoveryState.TrackFailedResolutionType(interfaceSignature, module))
                    {
                        WellKnownInteropExceptions.GenericTypeSignatureNotResolvedError(interfaceSignature, module).LogOrThrow(args.TreatWarningsAsErrors);
                    }

                    continue;
                }

                discoveryState.TrackGenericInterfaceType(constructedSignature, interopReferences);
            }
        }
    }

    /// <summary>
    /// Tries to track a constructed generic user-defined type.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> for the type to analyze.</param>
    /// <param name="typeSignature">The <see cref="GenericInstanceTypeSignature"/> for the constructed type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    private static void TryTrackManagedGenericTypeInstance(
        TypeDefinition typeDefinition,
        GenericInstanceTypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences)
    {
        // Check for all '[ReadOnly]Span<T>' types in particular, and track them as SZ array types.
        // This is because "pass-array" and "fill-array" parameters are projected using spans, but
        // those projections require the marshalling code produced when discovering SZ array types.
        // So if we see any of these spans where the element type is a Windows Runtime type, we
        // manually construct an SZ array type for it and add it to the set of tracked array types.
        if (typeSignature.IsValueType &&
            typeSignature.IsConstructedSpanOrReadOnlySpanType(interopReferences) &&
            typeSignature.TypeArguments[0].IsWindowsRuntimeType(interopReferences))
        {
            discoveryState.TrackSzArrayType(typeSignature.TypeArguments[0].MakeSzArrayType());

            return;
        }

        // Otherwise, try to track a constructed user-defined type
        TryTrackExposedUserDefinedType(
            typeDefinition,
            typeSignature,
            args,
            discoveryState,
            interopReferences);
    }
}
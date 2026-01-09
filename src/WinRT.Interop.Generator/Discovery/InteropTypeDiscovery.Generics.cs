// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
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
        // Ignore types that should explicitly be excluded
        if (TypeExclusions.IsExcluded(typeSignature, interopReferences))
        {
            return;
        }

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
                typeDefinition: typeDefinition,
                typeSignature: typeSignature,
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);
        }
        else
        {
            // Otherwise, try to track information for some constructed managed type
            TryTrackManagedGenericTypeInstance(
                typeDefinition: typeDefinition,
                typeSignature: typeSignature,
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);
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
        // Ignore types that should explicitly be excluded
        if (TypeExclusions.IsExcluded(typeSignature, interopReferences))
        {
            return;
        }

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
            TryTrackWindowsRuntimeGenericInterfaceTypeInstance(
                typeSignature: typeSignature,
                args: args,
                discoveryState,
                interopReferences: interopReferences,
                module: module);

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

                TryTrackWindowsRuntimeGenericInterfaceTypeInstance(
                    typeSignature: constructedSignature,
                    args: args,
                    discoveryState,
                    interopReferences: interopReferences,
                    module: module);
            }
        }
    }

    /// <summary>
    /// Tries to track a constructed generic Windows Runtime interface type.
    /// </summary>
    /// <param name="typeSignature">The <see cref="GenericInstanceTypeSignature"/> for the constructed type to analyze.</param>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void TryTrackWindowsRuntimeGenericInterfaceTypeInstance(
        GenericInstanceTypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IEnumerator1))
        {
            discoveryState.TrackIEnumerator1Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IEnumerable1))
        {
            discoveryState.TrackIEnumerable1Type(typeSignature);

            // We need special handling for 'IEnumerator<T>' types whenever we discover any constructed 'IEnumerable<T>'
            // type. This ensures that we're never missing any 'IEnumerator<T>' instantiation, which we might depend on
            // from other generated code, or projections. This special handling is needed because unlike with the other
            // interfaces, 'IEnumerator<T>' will not show up as a base interface for other collection interface types.
            discoveryState.TrackIEnumerator1Type(interopReferences.IEnumerator1.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IList1))
        {
            discoveryState.TrackIList1Type(typeSignature);

            // Whenever we find an 'IList<T>' instantiation, we also need to track the corresponding 'IReadOnlyList<T>' instantiation.
            // This is because that interface is needed to marshal the return value of the 'IVector<T>.GetView' method ('IVectorView<T>').
            discoveryState.TrackIReadOnlyList1Type(interopReferences.IReadOnlyList1.MakeGenericReferenceType([.. typeSignature.TypeArguments]));

            // We also need to track the constructed 'ReadOnlyCollection<T>' type, as that is used by 'IListAdapter<T>.GetView' in case the
            // input 'IList<T>' instance doesn't implement 'IReadOnlyList<T>' directly. In that case, we return a 'ReadOnlyCollection<T>'
            // object instead. This needs special handling because we won't analyze indirect (generated) calls into that adapter type.
            TryTrackGenericTypeInstance(
                typeSignature: interopReferences.ReadOnlyCollection1.MakeGenericReferenceType([.. typeSignature.TypeArguments]),
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IReadOnlyList1))
        {
            discoveryState.TrackIReadOnlyList1Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IDictionary2))
        {
            discoveryState.TrackIDictionary2Type(typeSignature);

            // When discovering dictionary types, make sure to also track 'KeyValuePair<TKey, TValue>' types. Those will
            // be needed when generating code for 'IEnumerator<KeyValuePair<TKey, TValue>>' types, which will be discovered
            // automatically. However, the same is not true the constructed 'KeyValuePair<TKey, TValue>' types themselves.
            // This is for the same reason why we need the other special cases in this method: members are not analyzed.
            discoveryState.TrackKeyValuePairType(interopReferences.KeyValuePair2.MakeGenericValueType([.. typeSignature.TypeArguments]));

            // When we discover a constructed 'IDictionary<TKey, TValue>' instantiation, we'll be generating a native object type during
            // the emit phase, which is used to marshal anonymous objects. This derives from 'WindowsRuntimeDictionary<TKey, TValue, ...>'.
            // For the 'Keys' and 'Values' properties, that base type will return instances of the 'DictionaryKeyCollection<TKey, TValue>'
            // and 'DictionaryValueCollection<TKey, TValue>' types, respectively. Those instantiations will not be seen by 'cswinrtgen',
            // because they will only exist in the final 'WinRT.Interop.dll' assembly being generated, and not in any input assemblies.
            // So to ensure that we can still correctly marshal those to native, if needed, we manually track them as if we had seen them.
            TryTrackGenericTypeInstance(
                typeSignature: interopReferences.DictionaryKeyCollection2.MakeGenericReferenceType([.. typeSignature.TypeArguments]),
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);

            // Handle 'DictionaryValueCollection<TKey, TValue>' as well
            TryTrackGenericTypeInstance(
                typeSignature: interopReferences.DictionaryValueCollection2.MakeGenericReferenceType([.. typeSignature.TypeArguments]),
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IReadOnlyDictionary2))
        {
            discoveryState.TrackIReadOnlyDictionary2Type(typeSignature);

            // Same handling as above for constructed 'KeyValuePair<TKey, TValue>' types
            discoveryState.TrackKeyValuePairType(interopReferences.KeyValuePair2.MakeGenericValueType([.. typeSignature.TypeArguments]));

            // Handle 'ReadOnlyDictionaryKeyCollection<TKey, TValue>' as above
            TryTrackGenericTypeInstance(
                typeSignature: interopReferences.ReadOnlyDictionaryKeyCollection2.MakeGenericReferenceType([.. typeSignature.TypeArguments]),
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);

            // Handle 'ReadOnlyDictionaryValueCollection<TKey, TValue>' as well
            TryTrackGenericTypeInstance(
                typeSignature: interopReferences.ReadOnlyDictionaryValueCollection2.MakeGenericReferenceType([.. typeSignature.TypeArguments]),
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);

            // Handle 'IReadOnlyDictionarySplitAdapter<TKey, TValue>', which is returned by the 'IMapView<K, V>.Split' method
            TryTrackGenericTypeInstance(
                typeSignature: interopReferences.IReadOnlyDictionarySplitAdapter2.MakeGenericReferenceType([.. typeSignature.TypeArguments]),
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);

            // Handle 'ArraySegment<T>.Enumerator', which is returned by 'IReadOnlyDictionarySplitAdapter<TKey, TValue>.GetEnumerator()'
            TryTrackGenericTypeInstance(
                typeSignature: interopReferences.ArraySegment1Enumerator.MakeGenericValueType(interopReferences.KeyValuePair2.MakeGenericValueType([.. typeSignature.TypeArguments])),
                args: args,
                discoveryState: discoveryState,
                interopReferences: interopReferences,
                module: module);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IObservableVector1))
        {
            discoveryState.TrackIObservableVector1Type(typeSignature);

            // We need special handling for constructed 'VectorChangedEventHandler<T>' types, as those are required for each
            // discovered 'IObservableVector<T>' type. These are not necessarily discovered in the same way, as while we are
            // recursively constructing interfaces, we don't have the same logic for delegate types (or for types used in
            // any signature of interface members). Because we only need this delegate type and the one below, we can just
            // special case it. That is, we manually construct it every time we discover a constructed 'IObservableVector<T>'.
            discoveryState.TrackGenericDelegateType(interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IObservableMap2))
        {
            discoveryState.TrackIObservableMap2Type(typeSignature);

            // Same handling as above for 'MapChangedEventHandler<K,V>' types
            discoveryState.TrackGenericDelegateType(interopReferences.MapChangedEventHandler2.MakeGenericReferenceType([.. typeSignature.TypeArguments]));

            // Also manually track the args type for 'MapChangedEventHandler<K,V>'
            discoveryState.TrackIMapChangedEventArgs1Type(interopReferences.IMapChangedEventArgs1.MakeGenericReferenceType(typeSignature.TypeArguments[1]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IMapChangedEventArgs1))
        {
            discoveryState.TrackIMapChangedEventArgs1Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IAsyncActionWithProgress1))
        {
            discoveryState.TrackIAsyncActionWithProgress1Type(typeSignature);

            // Ensure that the delegate types for this instantiation of 'IAsyncActionWithProgress<TProgress>' are also tracked.
            // Same rationale as above for the other special cased types. Same below as well for the other async info types.
            discoveryState.TrackGenericDelegateType(interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
            discoveryState.TrackGenericDelegateType(interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IAsyncOperation1))
        {
            discoveryState.TrackIAsyncOperation1Type(typeSignature);

            // Same handling as above for 'AsyncOperationCompletedHandler<TResult>'
            discoveryState.TrackGenericDelegateType(interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IAsyncOperationWithProgress2))
        {
            discoveryState.TrackIAsyncOperationWithProgress2Type(typeSignature);

            // Same handling as above for 'AsyncOperationProgressHandler<TResult, TProgress>' and 'AsyncOperationWithProgressCompletedHandler<TResult, TProgress>'
            discoveryState.TrackGenericDelegateType(interopReferences.AsyncOperationProgressHandler2.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
            discoveryState.TrackGenericDelegateType(interopReferences.AsyncOperationWithProgressCompletedHandler2.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
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
    /// <param name="module">The module currently being analyzed.</param>
    private static void TryTrackManagedGenericTypeInstance(
        TypeDefinition typeDefinition,
        GenericInstanceTypeSignature typeSignature,
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences,
        ModuleDefinition module)
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
            typeDefinition: typeDefinition,
            typeSignature: typeSignature,
            args: args,
            discoveryState: discoveryState,
            interopReferences: interopReferences,
            module: module);
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Provides output-bound signatures for the emit phase. Discovery may retain
/// any of several forwarded spellings of the same type, so they must be resolved before creating metadata.
/// User-defined types and their groups are prepared lazily once, sharing group state between emission passes.
/// </summary>
internal sealed class InteropGeneratorEmitInputs
{
    /// <summary>The completed discovery state.</summary>
    private readonly InteropGeneratorDiscoveryState _discoveryState;

    /// <summary>References used to identify key-value-pair collection types.</summary>
    private readonly InteropReferences _interopReferences;

    /// <summary>The output module's resolution-aware importer.</summary>
    private readonly ReferenceImporter _importer;

    /// <summary>The runtime context for ordering resolved type identities.</summary>
    private readonly RuntimeContext _runtimeContext;

    /// <summary>The cancellation token for canonicalizing types.</summary>
    private readonly CancellationToken _token;

    /// <summary>Canonicalized interface sets shared by discovered set identity.</summary>
    private readonly Dictionary<TypeSignatureEquatableSet, TypeSignatureEquatableSet> _canonicalVtableSets = new(ReferenceEqualityComparer.Instance);

    /// <summary>The shared, lazily prepared groups and globally ordered proxy inputs.</summary>
    private UserDefinedTypeInputs UserDefinedTypes => field ??= CreateUserDefinedTypeInputs();

    /// <summary>The canonicalized array inputs, prepared before concurrent emission.</summary>
    private (SzArrayTypeSignature Type, TypeSignatureEquatableSet VtableTypes)[] SzArrayTypes =>
        field ??= [.. CanonicalizeTypesAndVtables(_discoveryState.SzArrayAndVtableTypes)];

    /// <summary>
    /// Creates an output-bound view of the completed discovery state without materializing its signatures.
    /// </summary>
    /// <param name="discoveryState">The discovered types and interface sets.</param>
    /// <param name="interopReferences">References used to identify key-value-pair collection types.</param>
    /// <param name="module">The output module whose importer resolves forwarded type identities.</param>
    /// <param name="token">The cancellation token for preparing the emit inputs.</param>
    public InteropGeneratorEmitInputs(
        InteropGeneratorDiscoveryState discoveryState,
        InteropReferences interopReferences,
        ModuleDefinition module,
        CancellationToken token)
    {
        _discoveryState = discoveryState;
        _interopReferences = interopReferences;
        _importer = module.DefaultImporter;
        _runtimeContext = module.RuntimeContext!;
        _token = token;
    }

    /// <summary>
    /// Finishes shared interface-set canonicalization before independent emit families start.
    /// </summary>
    public void PrepareForConcurrentEmission()
    {
        _ = UserDefinedTypes;
        _ = SzArrayTypes;
    }

    /// <summary>Enumerates the generic delegate instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateGenericDelegateTypes()
    {
        return CanonicalizeTypes(_discoveryState.GenericDelegateTypes);
    }

    /// <summary>Enumerates the <see cref="IEnumerator{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIEnumerator1Types()
    {
        return CanonicalizeTypes(_discoveryState.IEnumerator1Types);
    }

    /// <summary>Enumerates the <see cref="IEnumerable{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIEnumerable1Types()
    {
        return CanonicalizeTypes(_discoveryState.IEnumerable1Types);
    }

    /// <summary>Enumerates the <see cref="IReadOnlyList{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIReadOnlyList1Types()
    {
        return CanonicalizeTypes(_discoveryState.IReadOnlyList1Types);
    }

    /// <summary>Enumerates the <see cref="IList{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIList1Types()
    {
        return CanonicalizeTypes(_discoveryState.IList1Types);
    }

    /// <summary>Enumerates the <see cref="IReadOnlyDictionary{TKey, TValue}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIReadOnlyDictionary2Types()
    {
        return CanonicalizeTypes(_discoveryState.IReadOnlyDictionary2Types);
    }

    /// <summary>Enumerates the <see cref="IDictionary{TKey, TValue}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIDictionary2Types()
    {
        return CanonicalizeTypes(_discoveryState.IDictionary2Types);
    }

    /// <summary>Enumerates the <see cref="KeyValuePair{TKey, TValue}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateKeyValuePairTypes()
    {
        return CanonicalizeTypes(_discoveryState.KeyValuePairTypes);
    }

    /// <summary>Enumerates the <c>IMapChangedEventArgs&lt;TKey&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIMapChangedEventArgs1Types()
    {
        return CanonicalizeTypes(_discoveryState.IMapChangedEventArgs1Types);
    }

    /// <summary>Enumerates the <c>IObservableVector&lt;T&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIObservableVector1Types()
    {
        return CanonicalizeTypes(_discoveryState.IObservableVector1Types);
    }

    /// <summary>Enumerates the <c>IObservableMap&lt;TKey, TValue&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIObservableMap2Types()
    {
        return CanonicalizeTypes(_discoveryState.IObservableMap2Types);
    }

    /// <summary>Enumerates the <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIAsyncActionWithProgress1Types()
    {
        return CanonicalizeTypes(_discoveryState.IAsyncActionWithProgress1Types);
    }

    /// <summary>Enumerates the <c>IAsyncOperation&lt;TResult&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIAsyncOperation1Types()
    {
        return CanonicalizeTypes(_discoveryState.IAsyncOperation1Types);
    }

    /// <summary>Enumerates the <c>IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIAsyncOperationWithProgress2Types()
    {
        return CanonicalizeTypes(_discoveryState.IAsyncOperationWithProgress2Types);
    }

    /// <summary>Enumerates the <see cref="IReadOnlyList{T}"/> instantiations of <see cref="KeyValuePair{TKey, TValue}"/> used for collection forwarders.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIReadOnlyCollectionKeyValuePair2Types()
    {
        IEnumerable<GenericInstanceTypeSignature> keyValuePairTypes = _discoveryState.IReadOnlyList1Types
            .Where(type => type.TypeArguments[0].IsConstructedKeyValuePairType(_interopReferences));

        return CanonicalizeTypes(keyValuePairTypes);
    }

    /// <summary>Enumerates the <see cref="IList{T}"/> instantiations of <see cref="KeyValuePair{TKey, TValue}"/> used for collection forwarders.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateICollectionKeyValuePair2Types()
    {
        IEnumerable<GenericInstanceTypeSignature> keyValuePairTypes = _discoveryState.IList1Types
            .Where(type => type.TypeArguments[0].IsConstructedKeyValuePairType(_interopReferences));

        return CanonicalizeTypes(keyValuePairTypes);
    }

    /// <summary>Enumerates Windows Runtime SZ arrays and their canonicalized interface sets.</summary>
    public IEnumerable<(SzArrayTypeSignature Type, TypeSignatureEquatableSet VtableTypes)> EnumerateSzArrayAndVtableTypes()
    {
        return SzArrayTypes;
    }

    /// <summary>Gets the shared user-defined vtable groups.</summary>
    /// <returns>The groups with stable representatives and ordered interface layouts.</returns>
    public IReadOnlyList<UserDefinedVtableGroup> GetUserDefinedVtableGroups()
    {
        return UserDefinedTypes.VtableGroups;
    }

    /// <summary>Enumerates user-defined proxy inputs in global canonical type order.</summary>
    /// <returns>Each type's own canonical interface set and its shared group.</returns>
    public IEnumerable<UserDefinedTypeInput> EnumerateUserDefinedAndVtableTypes()
    {
        return UserDefinedTypes.Types;
    }

    /// <summary>Prepares the shared groups and their proxy associations without changing either emission order.</summary>
    /// <returns>The ordered groups and globally ordered proxy inputs.</returns>
    private UserDefinedTypeInputs CreateUserDefinedTypeInputs()
    {
        // Finish importing forwarded names before formatting keys, so each key describes
        // the resolved identity that emission uses.
        (TypeSignature Type, TypeSignatureEquatableSet VtableTypes)[] imported =
            [.. _discoveryState.UserDefinedAndVtableTypes.Select(pair =>
            {
                _token.ThrowIfCancellationRequested();

                return (
                    Type: _importer.ImportTypeSignature(pair.Key),
                    VtableTypes: CanonicalizeVtableSet(pair.Value));
            })];

        // Identities are now stable, so share cached display names across type and interface ordering
        TypeDescriptorComparer comparer = TypeDescriptorComparer.CreateForEmission(_runtimeContext);

        // Compute each primary key once for 'OrderBy' and preserve the structural tie-break with 'ThenBy'
        IEnumerable<(TypeSignature Type, TypeSignatureEquatableSet VtableTypes)> orderedTypes = imported
            .Select(pair => (pair.Type, pair.VtableTypes, Key: comparer.GetOrderKey(pair.Type)))
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .ThenBy(static pair => pair.Type, comparer)
            .Select(static pair => (pair.Type, pair.VtableTypes));

        // Equal interface sets share generated marshallers. The identity map avoids rehashing
        // a shared canonical set for every user type, without treating distinct equal sets as different groups.
        Dictionary<TypeSignatureEquatableSet, UserDefinedVtableGroup> groupsBySet = [];
        Dictionary<TypeSignatureEquatableSet, UserDefinedVtableGroup> groupsByIdentity = new(ReferenceEqualityComparer.Instance);
        List<UserDefinedVtableGroup> groups = [];
        UserDefinedTypeInput[] types = new UserDefinedTypeInput[imported.Length];
        int index = 0;

        // The first type with a set names its shared generated types, so choose representatives
        // in global type order rather than parallel discovery order.
        foreach ((TypeSignature type, TypeSignatureEquatableSet vtableTypes) in orderedTypes)
        {
            _token.ThrowIfCancellationRequested();

            ref UserDefinedVtableGroup? identityGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(
                dictionary: groupsByIdentity,
                key: vtableTypes,
                exists: out _);

            if (identityGroup is null)
            {
                ref UserDefinedVtableGroup? semanticGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    dictionary: groupsBySet,
                    key: vtableTypes,
                    exists: out _);

                if (semanticGroup is null)
                {
                    semanticGroup = new(type, vtableTypes, comparer);
                    groups.Add(semanticGroup);
                }

                identityGroup = semanticGroup;
            }

            // Proxies keep global type order and their own interface set, while sharing the group's marshaller
            types[index++] = new(type, vtableTypes, identityGroup);
        }

        // Representatives are stable; independent groups need no ordering before parallel emission.
        return new(groups, types);
    }

    /// <summary>
    /// Enumerates discovered constructed types and mode-dependent custom mappings.
    /// </summary>
    /// <returns>The canonicalized types used to populate the external IID map.</returns>
    public IEnumerable<TypeSignature> EnumerateIidTypes()
    {
        IEnumerable<TypeSignature> types = [
            .. _discoveryState.GenericDelegateTypes,
            .. _discoveryState.IEnumerator1Types,
            .. _discoveryState.IEnumerable1Types,
            .. _discoveryState.IList1Types,
            .. _discoveryState.IReadOnlyList1Types,
            .. _discoveryState.IDictionary2Types,
            .. _discoveryState.IReadOnlyDictionary2Types,
            .. _discoveryState.KeyValuePairTypes,
            .. _discoveryState.IObservableVector1Types,
            .. _discoveryState.IObservableMap2Types,
            .. _discoveryState.IMapChangedEventArgs1Types,
            .. _discoveryState.IAsyncOperation1Types,
            .. _discoveryState.IAsyncActionWithProgress1Types,
            .. _discoveryState.IAsyncOperationWithProgress2Types,
            .. _discoveryState.SzArrayAndVtableTypes.Keys,

            // Fixed runtime IIDs are precompiled (only these custom mappings vary by XAML mode)
            _interopReferences.INotifyCollectionChanged.ToReferenceTypeSignature(),
            _interopReferences.INotifyPropertyChanged.ToReferenceTypeSignature(),
            _interopReferences.NotifyCollectionChangedEventArgs.ToReferenceTypeSignature(),
            _interopReferences.PropertyChangedEventArgs.ToReferenceTypeSignature(),
            _interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature(),
            _interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature(),
            _interopReferences.NotifyCollectionChangedAction.ToValueTypeSignature()];

        return CanonicalizeTypes(types);
    }

    /// <summary>Resolves aliases without imposing an emission order.</summary>
    /// <typeparam name="TSignature">The type of signature to enumerate.</typeparam>
    /// <param name="signatures">The discovered signatures.</param>
    /// <returns>The canonicalized signatures.</returns>
    private IEnumerable<TSignature> CanonicalizeTypes<TSignature>(IEnumerable<TSignature> signatures)
        where TSignature : TypeSignature
    {
        return signatures
            .Select(signature =>
            {
                _token.ThrowIfCancellationRequested();

                return (TSignature)_importer.ImportTypeSignature(signature);
            });
    }

    /// <summary>Reuses canonicalized interface sets by their original instance identity.</summary>
    /// <param name="original">The discovered set of interfaces.</param>
    /// <returns>The shared canonicalized set.</returns>
    private TypeSignatureEquatableSet CanonicalizeVtableSet(TypeSignatureEquatableSet original)
    {
        if (!_canonicalVtableSets.TryGetValue(original, out TypeSignatureEquatableSet? canonical))
        {
            canonical = new(_discoveryState.SignatureComparer, original.Select(_importer.ImportTypeSignature));

            _canonicalVtableSets.Add(original, canonical);
        }

        return canonical;
    }

    /// <summary>Resolves aliases in discovered types and their interface sets.</summary>
    /// <typeparam name="TSignature">The type of signature to enumerate.</typeparam>
    /// <param name="types">The discovered types and interface sets.</param>
    /// <returns>The canonicalized types and interface sets.</returns>
    private IEnumerable<(TSignature Type, TypeSignatureEquatableSet VtableTypes)> CanonicalizeTypesAndVtables<TSignature>(
        IReadOnlyDictionary<TSignature, TypeSignatureEquatableSet> types)
        where TSignature : TypeSignature
    {
        return types
            .Select(pair =>
            {
                _token.ThrowIfCancellationRequested();

                return (
                    Type: (TSignature)_importer.ImportTypeSignature(pair.Key),
                    VtableTypes: CanonicalizeVtableSet(pair.Value));
            });
    }

    /// <summary>
    /// Shares the prepared groups and proxy associations between both user-defined type emission passes.
    /// </summary>
    /// <param name="vtableGroups">The shared groups with stable representatives and interface layouts.</param>
    /// <param name="types">The globally ordered proxy inputs, retaining each type's own interface set.</param>
    private sealed class UserDefinedTypeInputs(
        IReadOnlyList<UserDefinedVtableGroup> vtableGroups,
        IReadOnlyList<UserDefinedTypeInput> types)
    {
        /// <summary>Gets the shared groups with stable representatives and interface layouts.</summary>
        public IReadOnlyList<UserDefinedVtableGroup> VtableGroups { get; } = vtableGroups;

        /// <summary>Gets the globally ordered types, their own canonical interface sets, and their shared groups.</summary>
        public IReadOnlyList<UserDefinedTypeInput> Types { get; } = types;
    }
}

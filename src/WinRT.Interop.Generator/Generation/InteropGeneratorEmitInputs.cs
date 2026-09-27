// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Ordered, output-bound signatures for the emit phase. Discovery may retain any of several
/// forwarded spellings of the same type, so they must be resolved before creating metadata.
/// </summary>
internal sealed class InteropGeneratorEmitInputs
{
    /// <summary>
    /// Creates ordered, canonicalized emit inputs from the completed discovery state.
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
        ReferenceImporter importer = module.DefaultImporter;
        RuntimeContext runtimeContext = module.RuntimeContext!;
        Dictionary<TypeSignatureEquatableSet, TypeSignatureEquatableSet> canonicalVtableSets = new(ReferenceEqualityComparer.Instance);

        ImmutableArray<TSignature> CanonicalizeTypes<TSignature>(IEnumerable<TSignature> signatures)
            where TSignature : TypeSignature
        {
            return signatures
                .Select(signature =>
                {
                    token.ThrowIfCancellationRequested();

                    return (TSignature)importer.ImportTypeSignature(signature);
                })
                .OrderByFullyQualifiedTypeName(runtimeContext)
                .ToImmutableArray();
        }

        TypeSignatureEquatableSet CanonicalizeVtableSet(TypeSignatureEquatableSet original)
        {
            if (!canonicalVtableSets.TryGetValue(original, out TypeSignatureEquatableSet? canonical))
            {
                canonical = new(discoveryState.SignatureComparer, original.Select(importer.ImportTypeSignature));
                canonicalVtableSets.Add(original, canonical);
            }

            return canonical;
        }

        ImmutableArray<(TSignature Type, TypeSignatureEquatableSet VtableTypes)> CanonicalizeTypesAndVtables<TSignature>(
            IReadOnlyDictionary<TSignature, TypeSignatureEquatableSet> types)
            where TSignature : TypeSignature
        {
            return types
                .Select(pair =>
                {
                    token.ThrowIfCancellationRequested();

                    return (
                        Type: (TSignature)importer.ImportTypeSignature(pair.Key),
                        VtableTypes: CanonicalizeVtableSet(pair.Value));
                })
                .OrderByFullyQualifiedTypeName(static pair => pair.Type, runtimeContext)
                .ToImmutableArray();
        }

        GenericDelegateTypes = CanonicalizeTypes(discoveryState.GenericDelegateTypes);
        IEnumerator1Types = CanonicalizeTypes(discoveryState.IEnumerator1Types);
        IEnumerable1Types = CanonicalizeTypes(discoveryState.IEnumerable1Types);
        IReadOnlyList1Types = CanonicalizeTypes(discoveryState.IReadOnlyList1Types);
        IList1Types = CanonicalizeTypes(discoveryState.IList1Types);
        IReadOnlyDictionary2Types = CanonicalizeTypes(discoveryState.IReadOnlyDictionary2Types);
        IDictionary2Types = CanonicalizeTypes(discoveryState.IDictionary2Types);
        KeyValuePairTypes = CanonicalizeTypes(discoveryState.KeyValuePairTypes);
        IMapChangedEventArgs1Types = CanonicalizeTypes(discoveryState.IMapChangedEventArgs1Types);
        IObservableVector1Types = CanonicalizeTypes(discoveryState.IObservableVector1Types);
        IObservableMap2Types = CanonicalizeTypes(discoveryState.IObservableMap2Types);
        IAsyncActionWithProgress1Types = CanonicalizeTypes(discoveryState.IAsyncActionWithProgress1Types);
        IAsyncOperation1Types = CanonicalizeTypes(discoveryState.IAsyncOperation1Types);
        IAsyncOperationWithProgress2Types = CanonicalizeTypes(discoveryState.IAsyncOperationWithProgress2Types);

        IReadOnlyCollectionKeyValuePair2Types =
            [.. IReadOnlyList1Types.Where(type => type.TypeArguments[0].IsConstructedKeyValuePairType(interopReferences))];
        ICollectionKeyValuePair2Types =
            [.. IList1Types.Where(type => type.TypeArguments[0].IsConstructedKeyValuePairType(interopReferences))];

        SzArrayAndVtableTypes = CanonicalizeTypesAndVtables(discoveryState.SzArrayAndVtableTypes);
        UserDefinedAndVtableTypes = CanonicalizeTypesAndVtables(discoveryState.UserDefinedAndVtableTypes);
    }

    /// <summary>Gets the ordered generic delegate instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> GenericDelegateTypes { get; }

    /// <summary>Gets the ordered <c>IEnumerator&lt;T&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IEnumerator1Types { get; }

    /// <summary>Gets the ordered <c>IEnumerable&lt;T&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IEnumerable1Types { get; }

    /// <summary>Gets the ordered <c>IReadOnlyList&lt;T&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IReadOnlyList1Types { get; }

    /// <summary>Gets the ordered <c>IList&lt;T&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IList1Types { get; }

    /// <summary>Gets the ordered <c>IReadOnlyDictionary&lt;TKey, TValue&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IReadOnlyDictionary2Types { get; }

    /// <summary>Gets the ordered <c>IDictionary&lt;TKey, TValue&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IDictionary2Types { get; }

    /// <summary>Gets the ordered <c>KeyValuePair&lt;TKey, TValue&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> KeyValuePairTypes { get; }

    /// <summary>Gets the ordered <c>IMapChangedEventArgs&lt;TKey&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IMapChangedEventArgs1Types { get; }

    /// <summary>Gets the ordered <c>IObservableVector&lt;T&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IObservableVector1Types { get; }

    /// <summary>Gets the ordered <c>IObservableMap&lt;TKey, TValue&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IObservableMap2Types { get; }

    /// <summary>Gets the ordered <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IAsyncActionWithProgress1Types { get; }

    /// <summary>Gets the ordered <c>IAsyncOperation&lt;TResult&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IAsyncOperation1Types { get; }

    /// <summary>Gets the ordered <c>IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> instantiations.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IAsyncOperationWithProgress2Types { get; }

    /// <summary>Gets the ordered <c>IReadOnlyList&lt;KeyValuePair&lt;TKey, TValue&gt;&gt;</c> instantiations used for collection forwarders.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> IReadOnlyCollectionKeyValuePair2Types { get; }

    /// <summary>Gets the ordered <c>IList&lt;KeyValuePair&lt;TKey, TValue&gt;&gt;</c> instantiations used for collection forwarders.</summary>
    public ImmutableArray<GenericInstanceTypeSignature> ICollectionKeyValuePair2Types { get; }

    /// <summary>Gets the ordered Windows Runtime SZ arrays and their canonicalized interface sets.</summary>
    public ImmutableArray<(SzArrayTypeSignature Type, TypeSignatureEquatableSet VtableTypes)> SzArrayAndVtableTypes { get; }

    /// <summary>Gets the ordered user-defined types and their shared canonicalized interface sets.</summary>
    public ImmutableArray<(TypeSignature Type, TypeSignatureEquatableSet VtableTypes)> UserDefinedAndVtableTypes { get; }
}

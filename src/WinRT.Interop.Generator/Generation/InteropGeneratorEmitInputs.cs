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

    public ImmutableArray<GenericInstanceTypeSignature> GenericDelegateTypes { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IEnumerator1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IEnumerable1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IReadOnlyList1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IList1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IReadOnlyDictionary2Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IDictionary2Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> KeyValuePairTypes { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IMapChangedEventArgs1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IObservableVector1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IObservableMap2Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IAsyncActionWithProgress1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IAsyncOperation1Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IAsyncOperationWithProgress2Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> IReadOnlyCollectionKeyValuePair2Types { get; }

    public ImmutableArray<GenericInstanceTypeSignature> ICollectionKeyValuePair2Types { get; }

    public ImmutableArray<(SzArrayTypeSignature Type, TypeSignatureEquatableSet VtableTypes)> SzArrayAndVtableTypes { get; }

    public ImmutableArray<(TypeSignature Type, TypeSignatureEquatableSet VtableTypes)> UserDefinedAndVtableTypes { get; }
}

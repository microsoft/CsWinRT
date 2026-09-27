// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Lazily enumerates ordered, output-bound signatures for the emit phase. Discovery may retain
/// any of several forwarded spellings of the same type, so they must be resolved before creating metadata.
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

    /// <summary>Enumerates the ordered generic delegate instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateGenericDelegateTypes()
    {
        return CanonicalizeTypes(_discoveryState.GenericDelegateTypes);
    }

    /// <summary>Enumerates the ordered <see cref="IEnumerator{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIEnumerator1Types()
    {
        return CanonicalizeTypes(_discoveryState.IEnumerator1Types);
    }

    /// <summary>Enumerates the ordered <see cref="IEnumerable{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIEnumerable1Types()
    {
        return CanonicalizeTypes(_discoveryState.IEnumerable1Types);
    }

    /// <summary>Enumerates the ordered <see cref="IReadOnlyList{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIReadOnlyList1Types()
    {
        return CanonicalizeTypes(_discoveryState.IReadOnlyList1Types);
    }

    /// <summary>Enumerates the ordered <see cref="IList{T}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIList1Types()
    {
        return CanonicalizeTypes(_discoveryState.IList1Types);
    }

    /// <summary>Enumerates the ordered <see cref="IReadOnlyDictionary{TKey, TValue}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIReadOnlyDictionary2Types()
    {
        return CanonicalizeTypes(_discoveryState.IReadOnlyDictionary2Types);
    }

    /// <summary>Enumerates the ordered <see cref="IDictionary{TKey, TValue}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIDictionary2Types()
    {
        return CanonicalizeTypes(_discoveryState.IDictionary2Types);
    }

    /// <summary>Enumerates the ordered <see cref="KeyValuePair{TKey, TValue}"/> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateKeyValuePairTypes()
    {
        return CanonicalizeTypes(_discoveryState.KeyValuePairTypes);
    }

    /// <summary>Enumerates the ordered <c>IMapChangedEventArgs&lt;TKey&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIMapChangedEventArgs1Types()
    {
        return CanonicalizeTypes(_discoveryState.IMapChangedEventArgs1Types);
    }

    /// <summary>Enumerates the ordered <c>IObservableVector&lt;T&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIObservableVector1Types()
    {
        return CanonicalizeTypes(_discoveryState.IObservableVector1Types);
    }

    /// <summary>Enumerates the ordered <c>IObservableMap&lt;TKey, TValue&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIObservableMap2Types()
    {
        return CanonicalizeTypes(_discoveryState.IObservableMap2Types);
    }

    /// <summary>Enumerates the ordered <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIAsyncActionWithProgress1Types()
    {
        return CanonicalizeTypes(_discoveryState.IAsyncActionWithProgress1Types);
    }

    /// <summary>Enumerates the ordered <c>IAsyncOperation&lt;TResult&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIAsyncOperation1Types()
    {
        return CanonicalizeTypes(_discoveryState.IAsyncOperation1Types);
    }

    /// <summary>Enumerates the ordered <c>IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> instantiations.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIAsyncOperationWithProgress2Types()
    {
        return CanonicalizeTypes(_discoveryState.IAsyncOperationWithProgress2Types);
    }

    /// <summary>Enumerates the ordered <see cref="IReadOnlyList{T}"/> instantiations of <see cref="KeyValuePair{TKey, TValue}"/> used for collection forwarders.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateIReadOnlyCollectionKeyValuePair2Types()
    {
        return CanonicalizeTypes(_discoveryState.IReadOnlyList1Types
            .Where(type => type.TypeArguments[0].IsConstructedKeyValuePairType(_interopReferences)));
    }

    /// <summary>Enumerates the ordered <see cref="IList{T}"/> instantiations of <see cref="KeyValuePair{TKey, TValue}"/> used for collection forwarders.</summary>
    public IEnumerable<GenericInstanceTypeSignature> EnumerateICollectionKeyValuePair2Types()
    {
        return CanonicalizeTypes(_discoveryState.IList1Types
            .Where(type => type.TypeArguments[0].IsConstructedKeyValuePairType(_interopReferences)));
    }

    /// <summary>Enumerates the ordered Windows Runtime SZ arrays and their canonicalized interface sets.</summary>
    public IEnumerable<(SzArrayTypeSignature Type, TypeSignatureEquatableSet VtableTypes)> EnumerateSzArrayAndVtableTypes()
    {
        return CanonicalizeTypesAndVtables(_discoveryState.SzArrayAndVtableTypes);
    }

    /// <summary>Enumerates the ordered user-defined types and their shared canonicalized interface sets.</summary>
    public IEnumerable<(TypeSignature Type, TypeSignatureEquatableSet VtableTypes)> EnumerateUserDefinedAndVtableTypes()
    {
        return CanonicalizeTypesAndVtables(_discoveryState.UserDefinedAndVtableTypes);
    }

    /// <summary>Resolves aliases and orders signatures by fully qualified type name.</summary>
    /// <typeparam name="TSignature">The type of signature to enumerate.</typeparam>
    /// <param name="signatures">The discovered signatures.</param>
    /// <returns>The canonicalized signatures in stable order.</returns>
    private IEnumerable<TSignature> CanonicalizeTypes<TSignature>(IEnumerable<TSignature> signatures)
        where TSignature : TypeSignature
    {
        return signatures
            .Select(signature =>
            {
                _token.ThrowIfCancellationRequested();

                return (TSignature)_importer.ImportTypeSignature(signature);
            })
            .OrderByFullyQualifiedTypeName(_runtimeContext);
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

    /// <summary>Resolves aliases in discovered types and their interface sets, then orders the types.</summary>
    /// <typeparam name="TSignature">The type of signature to enumerate.</typeparam>
    /// <param name="types">The discovered types and interface sets.</param>
    /// <returns>The canonicalized types and interface sets in stable order.</returns>
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
            })
            .OrderByFullyQualifiedTypeName(static pair => pair.Type, _runtimeContext);
    }
}

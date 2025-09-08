﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Global state tracking type for <see cref="InteropGenerator"/>, specifically for the discovery phase.
/// </summary>
internal sealed class InteropGeneratorDiscoveryState
{
    /// <summary>Backing field for <see cref="ModuleDefinitions"/>.</summary>
    private readonly ConcurrentDictionary<string, ModuleDefinition> _moduleDefinitions = [];

    /// <summary>Backing field for <see cref="TypeHierarchyEntries"/>.</summary>
    private readonly ConcurrentDictionary<string, string> _typeHierarchyEntries = [];

    /// <summary>Backing field for <see cref="IEnumerator1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ienumerator1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IEnumerable1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ienumerable1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IEnumerable1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ilist1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IEnumerable1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ireadOnlyList1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IEnumerable1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _idictionary2Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IEnumerable1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ireadOnlyDictionary2Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="GenericDelegateTypes"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _genericDelegateTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="KeyValuePairTypes"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _keyValuePairTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="KeyValuePairTypes"/>.</summary>
    private readonly ConcurrentDictionary<SzArrayTypeSignature, byte> _szArrayTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>
    /// Indicates whether the current state is readonly.
    /// </summary>
    private volatile bool _isReadOnly;

    /// <summary>
    /// Indicates whether any of the loaded modules reference the WinRT runtime .dll version 2.
    /// </summary>
    private volatile bool _hasWinRTRuntimeDllVersion2References;

    /// <summary>
    /// Gets the assembly resolver to use for loading assemblies.
    /// </summary>
    public required IAssemblyResolver AssemblyResolver { get; init; }

    /// <summary>
    /// Gets the loaded modules.
    /// </summary>
    public IReadOnlyDictionary<string, ModuleDefinition> ModuleDefinitions => _moduleDefinitions;

    /// <summary>
    /// Gets the type hierarchy entries.
    /// </summary>
    public IReadOnlyDictionary<string, string> TypeHierarchyEntries => _typeHierarchyEntries;

    /// <summary>
    /// Gets all <see cref="IEnumerator{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IEnumerator1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ienumerator1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IEnumerable{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IEnumerable1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ienumerable1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IList{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IList1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ilist1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IReadOnlyList{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IReadOnlyList1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ireadOnlyList1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IDictionary{TKey, TValue}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IDictionary2Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_idictionary2Types.Keys;

    /// <summary>
    /// Gets all <see cref="IReadOnlyDictionary{TKey, TValue}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IReadOnlyDictionary2Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ireadOnlyDictionary2Types.Keys;

    /// <summary>
    /// Gets all generic delegate types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> GenericDelegateTypes => (IReadOnlyCollection<GenericInstanceTypeSignature>)_genericDelegateTypes.Keys;

    /// <summary>
    /// Gets all <see cref="KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> KeyValuePairTypes => (IReadOnlyCollection<GenericInstanceTypeSignature>)_keyValuePairTypes.Keys;

    /// <summary>
    /// Gets all SZ array types.
    /// </summary>
    public IReadOnlyCollection<SzArrayTypeSignature> SzArrayTypes => (IReadOnlyCollection<SzArrayTypeSignature>)_szArrayTypes.Keys;

    /// <summary>
    /// Gets whether any of the loaded modules reference the WinRT runtime .dll version 2.
    /// </summary>
    public bool HasWinRTRuntimeDllVersion2References => _hasWinRTRuntimeDllVersion2References;

    /// <summary>
    /// Tracks a loaded module definition.
    /// </summary>
    /// <param name="path">The assembly path.</param>
    /// <param name="module">The loaded module.</param>
    public void TrackModuleDefinition(string path, ModuleDefinition module)
    {
        ThrowIfReadOnly();

        _ = _moduleDefinitions.TryAdd(path, module);
    }

    /// <summary>
    /// Tracks a pair of runtime class names for the type hierarchy.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name to use as key.</param>
    /// <param name="baseRuntimeClassName">The runtime class name of the base type.</param>
    public void TrackTypeHierarchyEntry(string runtimeClassName, string baseRuntimeClassName)
    {
        ThrowIfReadOnly();

        _ = _typeHierarchyEntries.TryAdd(runtimeClassName, baseRuntimeClassName);
    }

    /// <summary>
    /// Tracks a <see cref="IEnumerator{T}"/> type.
    /// </summary>
    /// <param name="enumeratorType">The <see cref="IEnumerator{T}"/> type.</param>
    public void TrackIEnumerator1Type(GenericInstanceTypeSignature enumeratorType)
    {
        ThrowIfReadOnly();

        _ = _ienumerator1Types.TryAdd(enumeratorType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IEnumerable{T}"/> type.
    /// </summary>
    /// <param name="enumerableType">The <see cref="IEnumerable{T}"/> type.</param>
    public void TrackIEnumerable1Type(GenericInstanceTypeSignature enumerableType)
    {
        ThrowIfReadOnly();

        _ = _ienumerable1Types.TryAdd(enumerableType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IList{T}"/> type.
    /// </summary>
    /// <param name="listType">The <see cref="IList{T}"/> type.</param>
    public void TrackIList1Type(GenericInstanceTypeSignature listType)
    {
        ThrowIfReadOnly();

        _ = _ilist1Types.TryAdd(listType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IReadOnlyList{T}"/> type.
    /// </summary>
    /// <param name="listType">The <see cref="IReadOnlyList{T}"/> type.</param>
    public void TrackIReadOnlyList1Type(GenericInstanceTypeSignature listType)
    {
        ThrowIfReadOnly();

        _ = _ireadOnlyList1Types.TryAdd(listType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IDictionary{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="dictionaryType">The <see cref="IDictionary{TKey, TValue}"/> type.</param>
    public void TrackIDictionary2Type(GenericInstanceTypeSignature dictionaryType)
    {
        ThrowIfReadOnly();

        _ = _idictionary2Types.TryAdd(dictionaryType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IReadOnlyDictionary{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="dictionaryType">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> type.</param>
    public void TrackIReadOnlyDictionary2Type(GenericInstanceTypeSignature dictionaryType)
    {
        ThrowIfReadOnly();

        _ = _ireadOnlyDictionary2Types.TryAdd(dictionaryType, 0);
    }

    /// <summary>
    /// Tracks a generic delegate type.
    /// </summary>
    /// <param name="delegateType">The delegate type.</param>
    public void TrackGenericDelegateType(GenericInstanceTypeSignature delegateType)
    {
        ThrowIfReadOnly();

        _ = _genericDelegateTypes.TryAdd(delegateType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="keyValuePairType">The <see cref="KeyValuePair{TKey, TValue}"/> type.</param>
    public void TrackKeyValuePairType(GenericInstanceTypeSignature keyValuePairType)
    {
        ThrowIfReadOnly();

        _ = _keyValuePairTypes.TryAdd(keyValuePairType, 0);
    }

    /// <summary>
    /// Tracks a SZ array type.
    /// </summary>
    /// <param name="szArrayType">The SZ array type.</param>
    public void TrackSzArrayType(SzArrayTypeSignature szArrayType)
    {
        ThrowIfReadOnly();

        _ = _szArrayTypes.TryAdd(szArrayType, 0);
    }

    /// <summary>
    /// Marks the current state as readonly.
    /// </summary>
    public void MakeReadOnly()
    {
        _isReadOnly = true;
    }

    /// <summary>
    /// Marks the state as having references to the WinRT runtime .dll version 2.
    /// </summary>
    public void MarkWinRTRuntimeDllVersion2References()
    {
        ThrowIfReadOnly();

        _hasWinRTRuntimeDllVersion2References = true;
    }

    /// <summary>
    /// Throws if <see cref="MakeReadOnly"/> was called.
    /// </summary>
    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
        {
            throw WellKnownInteropExceptions.StateChangeAfterMakeReadOnly();
        }
    }
}

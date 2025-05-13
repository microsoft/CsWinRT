// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Global state tracking type for <see cref="InteropGenerator"/>.
/// </summary>
internal sealed class InteropGeneratorState
{
    /// <summary>Backing field for <see cref="ModuleDefinitions"/>.</summary>
    private readonly ConcurrentDictionary<string, ModuleDefinition> _moduleDefinitions = [];

    /// <summary>Backing field for <see cref="TypeHierarchyEntries"/>.</summary>
    private readonly ConcurrentDictionary<string, string> _typeHierarchyEntries = [];

    /// <summary>Backing field for <see cref="GenericDelegateTypes"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _genericDelegateTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="KeyValuePairTypes"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _keyValuePairTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>
    /// Indicates whether the current state is readonly.
    /// </summary>
    private volatile bool _isReadOnly;

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
    /// Gets all generic delegate types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> GenericDelegateTypes => (IReadOnlyCollection<GenericInstanceTypeSignature>)_genericDelegateTypes.Keys;

    /// <summary>
    /// Gets all <see cref="KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> KeyValuePairTypes => (IReadOnlyCollection<GenericInstanceTypeSignature>)_keyValuePairTypes.Keys;

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
    /// Marks the current state as readonly.
    /// </summary>
    public void MakeReadOnly()
    {
        _isReadOnly = true;
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

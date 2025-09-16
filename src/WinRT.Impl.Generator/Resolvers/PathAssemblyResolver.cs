// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ImplGenerator.Resolvers;

/// <summary>
/// A custom <see cref="IAssemblyResolver"/> from a specific set of reference paths.
/// </summary>
internal sealed class PathAssemblyResolver : IAssemblyResolver
{
    /// <summary>
    /// The input .dll paths to load assemblies from.
    /// </summary>
    private readonly string[] _referencePath;

    /// <summary>
    /// The cached assemblies.
    /// </summary>
    private readonly ConcurrentDictionary<AssemblyDescriptor, AssemblyDefinition> _cache = new(SignatureComparer.IgnoreVersion);

    /// <summary>
    /// Creates a new <see cref="PathAssemblyResolver"/> instance with the specified parameters.
    /// </summary>
    /// <param name="referencePath">The input .dll paths.</param>
    public PathAssemblyResolver(string[] referencePath)
    {
        _referencePath = referencePath;
        ReaderParameters = new RuntimeContext(new DotNetRuntimeInfo(".NETCoreApp", new Version(10, 0)), this).DefaultReaderParameters;
    }

    /// <summary>
    /// Gets the <see cref="ModuleReaderParameters"/> instance to use.
    /// </summary>
    public ModuleReaderParameters ReaderParameters { get; }

    /// <inheritdoc/>
    public AssemblyDefinition? Resolve(AssemblyDescriptor assembly)
    {
        // If we already have the assembly in the cache, return it
        if (_cache.TryGetValue(assembly, out AssemblyDefinition? cachedDefinition))
        {
            return cachedDefinition;
        }

        // We can't load an assembly without a name
        if (assembly.Name is null)
        {
            return null;
        }

        // Find the first match in our list of reference paths, and load that assembly
        foreach (string path in _referencePath)
        {
            if (Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual(assembly.Name))
            {
                return _cache.GetOrAdd(
                    key: assembly,
                    valueFactory: static (_, args) => AssemblyDefinition.FromFile(args.Path, args.Parameters),
                    factoryArgument: (Path: path, Parameters: ReaderParameters));
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public void AddToCache(AssemblyDescriptor descriptor, AssemblyDefinition definition)
    {
        _ = _cache.TryAdd(descriptor, definition);
    }

    /// <inheritdoc/>
    public bool RemoveFromCache(AssemblyDescriptor descriptor)
    {
        return _cache.TryRemove(descriptor, out _);
    }

    /// <inheritdoc/>
    public bool HasCached(AssemblyDescriptor descriptor)
    {
        return _cache.ContainsKey(descriptor);
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        _cache.Clear();
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.WinMDGenerator.Resolvers;

/// <summary>
/// A custom <see cref="IAssemblyResolver"/> from a specific set of reference paths.
/// </summary>
internal sealed class PathAssemblyResolver : IAssemblyResolver
{
    /// <summary>
    /// The input .dll paths to load assemblies from.
    /// </summary>
    private readonly string[] _referencePaths;

    /// <summary>
    /// The cached assemblies.
    /// </summary>
    private readonly ConcurrentDictionary<AssemblyDescriptor, AssemblyDefinition> _cache = new(new SignatureComparer());

    /// <summary>
    /// Creates a new <see cref="PathAssemblyResolver"/> instance with the specified parameters.
    /// </summary>
    /// <param name="referencePaths">The input .dll paths.</param>
    public PathAssemblyResolver(string[] referencePaths)
    {
        _referencePaths = referencePaths;
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
        foreach (string path in _referencePaths)
        {
            if (Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual(assembly.Name))
            {
                return _cache.GetOrAdd(
                    key: assembly,
                    valueFactory: static (_, args) => AssemblyDefinition.FromFile(args.Path, args.Parameters),
                    factoryArgument: (Path: path, Parameters: ReaderParameters));
            }
        }

        // Fallback: search sibling directories of existing reference paths.
        // This handles type forwarding scenarios where the forwarder assembly (e.g., Microsoft.Windows.SDK.NET)
        // forwards to an assembly (e.g., WinRT.Sdk.Projection) that lives in a sibling directory.
        string targetFileName = assembly.Name + ".dll";
        foreach (string path in _referencePaths)
        {
            string? dir = Path.GetDirectoryName(path);
            if (dir is null)
            {
                continue;
            }

            string candidate = Path.Combine(dir, targetFileName);
            if (File.Exists(candidate))
            {
                return _cache.GetOrAdd(
                    key: assembly,
                    valueFactory: static (_, args) => AssemblyDefinition.FromFile(args.Path, args.Parameters),
                    factoryArgument: (Path: candidate, Parameters: ReaderParameters));
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
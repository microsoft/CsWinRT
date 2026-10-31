// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// The reference assemblies of a <see cref="ProjectionGenerator"/> invocation, loaded once so they can be
/// inspected early (e.g. to know which <c>.winmd</c> files are processed) and then reused by later phases.
/// </summary>
internal sealed class ReferenceAssemblySet
{
    /// <summary>
    /// The lookup over <see cref="ComponentAssemblyNames"/> that allows matching file names without allocating strings.
    /// </summary>
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _componentAssemblyNamesLookup;

    /// <summary>
    /// Creates a new <see cref="ReferenceAssemblySet"/> instance with the specified parameters.
    /// </summary>
    /// <param name="resolver">The resolver the reference assemblies were loaded with.</param>
    /// <param name="assemblies">The loaded reference assemblies, in the order they were provided.</param>
    public ReferenceAssemblySet(PathAssemblyResolver resolver, IReadOnlyList<ReferenceAssembly> assemblies)
    {
        Resolver = resolver;
        Assemblies = assemblies;

        // Collect the names of the assemblies marked as components, which are the ones with an authored '.winmd'
        HashSet<string> componentAssemblyNames = new(
            collection: assemblies
                .Where(static assembly => assembly.IsComponent && assembly.Name is not null)
                .Select(static assembly => assembly.Name!),
            comparer: StringComparer.Ordinal);

        ComponentAssemblyNames = componentAssemblyNames;

        _componentAssemblyNamesLookup = componentAssemblyNames.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    /// <summary>
    /// Gets the resolver the reference assemblies were loaded with.
    /// </summary>
    public PathAssemblyResolver Resolver { get; }

    /// <summary>
    /// Gets the loaded reference assemblies, in the order they were provided.
    /// </summary>
    public IReadOnlyList<ReferenceAssembly> Assemblies { get; }

    /// <summary>
    /// Gets the simple names of all reference assemblies annotated with <c>[WindowsRuntimeComponentAssembly]</c>.
    /// </summary>
    public IReadOnlySet<string> ComponentAssemblyNames { get; }

    /// <summary>
    /// Checks whether a <c>.winmd</c> file is the metadata of an authored component, i.e. it is named after
    /// one of the reference assemblies annotated with <c>[WindowsRuntimeComponentAssembly]</c> (e.g. <c>MyComponent.winmd</c>).
    /// </summary>
    /// <param name="winmdPath">The path to the <c>.winmd</c> file.</param>
    /// <returns>Whether <paramref name="winmdPath"/> is the metadata of an authored component.</returns>
    public bool IsComponentWinMD(ReadOnlySpan<char> winmdPath)
    {
        return _componentAssemblyNamesLookup.Contains(Path.GetFileNameWithoutExtension(winmdPath));
    }
}

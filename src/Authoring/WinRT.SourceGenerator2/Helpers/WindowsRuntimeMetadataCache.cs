// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Caches the Windows Runtime metadata read from a compilation, so it's shared by all the calls analyzed against that compilation.
/// </summary>
/// <remarks>
/// <para>
/// Every intercepted call is analyzed independently, but they all consult the same assemblies. Reading the metadata of a large reference
/// projection (which can have thousands of entries) for each call is expensive, so this type holds everything that is derived from an
/// assembly or a type definition (and not from a specific call), and is only computed the first time it's actually needed.
/// </para>
/// <para>
/// Instances are tied to a <see cref="Compilation"/> (and dropped with it), as the cached symbols belong to it. Values are computed with
/// the cancellation token of whichever call asks first, so no token is stored: if a computation is canceled, nothing is cached and the
/// next call simply computes it again. Calls for the same compilation don't normally run concurrently, but all members are thread-safe anyway.
/// </para>
/// <para>
/// Everything is resolved against the assembly defining the marshalling API. A call is only analyzed if it binds to the single assembly
/// defining it in the compilation, so there is exactly one for all the calls sharing a cache.
/// </para>
/// </remarks>
internal sealed class WindowsRuntimeMetadataCache
{
    /// <summary>
    /// The shared caches, one for each compilation. The cache is dropped when its compilation is, so symbols are never retained.
    /// </summary>
    private static readonly ConditionalWeakTable<Compilation, WindowsRuntimeMetadataCache> Caches = [];

    /// <summary>
    /// Gets the cache to use for a given compilation.
    /// </summary>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns>The cache shared by all calls analyzed against <paramref name="compilation"/>.</returns>
    public static WindowsRuntimeMetadataCache GetOrCreate(Compilation compilation)
    {
        return Caches.GetOrAdd(compilation, static _ => new WindowsRuntimeMetadataCache());
    }

    /// <summary>
    /// Gets the cached kind of each assembly (whether it's a reference projection or an authored component).
    /// </summary>
    public ConcurrentDictionary<IAssemblySymbol, ProjectionInfo> ProjectionInfos { get; } = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Gets the cached default interface names recorded by each reference projection, which can be large and are only needed for runtime classes.
    /// </summary>
    public ConcurrentDictionary<IAssemblySymbol, IReadOnlyDictionary<string, string>> DefaultInterfaceNames { get; } = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Gets the cached framework definitions, including type forwards (and the names that couldn't be found).
    /// </summary>
    public ConcurrentDictionary<string, INamedTypeSymbol?> FrameworkDefinitions { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the cached default interface of each runtime class (and the classes that don't have a supported one).
    /// </summary>
    public ConcurrentDictionary<INamedTypeSymbol, INamedTypeSymbol?> DefaultInterfaces { get; } = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Reference-only metadata for one assembly.
    /// </summary>
    /// <param name="IsProjection">Whether the assembly contains reference projections.</param>
    /// <param name="IsComponent">Whether the assembly is an authored component.</param>
    public sealed record ProjectionInfo(bool IsProjection, bool IsComponent);
}

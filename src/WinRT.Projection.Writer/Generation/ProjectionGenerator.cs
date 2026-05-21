// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Generation.WorkItems;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <summary>
/// Orchestrates the projection generation: discovers component-activatable types, walks each
/// namespace in the metadata cache and emits the per-namespace <c>.cs</c> files, then writes
/// the per-projection support files (default-interfaces map, exclusive-to map, base resources).
/// </summary>
/// <remarks>
/// <para>
/// Work is split into discrete <see cref="IProjectionWorkItem"/> units (one per namespace, plus
/// the global <c>GeneratedInterfaceIIDs.cs</c> file and -- in component mode -- the
/// <c>WinRT_Module.cs</c> activation-factory aggregator). Items are dispatched in parallel via
/// <see cref="Parallel.ForEach{TSource}(IEnumerable{TSource}, ParallelOptions, System.Action{TSource})"/>
/// with the configured <see cref="Settings.MaxDegreesOfParallelism"/>; cross-item shared state
/// lives in <see cref="ProjectionGeneratorRunState"/> and uses <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// / <see cref="ConcurrentBag{T}"/> / <see cref="Interlocked"/> so the per-item bodies can run
/// without explicit locking.
/// </para>
/// <para>
/// Discovery (component activation lookups) and post-processing (default-interfaces table,
/// exclusive-to-interfaces table, base-resource emission) remain sequential -- the former
/// because it produces the read-only state every work item depends on, the latter because they
/// consume the accumulated post-loop state.
/// </para>
/// </remarks>
/// <param name="settings">The active projection settings.</param>
/// <param name="cache">The metadata cache built from the input <c>.winmd</c> files.</param>
/// <param name="token">The cancellation token observed across all phases.</param>
internal sealed partial class ProjectionGenerator(Settings settings, MetadataCache cache, CancellationToken token)
{
    private readonly Settings _settings = settings;
    private readonly MetadataCache _cache = cache;
    private readonly CancellationToken _token = token;

    /// <summary>
    /// Runs the projection-generation pipeline end-to-end.
    /// </summary>
    public void Run()
    {
        HashSet<TypeDefinition> componentActivatable;
        Dictionary<string, HashSet<TypeDefinition>> componentByModule;

        // Phase 1: discover the activatable runtime classes (component mode only).
        try
        {
            (componentActivatable, componentByModule) = DiscoverComponentActivatableTypes();
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionWriterException("discovery", e);
        }

        _token.ThrowIfCancellationRequested();

        ProjectionGeneratorRunState state = new(componentActivatable, componentByModule);

        // Phase 3..6: parallel emission. All file writes happen below; wrap the whole emission
        // pipeline in a single try/catch so any unexpected failure surfaces as an
        // UnhandledProjectionWriterException rather than a raw stack trace.
        try
        {
            if (_settings.Verbose)
            {
                Action<string> log = _settings.Logger ?? Console.Out.WriteLine;

                foreach (string p in _settings.Input)
                {
                    log($"input: {p}");
                }

                log($"output: {_settings.OutputFolder}");
            }

            ParallelOptions parallelOptions = new()
            {
                CancellationToken = _token,
                MaxDegreeOfParallelism = _settings.MaxDegreesOfParallelism,
            };

            ParallelLoopResult result = Parallel.ForEach(
                source: EnumerateWorkItems(state),
                parallelOptions: parallelOptions,
                body: static item => item.Execute());

            // Defensive: should always be true (no break/stop in the body), but matches the
            // interop generator's pattern.
            if (!result.IsCompleted)
            {
                throw WellKnownProjectionWriterExceptions.WorkItemLoopDidNotComplete();
            }

            if (state.DefaultInterfaceEntries.Count > 0 && !_settings.ReferenceProjection)
            {
                List<KeyValuePair<string, string>> sorted = [.. state.DefaultInterfaceEntries];
                sorted.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
                MetadataAttributeFactory.WriteDefaultInterfacesClass(_settings, sorted);
            }

            if (!state.ExclusiveToInterfaceEntries.IsEmpty && _settings.Component && !_settings.ReferenceProjection)
            {
                List<KeyValuePair<string, string>> sorted = [.. state.ExclusiveToInterfaceEntries];
                sorted.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
                MetadataAttributeFactory.WriteExclusiveToInterfacesClass(_settings, sorted);
            }

            if (state.ProjectionFileWritten)
            {
                WriteBaseStrings();
            }
        }
        catch (AggregateException e)
        {
            Exception innerException = e.InnerExceptions.FirstOrDefault()!;

            // If the first inner exception is well known, just rethrow it.
            // We're not concerned about always throwing the same one across
            // re-runs with parallelism. It can be disabled for debugging.
            throw innerException.IsWellKnown
                ? innerException
                : WellKnownProjectionWriterExceptions.WorkItemLoopError(innerException);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionWriterException("emit", e);
        }
    }

    /// <summary>
    /// Enumerates the work items dispatched in parallel by <see cref="Run"/>: the global
    /// <c>GeneratedInterfaceIIDs.cs</c> file (skipped in reference-projection mode), one item
    /// per namespace in the metadata cache, and -- in component mode -- the
    /// <c>WinRT_Module.cs</c> file. The component module item is yielded last so the other two
    /// item kinds get a chance to start before the smaller (typically) component item picks up
    /// any remaining slot.
    /// </summary>
    /// <param name="state">The shared run state passed to per-item factories.</param>
    /// <returns>The lazy work-item sequence.</returns>
    private IEnumerable<IProjectionWorkItem> EnumerateWorkItems(ProjectionGeneratorRunState state)
    {
        if (!_settings.ReferenceProjection)
        {
            yield return new IidsWorkItem(this);
        }

        foreach ((string ns, NamespaceMembers members) in _cache.Namespaces)
        {
            yield return new NamespaceWorkItem(this, ns, members, state);
        }

        if (_settings.Component)
        {
            yield return new ComponentModuleWorkItem(this, state);
        }
    }

    /// <summary>
    /// Adds the factory interfaces (Static / Activatable / Composable) referenced by
    /// <paramref name="classType"/> to <paramref name="result"/>. Used by both the global
    /// <c>GeneratedInterfaceIIDs.cs</c> walk and the per-namespace ABI-emission walk.
    /// </summary>
    /// <param name="classType">The runtime class definition to scan.</param>
    /// <param name="result">The set the factory interface definitions are added to.</param>
    private void AddFactoryInterfacesForClass(TypeDefinition classType, HashSet<TypeDefinition> result)
    {
        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, _cache))
        {
            TypeDefinition? facType = kv.Value.Type;

            if (facType is not null)
            {
                _ = result.Add(facType);
            }
        }
    }
}

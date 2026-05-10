// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <summary>
/// Orchestrates the projection generation: discovers component-activatable types, walks each
/// namespace in the metadata cache and emits the per-namespace <c>.cs</c> files, then writes
/// the per-projection support files (default-interfaces map, exclusive-to map, base resources).
/// </summary>
internal sealed partial class ProjectionGenerator
{
    private readonly Settings _settings;
    private readonly MetadataCache _cache;
    private readonly CancellationToken _token;

    /// <summary>
    /// Initializes a new <see cref="ProjectionGenerator"/>.
    /// </summary>
    /// <param name="settings">The active projection settings.</param>
    /// <param name="cache">The metadata cache built from the input <c>.winmd</c> files.</param>
    /// <param name="token">The cancellation token observed across all phases.</param>
    public ProjectionGenerator(Settings settings, MetadataCache cache, CancellationToken token)
    {
        _settings = settings;
        _cache = cache;
        _token = token;
    }

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

        // Phase 2..6: emission. All file writes happen below; wrap the whole emission
        // pipeline in a single try/catch so any unexpected failure surfaces as an
        // UnhandledProjectionWriterException rather than a raw stack trace.
        try
        {
            if (_settings.Verbose)
            {
                foreach (string p in _settings.Input)
                {
                    Console.Out.WriteLine($"input: {p}");
                }
                Console.Out.WriteLine($"output: {_settings.OutputFolder}");
            }

            WriteGeneratedInterfaceIIDsFile();

            ConcurrentDictionary<string, string> defaultInterfaceEntries = [];
            ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries = [];
            ConcurrentDictionary<string, string> authoredTypeNameToMetadataMap = [];
            bool projectionFileWritten = false;

            foreach ((string ns, NamespaceMembers members) in _cache.Namespaces)
            {
                _token.ThrowIfCancellationRequested();
                bool wrote = ProcessNamespace(ns, members, componentActivatable, defaultInterfaceEntries, exclusiveToInterfaceEntries, authoredTypeNameToMetadataMap);
                if (wrote)
                {
                    projectionFileWritten = true;
                }
            }

            if (_settings.Component)
            {
                WriteComponentModuleFile(componentByModule);
                projectionFileWritten = true;
            }

            if (defaultInterfaceEntries.Count > 0 && !_settings.ReferenceProjection)
            {
                List<KeyValuePair<string, string>> sorted = [.. defaultInterfaceEntries];
                sorted.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
                MetadataAttributeFactory.WriteDefaultInterfacesClass(_settings, sorted);
            }

            if (!exclusiveToInterfaceEntries.IsEmpty && _settings.Component && !_settings.ReferenceProjection)
            {
                List<KeyValuePair<string, string>> sorted = [.. exclusiveToInterfaceEntries];
                sorted.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
                MetadataAttributeFactory.WriteExclusiveToInterfacesClass(_settings, sorted);
            }

            if (projectionFileWritten)
            {
                WriteBaseStrings();
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionWriterException("emit", e);
        }
    }
}

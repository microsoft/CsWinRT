// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
namespace WindowsRuntime.ProjectionWriter.Generation;

/// <summary>
/// Orchestrates the projection generation.
/// </summary>
internal sealed partial class ProjectionGenerator
{
    private readonly Settings _settings;
    private readonly MetadataCache _cache;
    private readonly CancellationToken _token;

    public ProjectionGenerator(Settings settings, MetadataCache cache, CancellationToken token)
    {
        _settings = settings;
        _cache = cache;
        _token = token;
    }

    public void Run()
    {
        // Phase 1: discover the activatable runtime classes (component mode only).
        (HashSet<TypeDefinition> componentActivatable, Dictionary<string, HashSet<TypeDefinition>> componentByModule) = DiscoverComponentActivatableTypes();

        if (_settings.Verbose)
        {
            foreach (string p in _settings.Input)
            {
                Console.Out.WriteLine($"input: {p}");
            }
            Console.Out.WriteLine($"output: {_settings.OutputFolder}");
        }

        // Phase 2: write the GeneratedInterfaceIIDs file (skipped in reference projection mode).
        WriteGeneratedInterfaceIIDsFile();

        // Phase 3: per-namespace processing.
        ConcurrentDictionary<string, string> defaultInterfaceEntries = [];
        ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries = [];
        ConcurrentDictionary<string, string> authoredTypeNameToMetadataMap = [];
        bool projectionFileWritten = false;

        // Process namespaces sequentially for now (C++ used task_group / parallel processing).
        foreach ((string ns, NamespaceMembers members) in _cache.Namespaces)
        {
            _token.ThrowIfCancellationRequested();
            bool wrote = ProcessNamespace(ns, members, componentActivatable, defaultInterfaceEntries, exclusiveToInterfaceEntries, authoredTypeNameToMetadataMap);
            if (wrote)
            {
                projectionFileWritten = true;
            }
        }

        // Phase 4: component-mode WinRT_Module.cs file with activation-factory entry points.
        if (_settings.Component)
        {
            WriteComponentModuleFile(componentByModule);
            projectionFileWritten = true;
        }

        // Phase 5: WindowsRuntimeDefaultInterfaces.cs and WindowsRuntimeExclusiveToInterfaces.cs.
        if (defaultInterfaceEntries.Count > 0 && !_settings.ReferenceProjection)
        {
            List<KeyValuePair<string, string>> sorted = [.. defaultInterfaceEntries];
            sorted.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Key, b.Key));
            MetadataAttributeFactory.WriteDefaultInterfacesClass(_settings, sorted);
        }

        if (!exclusiveToInterfaceEntries.IsEmpty && _settings.Component && !_settings.ReferenceProjection)
        {
            List<KeyValuePair<string, string>> sorted = [.. exclusiveToInterfaceEntries];
            sorted.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Key, b.Key));
            MetadataAttributeFactory.WriteExclusiveToInterfacesClass(_settings, sorted);
        }

        // Phase 6: embedded resource files (ComInteropExtensions, InspectableVftbl, etc.).
        if (projectionFileWritten)
        {
            WriteBaseStrings();
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Orchestrates the projection generation. Mirrors the body of <c>cswinrt::run</c> in <c>main.cpp</c>.
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
        // Set the static cache reference for writers that need source-file paths
        CodeWriters.SetMetadataCache(_cache);

        // Find component activatable classes (component mode only)
        HashSet<TypeDefinition> componentActivatable = new();
        Dictionary<string, HashSet<TypeDefinition>> componentByModule = new(StringComparer.Ordinal);

        if (_settings.Component)
        {
            foreach ((_, NamespaceMembers members) in _cache.Namespaces)
            {
                foreach (TypeDefinition type in members.Classes)
                {
                    if (!_settings.Filter.Includes(type)) { continue; }
                    if (type.HasAttribute("Windows.Foundation.Metadata", "ActivatableAttribute") ||
                        type.HasAttribute("Windows.Foundation.Metadata", "StaticAttribute"))
                    {
                        _ = componentActivatable.Add(type);
                        string moduleName = Path.GetFileNameWithoutExtension(_cache.GetSourcePath(type));
                        if (!componentByModule.TryGetValue(moduleName, out HashSet<TypeDefinition>? set))
                        {
                            set = new HashSet<TypeDefinition>();
                            componentByModule[moduleName] = set;
                        }
                        _ = set.Add(type);
                    }
                }
            }
        }

        if (_settings.Verbose)
        {
            foreach (string p in _settings.Input)
            {
                Console.Out.WriteLine($"input: {p}");
            }
            Console.Out.WriteLine($"output: {_settings.OutputFolder}");
        }

        // Write GeneratedInterfaceIIDs file (mirrors main.cpp logic)
        bool iidWritten = false;
        if (!_settings.ReferenceProjection)
        {
            // Collect factory interfaces (Static/Activatable/Composable) referenced by included
            // classes globally. Their IIDs must be present in GeneratedInterfaceIIDs.cs even if
            // the filter excludes them, because static class members reference them.
            HashSet<TypeDefinition> factoryInterfacesGlobal = new();
            foreach ((_, NamespaceMembers nsMembers) in _cache.Namespaces)
            {
                foreach (TypeDefinition type in nsMembers.Classes)
                {
                    if (!_settings.Filter.Includes(type)) { continue; }
                    // Skip mapped classes whose ABI surface is suppressed (e.g.
                    // 'Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs' maps to
                    // 'System.Collections.Specialized.NotifyCollectionChangedEventArgs' with
                    // EmitAbi=false). Their factory/statics interfaces should also be skipped.
                    (string clsNs, string clsNm) = type.Names();
                    MappedType? clsMapped = MappedTypes.Get(clsNs, clsNm);
                    if (clsMapped is not null && !clsMapped.EmitAbi) { continue; }
                    foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, _cache))
                    {
                        TypeDefinition? facType = kv.Value.Type;
                        if (facType is not null) { _ = factoryInterfacesGlobal.Add(facType); }
                    }
                }
            }

            HashSet<TypeDefinition> interfacesFromClassesEmitted = new();
            TypeWriter guidWriter = new(_settings, "ABI");
            CodeWriters.WriteInterfaceIidsBegin(guidWriter);
            // Iterate namespaces in sorted order (mirrors C++ std::map<std::string, namespace_members>
            // iteration). Within each namespace, types are already sorted by SortMembersByName.
            // The sorted-by-namespace order produces the parent-before-child grouping in the
            // GeneratedInterfaceIIDs.cs output (e.g. Windows.ApplicationModel.* types before
            // Windows.ApplicationModel.Activation.* types).
            foreach ((string ns, NamespaceMembers members) in _cache.Namespaces.OrderBy(kvp => kvp.Key, System.StringComparer.Ordinal))
            {
                foreach (TypeDefinition type in members.Types)
                {
                    bool isFactoryInterface = factoryInterfacesGlobal.Contains(type);
                    if (!_settings.Filter.Includes(type) && !isFactoryInterface) { continue; }
                    if (TypeCategorization.IsGeneric(type)) { continue; }
                    (string ns2, string nm2) = type.Names();
                    MappedType? m = MappedTypes.Get(ns2, nm2);
                    if (m is not null && !m.EmitAbi) { continue; }
                    iidWritten = true;
                    TypeCategory cat = TypeCategorization.GetCategory(type);
                    switch (cat)
                    {
                        case TypeCategory.Delegate:
                            CodeWriters.WriteIidGuidPropertyFromSignature(guidWriter, type);
                            CodeWriters.WriteIidGuidPropertyFromType(guidWriter, type);
                            break;
                        case TypeCategory.Enum:
                            CodeWriters.WriteIidGuidPropertyFromSignature(guidWriter, type);
                            break;
                        case TypeCategory.Interface:
                            CodeWriters.WriteIidGuidPropertyFromType(guidWriter, type);
                            break;
                        case TypeCategory.Struct:
                            CodeWriters.WriteIidGuidPropertyFromSignature(guidWriter, type);
                            break;
                        case TypeCategory.Class:
                            CodeWriters.WriteIidGuidPropertyForClassInterfaces(guidWriter, type, interfacesFromClassesEmitted);
                            break;
                    }
                }
            }
            CodeWriters.WriteInterfaceIidsEnd(guidWriter);
            if (iidWritten)
            {
                guidWriter.FlushToFile(Path.Combine(_settings.OutputFolder, "GeneratedInterfaceIIDs.cs"));
            }
        }

        ConcurrentDictionary<string, string> defaultInterfaceEntries = new();
        ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries = new();
        ConcurrentDictionary<string, string> authoredTypeNameToMetadataMap = new();
        bool projectionFileWritten = false;

        // Process namespaces sequentially for now (C++ used task_group / parallel processing)
        foreach ((string ns, NamespaceMembers members) in _cache.Namespaces)
        {
            _token.ThrowIfCancellationRequested();
            bool wrote = ProcessNamespace(ns, members, componentActivatable, defaultInterfaceEntries, exclusiveToInterfaceEntries, authoredTypeNameToMetadataMap);
            if (wrote)
            {
                projectionFileWritten = true;
            }
        }

        // Component mode: write the WinRT_Module.cs file with activation factory entry points
        if (_settings.Component)
        {
            TextWriter wm = new();
            CodeWriters.WriteFileHeader(wm);
            CodeWriters.WriteModuleActivationFactory(wm, componentByModule);
            wm.FlushToFile(Path.Combine(_settings.OutputFolder, "WinRT_Module.cs"));
            projectionFileWritten = true;
        }

        // Write WindowsRuntimeDefaultInterfaces.cs and WindowsRuntimeExclusiveToInterfaces.cs
        if (defaultInterfaceEntries.Count > 0 && !_settings.ReferenceProjection)
        {
            List<KeyValuePair<string, string>> sorted = new(defaultInterfaceEntries);
            sorted.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Key, b.Key));
            CodeWriters.WriteDefaultInterfacesClass(_settings, sorted);
        }

        if (!exclusiveToInterfaceEntries.IsEmpty && _settings.Component && !_settings.ReferenceProjection)
        {
            List<KeyValuePair<string, string>> sorted = new(exclusiveToInterfaceEntries);
            sorted.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Key, b.Key));
            CodeWriters.WriteExclusiveToInterfacesClass(_settings, sorted);
        }

        // Write strings/ base files (ComInteropExtensions etc.)
        if (projectionFileWritten)
        {
            WriteBaseStrings();
        }
    }
}

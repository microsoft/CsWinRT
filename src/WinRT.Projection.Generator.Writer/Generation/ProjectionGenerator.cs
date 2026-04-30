// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Orchestrates the projection generation. Mirrors the body of <c>cswinrt::run</c> in <c>main.cpp</c>.
/// </summary>
internal sealed class ProjectionGenerator
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
                    if (TypeCategorization.HasAttribute(type, "Windows.Foundation.Metadata", "ActivatableAttribute") ||
                        TypeCategorization.HasAttribute(type, "Windows.Foundation.Metadata", "StaticAttribute"))
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

        // Write GUID property file (placeholder for now)
        if (!_settings.ReferenceProjection)
        {
            // The C++ implementation iterates all types and writes their IID properties.
            // Full implementation requires write_iid_guid_property_from_signature/from_type/for_class_interfaces.
            // For now, we skip generating this file - it's emitted only when there are real types to process.
        }

        ConcurrentDictionary<string, string> authoredTypeNameToMetadataMap = new();
        ConcurrentDictionary<string, string> defaultInterfaceEntries = new();
        ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries = new();
        bool projectionFileWritten = false;

        // Process namespaces sequentially for now (C++ used task_group / parallel processing)
        foreach ((string ns, NamespaceMembers members) in _cache.Namespaces)
        {
            _token.ThrowIfCancellationRequested();
            bool wrote = ProcessNamespace(ns, members, componentActivatable);
            if (wrote)
            {
                projectionFileWritten = true;
            }
        }

        // Write strings/ base files (ComInteropExtensions etc.)
        if (projectionFileWritten)
        {
            WriteBaseStrings();
        }
    }

    /// <summary>
    /// Processes a single namespace and writes its projection file. Returns whether a file was written.
    /// </summary>
    private bool ProcessNamespace(string ns, NamespaceMembers members, HashSet<TypeDefinition> componentActivatable)
    {
        TypeWriter w = new(_settings, ns);
        w.WriteFileHeader();

        bool written = false;

        // Phase 1 (C++): TypeMapGroup assembly attributes (skipped for now in this initial port)

        // Phase 2: Projected types
        w.WriteBeginProjectedNamespace();

        foreach (TypeDefinition type in members.Types)
        {
            if (!_settings.Filter.Includes(type)) { continue; }
            // Skip generic types and mapped types (mirrors C++ logic)
            if (TypeCategorization.IsGeneric(type)) { written = true; continue; }
            string ns2 = type.Namespace?.Value ?? string.Empty;
            string nm2 = type.Name?.Value ?? string.Empty;
            if (MappedTypes.Get(ns2, nm2) is not null) { written = true; continue; }

            // Write the projected type per category
            TypeCategory category = TypeCategorization.GetCategory(type);
            CodeWriters.WriteType(w, type, category, _settings);

            written = true;
        }

        w.WriteEndProjectedNamespace();

        if (!written)
        {
            return false;
        }

        // Output to file
        string filename = ns + ".cs";
        string fullPath = Path.Combine(_settings.OutputFolder, filename);
        w.FlushToFile(fullPath);
        return true;
    }

    /// <summary>
    /// Writes the embedded string resources (e.g., ComInteropExtensions.cs, InspectableVftbl.cs)
    /// to the output folder.
    /// </summary>
    private void WriteBaseStrings()
    {
        Assembly asm = typeof(ProjectionWriter).Assembly;
        foreach (string resName in asm.GetManifestResourceNames())
        {
            // Resource names look like 'WindowsRuntime.ProjectionGenerator.Writer.Resources.Base.ComInteropExtensions.cs'
            if (!resName.Contains(".Resources.Base."))
            {
                continue;
            }
            // Skip ComInteropExtensions if Windows is not included
            string fileName = resName[(resName.IndexOf(".Resources.Base.", StringComparison.Ordinal) + ".Resources.Base.".Length)..];
            if (fileName == "ComInteropExtensions.cs" && !_settings.Filter.Includes("Windows"))
            {
                continue;
            }

            using Stream stream = asm.GetManifestResourceStream(resName)!;
            using StreamReader reader = new(stream);
            string content = reader.ReadToEnd();

            // For ComInteropExtensions, prepend the UAC_VERSION define
            if (fileName == "ComInteropExtensions.cs")
            {
                int uapContractVersion = _cache.Find("Windows.Graphics.Display.DisplayInformation") is not null ? 15 : 7;
                content = $"#define UAC_VERSION_{uapContractVersion}\n" + content;
            }

            string outPath = Path.Combine(_settings.OutputFolder, fileName);
            File.WriteAllText(outPath, content);
        }
    }
}

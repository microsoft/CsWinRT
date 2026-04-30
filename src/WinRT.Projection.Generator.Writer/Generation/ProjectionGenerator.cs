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

        // Write GeneratedInterfaceIIDs file (mirrors main.cpp logic)
        bool iidWritten = false;
        if (!_settings.ReferenceProjection)
        {
            HashSet<TypeDefinition> interfacesFromClassesEmitted = new();
            TypeWriter guidWriter = new(_settings, "ABI");
            CodeWriters.WriteInterfaceIidsBegin(guidWriter);
            foreach ((string ns, NamespaceMembers members) in _cache.Namespaces)
            {
                foreach (TypeDefinition type in members.Types)
                {
                    if (!_settings.Filter.Includes(type)) { continue; }
                    if (TypeCategorization.IsGeneric(type)) { continue; }
                    string ns2 = type.Namespace?.Value ?? string.Empty;
                    string nm2 = type.Name?.Value ?? string.Empty;
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
        bool projectionFileWritten = false;

        // Process namespaces sequentially for now (C++ used task_group / parallel processing)
        foreach ((string ns, NamespaceMembers members) in _cache.Namespaces)
        {
            _token.ThrowIfCancellationRequested();
            bool wrote = ProcessNamespace(ns, members, componentActivatable, defaultInterfaceEntries, exclusiveToInterfaceEntries);
            if (wrote)
            {
                projectionFileWritten = true;
            }
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

    /// <summary>
    /// Processes a single namespace and writes its projection file. Returns whether a file was written.
    /// </summary>
    private bool ProcessNamespace(string ns, NamespaceMembers members, HashSet<TypeDefinition> componentActivatable,
        ConcurrentDictionary<string, string> defaultInterfaceEntries, ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries)
    {
        TypeWriter w = new(_settings, ns);
        w.WriteFileHeader();

        bool written = false;

        // Phase 1: TypeMapGroup assembly attributes
        if (!_settings.ReferenceProjection)
        {
            CodeWriters.WritePragmaDisableIL2026(w);
            foreach (TypeDefinition type in members.Types)
            {
                if (!_settings.Filter.Includes(type)) { continue; }
                if (TypeCategorization.IsGeneric(type)) { continue; }
                string ns2 = type.Namespace?.Value ?? string.Empty;
                string nm2 = type.Name?.Value ?? string.Empty;
                MappedType? m = MappedTypes.Get(ns2, nm2);
                if (m is not null && !m.EmitAbi) { continue; }

                TypeCategory cat = TypeCategorization.GetCategory(type);
                switch (cat)
                {
                    case TypeCategory.Class:
                        if (!TypeCategorization.IsStatic(type) && !TypeCategorization.IsAttributeType(type))
                        {
                            if (_settings.Component)
                            {
                                CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(w, type);
                            }
                            else
                            {
                                CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(w, type, false);
                            }
                        }
                        break;
                    case TypeCategory.Delegate:
                        CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(w, type, true);
                        CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(w, type);
                        break;
                    case TypeCategory.Enum:
                        CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(w, type, true);
                        CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(w, type);
                        break;
                    case TypeCategory.Interface:
                        CodeWriters.WriteWinRTIdicTypeMapGroupAssemblyAttribute(w, type);
                        CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(w, type);
                        break;
                    case TypeCategory.Struct:
                        if (!TypeCategorization.IsApiContractType(type))
                        {
                            CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(w, type, true);
                            CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(w, type);
                        }
                        break;
                }
            }
            CodeWriters.WritePragmaRestoreIL2026(w);
        }

        // Phase 2: Projected types
        w.WriteBeginProjectedNamespace();

        foreach (TypeDefinition type in members.Types)
        {
            if (!_settings.Filter.Includes(type)) { continue; }
            string ns2 = type.Namespace?.Value ?? string.Empty;
            string nm2 = type.Name?.Value ?? string.Empty;
            // Skip generic types and mapped types (mirrors C++ logic)
            if (MappedTypes.Get(ns2, nm2) is not null || TypeCategorization.IsGeneric(type))
            {
                written = true;
                continue;
            }

            // Write the projected type per category
            TypeCategory category = TypeCategorization.GetCategory(type);
            CodeWriters.WriteType(w, type, category, _settings, _cache);

            if (category == TypeCategory.Class && !TypeCategorization.IsAttributeType(type))
            {
                CodeWriters.AddDefaultInterfaceEntry(w, type, defaultInterfaceEntries);
                CodeWriters.AddExclusiveToInterfaceEntries(w, type, exclusiveToInterfaceEntries);
            }

            written = true;
        }

        w.WriteEndProjectedNamespace();

        if (!written)
        {
            return false;
        }

        // Phase 3: ABI types (when not reference projection)
        if (!_settings.ReferenceProjection)
        {
            w.WriteBeginAbiNamespace();
            foreach (TypeDefinition type in members.Types)
            {
                if (!_settings.Filter.Includes(type)) { continue; }
                if (TypeCategorization.IsGeneric(type)) { continue; }
                string ns2 = type.Namespace?.Value ?? string.Empty;
                string nm2 = type.Name?.Value ?? string.Empty;
                MappedType? m = MappedTypes.Get(ns2, nm2);
                if (m is not null && !m.EmitAbi) { continue; }
                if (TypeCategorization.IsApiContractType(type)) { continue; }
                if (TypeCategorization.IsAttributeType(type)) { continue; }

                TypeCategory category = TypeCategorization.GetCategory(type);
                CodeWriters.WriteAbiType(w, type, category, _settings);
            }
            w.WriteEndAbiNamespace();
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

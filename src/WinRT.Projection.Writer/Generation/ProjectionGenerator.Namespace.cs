// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter;

/// <inheritdoc cref="ProjectionGenerator"/>
internal sealed partial class ProjectionGenerator
{
    /// <summary>
    /// Processes a single namespace and writes its projection file. Returns whether a file was written.
    /// </summary>
    private bool ProcessNamespace(string ns, NamespaceMembers members, HashSet<TypeDefinition> componentActivatable,
        ConcurrentDictionary<string, string> defaultInterfaceEntries, ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries,
        ConcurrentDictionary<string, string> authoredTypeNameToMetadataMap)
    {
        ProjectionEmitContext context = new(_settings, _cache, ns);
        Writers.IndentedTextWriter writer = new();

        writer.WriteFileHeader(context);

        bool written = false;

        // Phase 1: TypeMapGroup assembly attributes
        if (!_settings.ReferenceProjection)
        {
            CodeWriters.WritePragmaDisableIL2026(writer);
            foreach (TypeDefinition type in members.Types)
            {
                if (!_settings.Filter.Includes(type)) { continue; }
                if (TypeCategorization.IsGeneric(type)) { continue; }
                (string ns2, string nm2) = type.Names();
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
                                CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                            }
                            else
                            {
                                CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, false);
                            }
                        }
                        break;
                    case TypeCategory.Delegate:
                        CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                        CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeCategory.Enum:
                        CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                        CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeCategory.Interface:
                        CodeWriters.WriteWinRTIdicTypeMapGroupAssemblyAttribute(writer, context, type);
                        CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeCategory.Struct:
                        if (!TypeCategorization.IsApiContractType(type))
                        {
                            CodeWriters.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                            CodeWriters.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        }
                        break;
                }
            }
            CodeWriters.WritePragmaRestoreIL2026(writer);
        }

        // Phase 2: Projected types
        writer.WriteBeginProjectedNamespace(context);

        foreach (TypeDefinition type in members.Types)
        {
            if (!_settings.Filter.Includes(type)) { continue; }
            (string ns2, string nm2) = type.Names();
            // Skip generic types and mapped types (mirrors C++ logic)
            if (MappedTypes.Get(ns2, nm2) is not null || TypeCategorization.IsGeneric(type))
            {
                written = true;
                continue;
            }

            // Write the projected type per category
            TypeCategory category = TypeCategorization.GetCategory(type);
            CodeWriters.WriteType(writer, context, type, category);

            if (category == TypeCategory.Class && !TypeCategorization.IsAttributeType(type))
            {
                CodeWriters.AddDefaultInterfaceEntry(context, type, defaultInterfaceEntries);
                CodeWriters.AddExclusiveToInterfaceEntries(context, type, exclusiveToInterfaceEntries);
                CodeWriters.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);
                if (_settings.Component && componentActivatable.Contains(type))
                {
                    CodeWriters.WriteFactoryClass(writer, context, type);
                }
            }
            else if (category is TypeCategory.Delegate or TypeCategory.Enum or TypeCategory.Interface)
            {
                CodeWriters.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);
            }
            else if (category == TypeCategory.Struct && !TypeCategorization.IsApiContractType(type))
            {
                CodeWriters.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);
            }

            written = true;
        }

        writer.WriteEndProjectedNamespace();

        if (!written)
        {
            return false;
        }

        // Phase 3: ABI types (when not reference projection)
        if (!_settings.ReferenceProjection)
        {
            // Collect factory interfaces (Static/Activatable/Composable) referenced by classes
            // included in this namespace. These must have their ABI Methods classes emitted even
            // when the filter excludes them, because the projected static class members dispatch
            // through them.
            HashSet<TypeDefinition> factoryInterfacesInThisNs = new();
            foreach (TypeDefinition type in members.Types)
            {
                if (!_settings.Filter.Includes(type)) { continue; }
                if (TypeCategorization.GetCategory(type) != TypeCategory.Class) { continue; }
                foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, _cache))
                {
                    AttributedType info = kv.Value;
                    TypeDefinition? facType = info.Type;
                    if (facType is null) { continue; }
                    // Only consider factory interfaces in the same namespace as we're processing.
                    string facNs = facType.Namespace?.Value ?? string.Empty;
                    if (facNs != ns) { continue; }
                    _ = factoryInterfacesInThisNs.Add(facType);
                }
            }

            writer.WriteBeginAbiNamespace(context);
            foreach (TypeDefinition type in members.Types)
            {
                bool isFactoryInterface = factoryInterfacesInThisNs.Contains(type);
                if (!_settings.Filter.Includes(type) && !isFactoryInterface) { continue; }
                if (TypeCategorization.IsGeneric(type)) { continue; }
                (string ns2, string nm2) = type.Names();
                MappedType? m = MappedTypes.Get(ns2, nm2);
                if (m is not null && !m.EmitAbi) { continue; }
                if (TypeCategorization.IsApiContractType(type)) { continue; }
                if (TypeCategorization.IsAttributeType(type)) { continue; }

                TypeCategory category = TypeCategorization.GetCategory(type);
                CodeWriters.WriteAbiType(writer, context, type, category);
            }
            writer.WriteEndAbiNamespace();
        }

        // Phase 4: Custom additions to namespaces (mirrors C++ main.cpp)
        foreach ((string addNs, string resName) in Additions.All)
        {
            if (addNs == ns && _settings.AdditionFilter.Includes(ns))
            {
                using System.IO.Stream? stream = typeof(ProjectionWriter).Assembly.GetManifestResourceStream(resName);
                if (stream is null) { continue; }
                using System.IO.StreamReader reader = new(stream);
                string content = reader.ReadToEnd();
                writer.Write(content);
            }
        }

        // Output to file
        string filename = ns + ".cs";
        string fullPath = Path.Combine(_settings.OutputFolder, filename);
        writer.FlushToFile(fullPath);
        return true;
    }
}

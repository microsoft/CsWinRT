// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Builders;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal sealed partial class ProjectionGenerator
{
    /// <summary>
    /// Processes a single namespace and writes its projection file. Returns whether a file was written.
    /// </summary>
    internal bool ProcessNamespace(string ns, NamespaceMembers members, ProjectionGeneratorRunState state)
    {
        ConcurrentDictionary<string, string> defaultInterfaceEntries = state.DefaultInterfaceEntries;
        ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries = state.ExclusiveToInterfaceEntries;
        ConcurrentDictionary<string, string> authoredTypeNameToMetadataMap = state.AuthoredTypeNameToMetadataMap;
        HashSet<TypeDefinition> componentActivatable = state.ComponentActivatable;
        ProjectionEmitContext context = new(_settings, _cache, ns);
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();
        IndentedTextWriter writer = writerOwner.Writer;

        writer.WriteFileHeader(context);

        bool written = false;

        // Phase 1: TypeMapGroup assembly attributes
        _token.ThrowIfCancellationRequested();
        if (!_settings.ReferenceProjection)
        {
            writer.WriteLine();
            writer.WriteLine("#pragma warning disable IL2026");
            foreach (TypeDefinition type in members.Types)
            {
                if (!_settings.Filter.Includes(type))
                {
                    continue;
                }

                if (TypeCategorization.IsGeneric(type))
                {
                    continue;
                }

                (string ns2, string nm2) = type.Names();
                MappedType? m = MappedTypes.Get(ns2, nm2);

                if (m is { EmitAbi: false })
                {
                    continue;
                }

                TypeCategory cat = TypeCategorization.GetCategory(type);
                switch (cat)
                {
                    case TypeCategory.Class:
                        if (!TypeCategorization.IsStatic(type) && !TypeCategorization.IsAttributeType(type))
                        {
                            if (_settings.Component)
                            {
                                MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                            }
                            else
                            {
                                MetadataAttributeFactory.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, false);
                            }
                        }

                        break;
                    case TypeCategory.Delegate:
                        MetadataAttributeFactory.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                        MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeCategory.Enum:
                        MetadataAttributeFactory.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                        MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeCategory.Interface:
                        MetadataAttributeFactory.WriteWinRTIdicTypeMapGroupAssemblyAttribute(writer, context, type);
                        MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeCategory.Struct:
                        if (!TypeCategorization.IsApiContractType(type))
                        {
                            MetadataAttributeFactory.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                            MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        }

                        break;
                }
            }

            writer.WriteLine();
            writer.WriteLine("#pragma warning restore IL2026");
        }

        // Phase 2: Projected types
        _token.ThrowIfCancellationRequested();
        writer.WriteBeginProjectedNamespace(context);

        foreach (TypeDefinition type in members.Types)
        {
            if (!_settings.Filter.Includes(type))
            {
                continue;
            }

            (string ns2, string nm2) = type.Names();
            // Skip generic types and mapped types
            if (MappedTypes.Get(ns2, nm2) is not null || TypeCategorization.IsGeneric(type))
            {
                written = true;
                continue;
            }

            // Write the projected type per category
            TypeCategory category = TypeCategorization.GetCategory(type);
            ProjectionFileBuilder.WriteType(writer, context, type, category);

            if (category == TypeCategory.Class && !TypeCategorization.IsAttributeType(type))
            {
                MetadataAttributeFactory.AddDefaultInterfaceEntry(context, type, defaultInterfaceEntries);
                MetadataAttributeFactory.AddExclusiveToInterfaceEntries(context, type, exclusiveToInterfaceEntries);
                ComponentFactory.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);

                if (_settings.Component && componentActivatable.Contains(type))
                {
                    ComponentFactory.WriteFactoryClass(writer, context, type);
                }
            }
            else if (category is TypeCategory.Delegate or TypeCategory.Enum or TypeCategory.Interface)
            {
                ComponentFactory.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);
            }
            else if (category == TypeCategory.Struct && !TypeCategorization.IsApiContractType(type))
            {
                ComponentFactory.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);
            }

            written = true;
        }

        writer.WriteEndProjectedNamespace(context);

        if (!written)
        {
            return false;
        }

        // Phase 3: ABI types (when not reference projection)
        _token.ThrowIfCancellationRequested();
        if (!_settings.ReferenceProjection)
        {
            // Collect factory interfaces (Static/Activatable/Composable) referenced by classes
            // included in this namespace. These must have their ABI Methods classes emitted even
            // when the filter excludes them, because the projected static class members dispatch
            // through them.
            HashSet<TypeDefinition> factoryInterfacesInThisNs = [];
            HashSet<TypeDefinition> factoryInterfacesAllNs = [];
            foreach (TypeDefinition type in members.Types)
            {
                if (!_settings.Filter.Includes(type))
                {
                    continue;
                }

                if (TypeCategorization.GetCategory(type) != TypeCategory.Class)
                {
                    continue;
                }

                AddFactoryInterfacesForClass(type, factoryInterfacesAllNs);
            }
            foreach (TypeDefinition facType in factoryInterfacesAllNs)
            {
                // Only consider factory interfaces in the same namespace as we're processing.
                string facNs = facType.Namespace?.Value ?? string.Empty;

                if (facNs == ns)
                {
                    _ = factoryInterfacesInThisNs.Add(facType);
                }
            }

            writer.WriteBeginAbiNamespace(context);
            foreach (TypeDefinition type in members.Types)
            {
                bool isFactoryInterface = factoryInterfacesInThisNs.Contains(type);

                if (!_settings.Filter.Includes(type) && !isFactoryInterface)
                {
                    continue;
                }

                if (TypeCategorization.IsGeneric(type))
                {
                    continue;
                }

                (string ns2, string nm2) = type.Names();
                MappedType? m = MappedTypes.Get(ns2, nm2);

                if (m is { EmitAbi: false })
                {
                    continue;
                }

                if (TypeCategorization.IsApiContractType(type))
                {
                    continue;
                }

                if (TypeCategorization.IsAttributeType(type))
                {
                    continue;
                }

                TypeCategory category = TypeCategorization.GetCategory(type);
                ProjectionFileBuilder.WriteAbiType(writer, context, type, category);
            }
            writer.WriteEndAbiNamespace(context);
        }

        // Phase 4: Custom additions to namespaces
        _token.ThrowIfCancellationRequested();
        if (_settings.AdditionFilter.Includes(ns))
        {
            foreach (string resName in Additions.EnumerateByNamespace(ns))
            {
                using Stream? stream = typeof(ProjectionWriter).Assembly.GetManifestResourceStream(resName);

                if (stream is null)
                {
                    continue;
                }

                using StreamReader reader = new(stream);
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

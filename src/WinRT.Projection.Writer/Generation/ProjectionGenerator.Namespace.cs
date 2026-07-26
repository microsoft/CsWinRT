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
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
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
        if (_settings.ImplementWinMDTypes)
        {
            return ProcessNamespaceImplementWinMDTypes(ns, members, state);
        }

        ConcurrentDictionary<string, string> defaultInterfaceEntries = state.DefaultInterfaceEntries;
        ConcurrentBag<KeyValuePair<string, string>> exclusiveToInterfaceEntries = state.ExclusiveToInterfaceEntries;
        ConcurrentDictionary<string, string> authoredTypeNameToMetadataMap = state.AuthoredTypeNameToMetadataMap;
        HashSet<TypeDefinition> componentActivatable = state.ComponentActivatable;
        ProjectionEmitContext context = new(_settings, _cache, ns, _staticConstructorAnalyzer, state.WindowsRuntimeMetadataTypeEntries);
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
                if (!_settings.Filter.Includes(type.FullName))
                {
                    continue;
                }

                if (type.IsGeneric)
                {
                    continue;
                }

                (string ns2, string nm2) = type.Names();
                MappedType? m = MappedTypes.Get(ns2, nm2);

                if (m is { EmitAbi: false })
                {
                    continue;
                }

                TypeKind kind = TypeKindResolver.Resolve(type);
                switch (kind)
                {
                    case TypeKind.Class:
                        if (!type.IsStatic && !type.IsAttributeType)
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
                    case TypeKind.Delegate:
                        MetadataAttributeFactory.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                        MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeKind.Enum:
                        MetadataAttributeFactory.WriteWinRTComWrappersTypeMapGroupAssemblyAttribute(writer, context, type, true);
                        MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeKind.Interface:
                        MetadataAttributeFactory.WriteWinRTIdicTypeMapGroupAssemblyAttribute(writer, context, type);
                        MetadataAttributeFactory.WriteWinRTWindowsMetadataTypeMapGroupAssemblyAttribute(writer, context, type);
                        break;
                    case TypeKind.Struct:
                        if (!type.IsApiContractType)
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
            if (!_settings.Filter.Includes(type.FullName))
            {
                continue;
            }

            (string ns2, string nm2) = type.Names();

            // Skip generic types and mapped types
            if (MappedTypes.Get(ns2, nm2) is not null || type.IsGeneric)
            {
                written = true;
                continue;
            }

            // Write the projected type per type kind
            TypeKind kind = TypeKindResolver.Resolve(type);
            ProjectionFileBuilder.WriteType(writer, context, type, kind);

            if (kind == TypeKind.Class && !type.IsAttributeType)
            {
                MetadataAttributeFactory.AddDefaultInterfaceEntry(context, type, defaultInterfaceEntries);
                MetadataAttributeFactory.AddExclusiveToInterfaceEntries(context, type, exclusiveToInterfaceEntries);
                ComponentFactory.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);

                if (_settings.Component && componentActivatable.Contains(type))
                {
                    ComponentFactory.WriteFactoryClass(writer, context, type);
                }
            }
            else if (kind is TypeKind.Delegate or TypeKind.Enum or TypeKind.Interface)
            {
                ComponentFactory.AddMetadataTypeEntry(context, type, authoredTypeNameToMetadataMap);
            }
            else if (kind == TypeKind.Struct && !type.IsApiContractType)
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
                if (!_settings.Filter.Includes(type.FullName))
                {
                    continue;
                }

                if (TypeKindResolver.Resolve(type) != TypeKind.Class)
                {
                    continue;
                }

                AddFactoryInterfacesForClass(type, factoryInterfacesAllNs);
            }
            foreach (TypeDefinition facType in factoryInterfacesAllNs)
            {
                // Only consider factory interfaces in the same namespace as we're processing.
                string facNs = facType.GetRawNamespace();

                if (facNs == ns)
                {
                    _ = factoryInterfacesInThisNs.Add(facType);
                }
            }

            writer.WriteBeginAbiNamespace(context);
            foreach (TypeDefinition type in members.Types)
            {
                bool isFactoryInterface = factoryInterfacesInThisNs.Contains(type);

                if (!_settings.Filter.Includes(type.FullName) && !isFactoryInterface)
                {
                    continue;
                }

                if (type.IsGeneric)
                {
                    continue;
                }

                (string ns2, string nm2) = type.Names();
                MappedType? m = MappedTypes.Get(ns2, nm2);

                if (m is { EmitAbi: false })
                {
                    continue;
                }

                if (type.IsApiContractType)
                {
                    continue;
                }

                if (type.IsAttributeType)
                {
                    continue;
                }

                TypeKind kind = TypeKindResolver.Resolve(type);
                ProjectionFileBuilder.WriteAbiType(writer, context, type, kind);

                // Regenerate the authoring surface behind any referenced authoring reference assembly, so
                // the abstract base classes the components extend get their real implementation here.
                if (kind == TypeKind.Class && _settings.ImplementableTypes.Contains(type.FullName))
                {
                    AbiImplementableClassFactory.WriteImplementableClass(writer, context, type);
                    AbiImplementableClassFactory.WriteImplementableFactoryClass(writer, context, type);
                }
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

    /// <summary>
    /// Emits only the authoring surface for a namespace: the implemented types' exclusive-to and factory
    /// interfaces (declarations only) and the abstract <c>ABI.&lt;Ns&gt;.&lt;Class&gt;</c> /
    /// <c>ABI.&lt;Ns&gt;.&lt;Class&gt;Factory</c> bases. Nothing else is emitted, so all projections stay
    /// unchanged and this can be compiled into a component's own assembly.
    /// </summary>
    private bool ProcessNamespaceImplementWinMDTypes(string ns, NamespaceMembers members, ProjectionGeneratorRunState state)
    {
        ProjectionEmitContext context = new(_settings, _cache, ns, state.WindowsRuntimeMetadataTypeEntries);
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();
        IndentedTextWriter writer = writerOwner.Writer;

        // The runtime classes in this namespace that the user asked to implement.
        List<TypeDefinition> implementedTypes = [];

        foreach (TypeDefinition type in members.Types)
        {
            _token.ThrowIfCancellationRequested();

            if (!type.IsGeneric &&
                _settings.Filter.Includes(type.FullName) &&
                TypeKindResolver.Resolve(type) == TypeKind.Class &&
                AbiImplementableClassFactory.ShouldEmit(context, type))
            {
                implementedTypes.Add(type);
            }
        }

        if (implementedTypes.Count == 0)
        {
            return false;
        }

        writer.WriteFileHeader(context);

        // Projected namespace: emit the exclusive-to interface declarations for the implemented types.
        // These are skipped by a normal projection, so the component is their sole definer. They are
        // resolved from their '[ExclusiveTo]' class rather than from the filter, so a type-scoped
        // filter (e.g. a single class) still emits every interface its abstract base needs.
        HashSet<TypeDefinition> implementedTypeSet = [.. implementedTypes];

        writer.WriteBeginProjectedNamespace(context);

        foreach (TypeDefinition type in members.Types)
        {
            _token.ThrowIfCancellationRequested();

            if (type.IsGeneric || !type.IsExclusiveTo || TypeKindResolver.Resolve(type) != TypeKind.Interface)
            {
                continue;
            }

            if (AbiTypeHelpers.GetExclusiveToType(_cache, type) is { } exclusiveToType && implementedTypeSet.Contains(exclusiveToType))
            {
                InterfaceFactory.WriteInterface(writer, context, type);
            }
        }

        writer.WriteEndProjectedNamespace(context);

        // ABI namespace: emit the abstract bases the author extends to implement each type.
        writer.WriteBeginAbiNamespace(context);

        foreach (TypeDefinition type in implementedTypes)
        {
            _token.ThrowIfCancellationRequested();

            AbiImplementableClassFactory.WriteImplementableClass(writer, context, type);
            AbiImplementableClassFactory.WriteImplementableFactoryClass(writer, context, type);
        }

        writer.WriteEndAbiNamespace(context);

        string fullPath = Path.Combine(_settings.OutputFolder, ns + ".cs");

        writer.FlushToFile(fullPath);
        state.MarkProjectionFileWritten();

        return true;
    }
}

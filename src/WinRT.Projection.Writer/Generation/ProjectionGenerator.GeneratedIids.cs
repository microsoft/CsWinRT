// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal sealed partial class ProjectionGenerator
{
    /// <summary>
    /// Writes the <c>GeneratedInterfaceIIDs.cs</c> file containing the IID GUID property
    /// definitions for every projected interface, delegate, enum, struct, and runtime class.
    /// </summary>
    /// <remarks>
    /// Skipped entirely in reference-projection mode (no IIDs are needed in the public API surface).
    /// </remarks>
    internal void WriteGeneratedInterfaceIidsFile()
    {
        if (_settings.ReferenceProjection)
        {
            return;
        }

        // Collect factory interfaces (Static/Activatable/Composable) referenced by included
        // classes globally. Their IIDs must be present in GeneratedInterfaceIIDs.cs even if
        // the filter excludes them, because static class members reference them.
        HashSet<TypeDefinition> factoryInterfacesGlobal = [];
        foreach ((_, NamespaceMembers nsMembers) in _cache.Namespaces)
        {
            foreach (TypeDefinition type in nsMembers.Classes)
            {
                if (!_settings.Filter.Includes(type))
                {
                    continue;
                }

                // Skip mapped classes whose ABI surface is suppressed (e.g.
                // 'Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs' maps to
                // 'System.Collections.Specialized.NotifyCollectionChangedEventArgs' with
                // EmitAbi=false). Their factory/statics interfaces should also be skipped.
                (string clsNs, string clsNm) = type.Names();
                MappedType? clsMapped = MappedTypes.Get(clsNs, clsNm);

                if (clsMapped is { EmitAbi: false })
                {
                    continue;
                }

                AddFactoryInterfacesForClass(type, factoryInterfacesGlobal);
            }
        }

        bool iidWritten = false;
        HashSet<TypeDefinition> interfacesFromClassesEmitted = [];
        ProjectionEmitContext guidContext = new(_settings, _cache, "ABI");
        using IndentedTextWriterOwner guidIndentedOwner = IndentedTextWriterPool.GetOrCreate();
        IndentedTextWriter guidIndented = guidIndentedOwner.Writer;
        IidExpressionGenerator.WriteInterfaceIidsBegin(guidIndented);
        guidIndented.IncreaseIndent();
        // Iterate namespaces in sorted order. Within each namespace, types are already sorted by SortMembersByName.
        // The sorted-by-namespace order produces the parent-before-child grouping in the
        // GeneratedInterfaceIIDs.cs output (e.g. Windows.ApplicationModel.* types before
        // Windows.ApplicationModel.Activation.* types).
        foreach ((string ns, NamespaceMembers members) in _cache.Namespaces.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            _token.ThrowIfCancellationRequested();
            foreach (TypeDefinition type in members.Types)
            {
                bool isFactoryInterface = factoryInterfacesGlobal.Contains(type);

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

                iidWritten = true;
                TypeCategory cat = TypeCategorization.GetCategory(type);
                switch (cat)
                {
                    case TypeCategory.Delegate:
                        IidExpressionGenerator.WriteIidGuidPropertyFromSignature(guidIndented, guidContext, type);
                        IidExpressionGenerator.WriteIidGuidPropertyFromType(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Enum:
                        IidExpressionGenerator.WriteIidGuidPropertyFromSignature(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Interface:
                        IidExpressionGenerator.WriteIidGuidPropertyFromType(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Struct:
                        IidExpressionGenerator.WriteIidGuidPropertyFromSignature(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Class:
                        IidExpressionGenerator.WriteIidGuidPropertyForClassInterfaces(guidIndented, guidContext, type, interfacesFromClassesEmitted);
                        break;
                }
            }
        }

        IidExpressionGenerator.WriteInterfaceIidsEnd(guidIndented);

        if (iidWritten)
        {
            guidIndented.FlushToFile(Path.Combine(_settings.OutputFolder, "GeneratedInterfaceIIDs.cs"));
        }
    }
}

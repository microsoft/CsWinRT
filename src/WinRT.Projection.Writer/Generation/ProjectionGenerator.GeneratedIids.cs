// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter;

/// <inheritdoc cref="ProjectionGenerator"/>
internal sealed partial class ProjectionGenerator
{
    /// <summary>
    /// Writes the <c>GeneratedInterfaceIIDs.cs</c> file containing the IID GUID property
    /// definitions for every projected interface, delegate, enum, struct, and runtime class.
    /// Mirrors the corresponding logic from <c>main.cpp</c>.
    /// </summary>
    /// <remarks>
    /// Skipped entirely in reference-projection mode (no IIDs are needed in the public API surface).
    /// </remarks>
    private void WriteGeneratedInterfaceIIDsFile()
    {
        if (_settings.ReferenceProjection)
        {
            return;
        }

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

        bool iidWritten = false;
        HashSet<TypeDefinition> interfacesFromClassesEmitted = new();
        ProjectionEmitContext guidContext = new(_settings, _cache, "ABI");
        Writers.IndentedTextWriter guidIndented = new();
        IIDExpressionWriter.WriteInterfaceIidsBegin(guidIndented);
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
                        IIDExpressionWriter.WriteIidGuidPropertyFromSignature(guidIndented, guidContext, type);
                        IIDExpressionWriter.WriteIidGuidPropertyFromType(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Enum:
                        IIDExpressionWriter.WriteIidGuidPropertyFromSignature(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Interface:
                        IIDExpressionWriter.WriteIidGuidPropertyFromType(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Struct:
                        IIDExpressionWriter.WriteIidGuidPropertyFromSignature(guidIndented, guidContext, type);
                        break;
                    case TypeCategory.Class:
                        IIDExpressionWriter.WriteIidGuidPropertyForClassInterfaces(guidIndented, guidContext, type, interfacesFromClassesEmitted);
                        break;
                }
            }
        }
        IIDExpressionWriter.WriteInterfaceIidsEnd(guidIndented);
        if (iidWritten)
        {
            guidIndented.FlushToFile(Path.Combine(_settings.OutputFolder, "GeneratedInterfaceIIDs.cs"));
        }
    }
}

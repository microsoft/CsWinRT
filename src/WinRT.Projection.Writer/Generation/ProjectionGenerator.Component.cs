// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal sealed partial class ProjectionGenerator
{
    /// <summary>
    /// Discovers all component-mode activatable runtime classes (those carrying
    /// <c>[ActivatableAttribute]</c> or <c>[StaticAttribute]</c>) across the cached
    /// namespaces and groups them by source <c>.winmd</c> module name.
    /// </summary>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    /// <item><description><c>ComponentActivatable</c> -- the flat set of all activatable classes</description></item>
    /// <item><description><c>ByModule</c> -- the same set keyed by source module name (used to emit per-module activation-factory entry points in <see cref="WriteComponentModuleFile"/>)</description></item>
    /// </list>
    /// </returns>
    private (HashSet<TypeDefinition> ComponentActivatable, Dictionary<string, HashSet<TypeDefinition>> ByModule) DiscoverComponentActivatableTypes()
    {
        HashSet<TypeDefinition> componentActivatable = [];
        Dictionary<string, HashSet<TypeDefinition>> componentByModule = [];

        if (!_settings.Component)
        {
            return (componentActivatable, componentByModule);
        }

        foreach ((_, NamespaceMembers members) in _cache.Namespaces)
        {
            foreach (TypeDefinition type in members.Classes)
            {
                if (!_settings.Filter.Includes(type)) { continue; }
                if (type.HasAttribute(WindowsFoundationMetadata, ActivatableAttribute) ||
                    type.HasAttribute(WindowsFoundationMetadata, StaticAttribute))
                {
                    _ = componentActivatable.Add(type);
                    string moduleName = Path.GetFileNameWithoutExtension(_cache.GetSourcePath(type));
                    if (!componentByModule.TryGetValue(moduleName, out HashSet<TypeDefinition>? set))
                    {
                        set = [];
                        componentByModule[moduleName] = set;
                    }
                    _ = set.Add(type);
                }
            }
        }

        return (componentActivatable, componentByModule);
    }

    /// <summary>
    /// Writes the <c>WinRT_Module.cs</c> file containing the per-module activation factory
    /// entry points. Component mode only.
    /// </summary>
    /// <param name="componentByModule">The activatable classes grouped by source module name (from <see cref="DiscoverComponentActivatableTypes"/>).</param>
    private void WriteComponentModuleFile(Dictionary<string, HashSet<TypeDefinition>> componentByModule)
    {
        // WinRT_Module.cs (and similar support files like GeneratedInterfaceIIDs.cs and the
        // base resources under Resources/Base/) require only the auto-generated banner without
        // the standard usings/pragmas prelude that per-namespace projection files emit, so this
        // path goes through MetadataAttributeFactory.WriteFileHeader rather than the
        // IndentedTextWriter extension that includes the full prelude.
        IndentedTextWriter wm = new();
        MetadataAttributeFactory.WriteFileHeader(wm);
        ComponentFactory.WriteModuleActivationFactory(wm, componentByModule);
        wm.FlushToFile(Path.Combine(_settings.OutputFolder, "WinRT_Module.cs"));
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for the <c>[IgnoresAccessChecksTo]</c> uses.
/// </summary>
internal static partial class IgnoresAccessChecksToBuilder
{
    /// <summary>
    /// Defines all assembly attributes for target modules using the Windows TFM.
    /// </summary>
    /// <param name="winRTSdkXamlProjectionModule">The <see cref="ModuleDefinition"/> for the Windows Runtime XAML projection assembly (i.e. <c>WinRT.Sdk.Xaml.Projection.dll</c>).</param>
    /// <param name="winRTProjectionModule">The <see cref="ModuleDefinition"/> for the Windows Runtime projection assembly (i.e. <c>WinRT.Projection.dll</c>).</param>
    /// <param name="winRTComponentModule">The <see cref="ModuleDefinition"/> for the Windows Runtime component assembly (i.e. <c>WinRT.Component.dll</c>).</param>
    /// <param name="referencePathModules">The input set of reference path modules.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    public static void AssemblyAttributes(
        ModuleDefinition? winRTSdkXamlProjectionModule,
        ModuleDefinition? winRTProjectionModule,
        ModuleDefinition? winRTComponentModule,
        IEnumerable<ModuleDefinition> referencePathModules,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (ModuleDefinition assemblyModule in referencePathModules)
        {
            // We only need this for assemblies targeting the Windows TFM
            if (!assemblyModule.ReferencesWindowsRuntimeAssembly &&
                !assemblyModule.IsWindowsRuntimeModule)
            {
                continue;
            }

            // Drop the '.dll' extension from the assembly name
            string assemblyName = Path.GetFileNameWithoutExtension(Path.Normalize(assemblyModule.Name!));

            // Create the attribute and add it to the assembly
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.IgnoresAccessChecksTo(assemblyName, interopDefinitions, interopReferences));
        }

        // We also always add an attribute for 'WinRT.Sdk.Projection', which is the precompiled projection .dll for the Windows SDK
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.IgnoresAccessChecksTo("WinRT.Sdk.Projection", interopDefinitions, interopReferences));

        // For 'WinRT.Sdk.Xaml.Projection', which is the precompiled projection .dll for Windows SDK XAML types, we only
        // need the attribute if we do have that .dll being passed as input. That is only if XAML projections are enabled.
        if (winRTSdkXamlProjectionModule is not null)
        {
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.IgnoresAccessChecksTo("WinRT.Sdk.Xaml.Projection", interopDefinitions, interopReferences));
        }

        // For 'WinRT.Projection', which is the merged Windows Runtime projection assembly that is generated at final build time,
        // we only need the attribute if we do have that .dll being passed as input. That is only if we have any 3rd party types.
        if (winRTProjectionModule is not null)
        {
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.IgnoresAccessChecksTo("WinRT.Projection", interopDefinitions, interopReferences));
        }

        // Similarly, we need an attribute for 'WinRT.Component' only if we reference some components and we do have this .dll
        if (winRTComponentModule is not null)
        {
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.IgnoresAccessChecksTo("WinRT.Component", interopDefinitions, interopReferences));
        }
    }
}
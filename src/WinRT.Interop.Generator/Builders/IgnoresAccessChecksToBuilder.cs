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
    /// <param name="referencePathModules">The input set of reference path modules.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    public static void AssemblyAttributes(
        IEnumerable<ModuleDefinition> referencePathModules,
        InteropDefinitions interopDefinitions,
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
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyModule.Name!);

            // Create the attribute and add it to the assembly
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.IgnoresAccessChecksTo(assemblyName, interopDefinitions, module));
        }
    }
}
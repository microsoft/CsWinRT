// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="ModuleDefinition"/>.
/// </summary>
internal static class ModuleDefinitionExtensions
{
    /// <summary>
    /// Imports a type definition or reference into a module using the default reference importer.
    /// </summary>
    /// <param name="typeDefOrRef">The <see cref="ITypeDefOrRef"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="ITypeDefOrRef"/>.</returns>
    public static bool IsOrReferencesWindowsSDKProjectionsAssembly(this ModuleDefinition module)
    {
        // If the assembly references the Windows SDK projections, gather it
        foreach (AssemblyReference reference in module.AssemblyReferences)
        {
            if (reference.Name?.AsSpan().SequenceEqual(WellKnownInteropNames.WindowsSDKDllNameUtf8[..^4]) is true)
            {
                return true;
            }
        }

        // Otherwise, check if it's the Windows SDK projections assembly itself
        return module.Name?.AsSpan().SequenceEqual(WellKnownInteropNames.WindowsSDKDllNameUtf8) is true;
    }
}

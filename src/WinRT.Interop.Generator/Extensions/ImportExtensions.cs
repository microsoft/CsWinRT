// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions to import metadata elements into modules.
/// </summary>
internal static class ImportExtensions
{
    /// <summary>
    /// Imports a member reference into a module using the default reference importer.
    /// </summary>
    /// <param name="assemblyReference">The <see cref="AssemblyReference"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="AssemblyReference"/>.</returns>
    public static AssemblyReference Import(this AssemblyReference assemblyReference, ModuleDefinition module)
    {
        return assemblyReference.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a member reference into a module using the default reference importer.
    /// </summary>
    /// <param name="assemblyReference">The <see cref="AssemblyReference"/> instance to import.</param>
    /// <param name="scope">The scope to import into.</param>
    /// <returns>The imported <see cref="AssemblyReference"/>.</returns>
    public static AssemblyReference Import(this AssemblyReference assemblyReference, IResolutionScope scope)
    {
        return assemblyReference.ImportWith(scope.ContextModule!.DefaultImporter);
    }
}
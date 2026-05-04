// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// State produced by the processing phase of <see cref="ProjectionGenerator"/>.
/// </summary>
/// <param name="sourcesFolder">The path to the folder where sources will be generated.</param>
/// <param name="rspFilePath">The path to the generated response file for CsWinRT.</param>
/// <param name="referencesWithoutProjections">The reference assembly paths excluding projection assemblies.</param>
/// <param name="hasTypesToProject">Whether any types were found to project.</param>
/// <param name="componentAssemblyNames">Sorted simple names of all input <c>[WindowsRuntimeComponentAssembly]</c> references (component-mode only).</param>
internal sealed class ProjectionGeneratorProcessingState(
    string sourcesFolder,
    string rspFilePath,
    string[] referencesWithoutProjections,
    bool hasTypesToProject = true,
    IReadOnlyList<string>? componentAssemblyNames = null)
{
    /// <summary>
    /// Gets the path to the folder where sources will be generated.
    /// </summary>
    public string SourcesFolder { get; } = sourcesFolder;

    /// <summary>
    /// Gets the path to the generated response file for CsWinRT.
    /// </summary>
    public string RspFilePath { get; } = rspFilePath;

    /// <summary>
    /// Gets the reference assembly paths excluding projection assemblies.
    /// </summary>
    public string[] ReferencesWithoutProjections { get; } = referencesWithoutProjections;

    /// <summary>
    /// Gets whether any types were found to project. When <c>false</c>, the source generation
    /// and emit phases should be skipped (no DLL will be produced).
    /// </summary>
    public bool HasTypesToProject { get; } = hasTypesToProject;

    /// <summary>
    /// Gets the simple names of all input assemblies marked with
    /// <c>[WindowsRuntimeComponentAssembly]</c>. Empty unless this is a component-mode run
    /// (i.e. producing <c>WinRT.Component.dll</c>).
    /// </summary>
    public IReadOnlyList<string> ComponentAssemblyNames { get; } = componentAssemblyNames ?? [];
}

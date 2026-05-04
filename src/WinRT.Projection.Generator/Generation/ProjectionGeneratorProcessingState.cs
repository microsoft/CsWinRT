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
/// <param name="componentNamespacePrefixes">Per-component top-level namespace segments (component-mode only).</param>
internal sealed class ProjectionGeneratorProcessingState(
    string sourcesFolder,
    string rspFilePath,
    string[] referencesWithoutProjections,
    bool hasTypesToProject = true,
    IReadOnlyList<string>? componentAssemblyNames = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? componentNamespacePrefixes = null)
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

    /// <summary>
    /// Per-component top-level namespace segments observed in the corresponding
    /// <c>.winmd</c> file. The merged activation dispatcher uses these to short-circuit
    /// to the correct component's <c>ManagedExports.GetActivationFactory</c> based on
    /// the first segment of the requested runtime class name, instead of walking all
    /// components linearly.
    /// </summary>
    /// <remarks>
    /// Indexed by component assembly simple name (matches <see cref="ComponentAssemblyNames"/>).
    /// Each value is the set of distinct first-segment namespace tokens contributed by that
    /// component. When a runtime class name's first segment doesn't match any entry here,
    /// the dispatcher falls back to iterating all components.
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ComponentNamespacePrefixes { get; } =
        componentNamespacePrefixes ?? new Dictionary<string, IReadOnlyList<string>>();
}

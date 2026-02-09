// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// State produced by the processing phase of <see cref="ProjectionGenerator"/>.
/// </summary>
/// <param name="sourcesFolder">The path to the folder where sources will be generated.</param>
/// <param name="rspFilePath">The path to the generated response file for CsWinRT.</param>
/// <param name="referencesWithoutProjections">The reference assembly paths excluding projection assemblies.</param>
internal sealed class ProjectionGeneratorProcessingState(string sourcesFolder, string rspFilePath, string[] referencesWithoutProjections)
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
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// Options for <see cref="AuthoringExportTypesGenerator"/>.
/// </summary>
/// <param name="PublishAot"><inheritdoc cref="AnalyzerConfigOptionsExtensions.GetPublishAot" path="/returns/node()"/></param>
/// <param name="IsComponent"><inheritdoc cref="AnalyzerConfigOptionsExtensions.GetCsWinRTComponent" path="/returns/node()"/></param>
/// <param name="MergeReferencedActivationFactories"><inheritdoc cref="AnalyzerConfigOptionsExtensions.GetCsWinRTMergeReferencedActivationFactories" path="/returns/node()"/></param>
internal record AuthoringExportTypesOptions(
    bool PublishAot,
    bool IsComponent,
    bool MergeReferencedActivationFactories)
{
    /// <summary>
    /// Gets whether the managed exports should be emitted.
    /// </summary>
    /// <returns>Whether the managed exports should be emitted.</returns>
    public bool ShouldEmitManagedExports()
    {
        return IsComponent || MergeReferencedActivationFactories;
    }

    /// <summary>
    /// Gets whether the native exports should be emitted.
    /// </summary>
    /// <returns>Whether the native exports should be emitted.</returns>
    public bool ShouldEmitNativeExports()
    {
        if (!PublishAot)
        {
            return false;
        }

        // We need these either in normal publishing scenarios where AOT is enabled, or also
        // if the project is not a component, but we're merging referenced activation factories.
        return IsComponent || MergeReferencedActivationFactories;
    }
}
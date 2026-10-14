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
    /// <param name="hasActivationFactories">Whether the compilation declares any <c>[WindowsRuntimeActivationFactory]</c> types.</param>
    /// <returns>Whether the managed exports should be emitted.</returns>
    public bool ShouldEmitManagedExports(bool hasActivationFactories = false)
    {
        return IsComponent || MergeReferencedActivationFactories || hasActivationFactories;
    }

    /// <summary>
    /// Gets whether the <c>[WindowsRuntimeComponentAssembly]</c> assembly attribute should be emitted.
    /// </summary>
    /// <returns>Whether the <c>[WindowsRuntimeComponentAssembly]</c> assembly attribute should be emitted.</returns>
    /// <remarks>
    /// The attribute marks an assembly whose Windows Runtime types are projected into the generated
    /// <c>WinRT.Component.dll</c>. A project that only implements types declared in existing metadata does not
    /// produce one: its types are projected by the referenced projection instead, so it must not be marked.
    /// </remarks>
    public bool ShouldEmitComponentAssemblyAttribute()
    {
        return IsComponent || MergeReferencedActivationFactories;
    }

    /// <summary>
    /// Gets whether the native exports should be emitted.
    /// </summary>
    /// <param name="hasActivationFactories">Whether the compilation declares any <c>[WindowsRuntimeActivationFactory]</c> types.</param>
    /// <returns>Whether the native exports should be emitted.</returns>
    public bool ShouldEmitNativeExports(bool hasActivationFactories = false)
    {
        if (!PublishAot)
        {
            return false;
        }

        // We need these either in normal publishing scenarios where AOT is enabled, or also if the project is not a
        // component, but we're merging referenced activation factories or authoring types defined in an existing .winmd.
        return IsComponent || MergeReferencedActivationFactories || hasActivationFactories;
    }
}
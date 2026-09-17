// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.References;

/// <summary>
/// Well-known CsWinRT diagnostics that generated projections report in user code, through the
/// attributes the projection writer emits.
/// </summary>
/// <remarks>
/// Each id has a documentation page under <c>docs/diagnostics</c>, reachable through
/// <see cref="UrlFormat"/>. The ids are part of the public contract of a projection: user code
/// suppresses them by id, so they must never be renamed or reused for a different meaning.
/// </remarks>
internal static class WellKnownDiagnostics
{
    /// <summary>
    /// The diagnostic id reported when user code uses a Windows Runtime API marked with the
    /// <c>[Windows.Foundation.Metadata.Experimental]</c> attribute in its Windows Runtime metadata.
    /// </summary>
    /// <remarks>
    /// Windows Runtime metadata has no per-API diagnostic id (the attribute carries no arguments),
    /// so every experimental Windows Runtime API shares this single id.
    /// </remarks>
    public const string ExperimentalWindowsRuntimeApiId = "CSWINRT3005";

    /// <summary>
    /// The message reported alongside <see cref="ExperimentalWindowsRuntimeApiId"/>.
    /// </summary>
    /// <remarks>
    /// The compiler already explains that the API is for evaluation purposes only, so this only adds
    /// the piece of context it cannot know: that the marker comes from Windows Runtime metadata.
    /// </remarks>
    public const string ExperimentalWindowsRuntimeApiMessage = "This Windows Runtime API is marked as experimental in its Windows Runtime metadata";

    /// <summary>
    /// The URL format for all CsWinRT diagnostics.
    /// </summary>
    /// <remarks>
    /// This URL format assumes it will receive the diagnostic id as a parameter.
    /// </remarks>
    public const string UrlFormat = "https://aka.ms/cswinrt/errors/{0}";
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Per-emission context bundling all state that is shared by the projection writers when
/// emitting a single projection (settings + metadata cache + the active namespace).
/// </summary>
/// <remarks>
/// <para>
/// Replaces the implicit state previously held on <c>TypeWriter</c> (which mixed indented-text
/// emission with WinRT-specific state) and the hidden static <c>CodeWriters._cacheRef</c>.
/// </para>
/// <para>
/// During the refactor the existing <c>TypeWriter</c> remains the primary writer surface;
/// <see cref="ProjectionEmitContext"/> is introduced first as additive infrastructure so it can
/// then be threaded through writer signatures one family at a time (sub-passes 10b/10c) before
/// <c>TypeWriter</c>/<c>TextWriter</c> are finally retired.
/// </para>
/// </remarks>
/// <param name="settings">The active projection settings.</param>
/// <param name="cache">The metadata cache for the current generation.</param>
/// <param name="currentNamespace">The namespace currently being emitted (or <see cref="string.Empty"/> when not in a per-namespace pass).</param>
internal sealed class ProjectionEmitContext(Settings settings, MetadataCache cache, string currentNamespace)
{
    /// <summary>Gets the active projection settings.</summary>
    public Settings Settings { get; } = settings;

    /// <summary>Gets the metadata cache for the current generation.</summary>
    public MetadataCache Cache { get; } = cache;

    /// <summary>Gets the namespace currently being emitted, or <see cref="string.Empty"/> when not in a per-namespace pass.</summary>
    public string CurrentNamespace { get; } = currentNamespace;
}

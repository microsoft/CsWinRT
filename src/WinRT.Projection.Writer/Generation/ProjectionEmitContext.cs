// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Resolvers;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <summary>
/// Per-emission context bundling all state shared by the projection writers when emitting a
/// single projection (settings, metadata cache, the active namespace, scoped emission-mode flags).
/// </summary>
/// <param name="settings">The active projection settings.</param>
/// <param name="cache">The metadata cache for the current generation.</param>
/// <param name="currentNamespace">The namespace currently being emitted (or <see cref="string.Empty"/> when not in a per-namespace pass).</param>
internal sealed class ProjectionEmitContext(Settings settings, MetadataCache cache, string currentNamespace)
{
    /// <summary>
    /// Gets the active projection settings.
    /// </summary>
    public Settings Settings { get; } = settings;

    /// <summary>
    /// Gets the metadata cache for the current generation.
    /// </summary>
    public MetadataCache Cache { get; } = cache;

    /// <summary>
    /// Gets the namespace currently being emitted, or <see cref="string.Empty"/> when not in a per-namespace pass.
    /// </summary>
    public string CurrentNamespace { get; } = currentNamespace;

    /// <summary>
    /// Gets a value indicating whether the writer is currently emitting inside an
    /// <c>ABI.&lt;Ns&gt;</c> namespace block. Set by
    /// <see cref="ProjectionWriterExtensions.WriteBeginAbiNamespace"/> and reset by
    /// <see cref="ProjectionWriterExtensions.WriteEndAbiNamespace"/>. Used by
    /// <see cref="Helpers.TypedefNameWriter"/> to force the <c>global::&lt;Ns&gt;.</c> prefix on
    /// projected type references inside the ABI block (the ABI section can't see the projected
    /// namespace via using directives, so unqualified names would fail to resolve).
    /// </summary>
    public bool InAbiNamespace { get; set; }

    /// <summary>
    /// Gets a value indicating whether the writer is currently emitting inside an
    /// <c>ABI.Impl.&lt;Ns&gt;</c> namespace block (component-mode authored projections).
    /// </summary>
    public bool InAbiImplNamespace { get; set; }

    /// <summary>
    /// Gets the resolver used to classify type signatures by their ABI marshalling shape.
    /// </summary>
    public AbiTypeShapeResolver AbiTypeShapeResolver { get; } = new AbiTypeShapeResolver(cache);

    /// <summary>
    /// Gets a value indicating whether platform-attribute computation should suppress platforms
    /// that are less than or equal to <see cref="Platform"/>. Used to apply class-scope platform
    /// suppression so member-level <c>[SupportedOSPlatform]</c> attributes don't repeat
    /// information already on the enclosing type. Set via <see cref="EnterPlatformSuppressionScope(string)"/>.
    /// </summary>
    public bool CheckPlatform { get; private set; }

    /// <summary>
    /// Gets the active platform string for the platform-attribute suppression mode. Set initially
    /// by <see cref="EnterPlatformSuppressionScope(string)"/>; subsequently seeded by the
    /// platform-attribute algorithm on the first non-empty observation within the scope via
    /// <see cref="SeedPlatform(string)"/>.
    /// </summary>
    public string Platform { get; private set; } = string.Empty;

    /// <summary>
    /// Seeds <see cref="Platform"/> with the first non-empty platform observed by the platform-
    /// attribute algorithm within an active suppression scope. No-op outside a scope.
    /// </summary>
    /// <param name="platform">The platform string observed at the current emission site.</param>
    public void SeedPlatform(string platform)
    {
        if (CheckPlatform && Platform.Length == 0)
        {
            Platform = platform;
        }
    }

    /// <summary>
    /// Enters platform-attribute suppression mode for the given <paramref name="platform"/>.
    /// Returns an <see cref="IDisposable"/> token that resets <see cref="CheckPlatform"/> and
    /// <see cref="Platform"/> on dispose. Use as
    /// <c>using (context.EnterPlatformSuppressionScope(platform)) { ... }</c>.
    /// </summary>
    /// <param name="platform">The platform string for which member-level attributes are suppressed.</param>
    /// <returns>The scope token.</returns>
    public PlatformSuppressionScope EnterPlatformSuppressionScope(string platform)
    {
        bool prevCheck = CheckPlatform;
        string prevPlatform = Platform;
        CheckPlatform = true;
        Platform = platform;
        return new PlatformSuppressionScope(this, prevCheck, prevPlatform);
    }

    /// <summary>
    /// Scope token for <see cref="EnterPlatformSuppressionScope(string)"/>.
    /// </summary>
    public ref struct PlatformSuppressionScope : IDisposable
    {
        private ProjectionEmitContext? _context;
        private readonly bool _prevCheck;
        private readonly string _prevPlatform;

        internal PlatformSuppressionScope(ProjectionEmitContext context, bool prevCheck, string prevPlatform)
        {
            _context = context;
            _prevCheck = prevCheck;
            _prevPlatform = prevPlatform;
        }

        /// <summary>
        /// Restores the prior platform-suppression state.
        /// </summary>
        public void Dispose()
        {
            if (_context is { } context)
            {
                context.CheckPlatform = _prevCheck;
                context.Platform = _prevPlatform;
                _context = null;
            }
        }
    }
}

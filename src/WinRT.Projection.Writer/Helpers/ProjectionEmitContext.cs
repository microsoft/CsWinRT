// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionWriter.Metadata;
namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Per-emission context bundling all state shared by the projection writers when emitting a
/// single projection (settings, metadata cache, the active namespace, scoped emission-mode flags).
/// </summary>
/// <remarks>
/// The two emission-mode flags (<see cref="InAbiNamespace"/> and <see cref="InAbiImplNamespace"/>)
/// are exposed as read-only properties; callers must enter/leave them via the scoped
/// <see cref="EnterAbiNamespace"/> / <see cref="EnterAbiImplNamespace"/> <see cref="IDisposable"/>
/// helpers, which guarantees the flag is reset even on exceptional control flow.
/// </remarks>
internal sealed class ProjectionEmitContext
{
    /// <summary>Initializes a new <see cref="ProjectionEmitContext"/>.</summary>
    /// <param name="settings">The active projection settings.</param>
    /// <param name="cache">The metadata cache for the current generation.</param>
    /// <param name="currentNamespace">The namespace currently being emitted (or <see cref="string.Empty"/> when not in a per-namespace pass).</param>
    public ProjectionEmitContext(Settings settings, MetadataCache cache, string currentNamespace)
    {
        Settings = settings;
        Cache = cache;
        CurrentNamespace = currentNamespace;
    }

    /// <summary>Gets the active projection settings.</summary>
    public Settings Settings { get; }

    /// <summary>Gets the metadata cache for the current generation.</summary>
    public MetadataCache Cache { get; }

    /// <summary>Gets the namespace currently being emitted, or <see cref="string.Empty"/> when not in a per-namespace pass.</summary>
    public string CurrentNamespace { get; }

    /// <summary>Gets a value indicating whether the writer is currently inside an ABI namespace block.</summary>
    public bool InAbiNamespace { get; private set; }

    /// <summary>Gets a value indicating whether the writer is currently inside an ABI.Impl namespace block.</summary>
    public bool InAbiImplNamespace { get; private set; }

    /// <summary>
    /// Gets a value indicating whether platform-attribute computation should suppress platforms
    /// that are less than or equal to <see cref="Platform"/>. Used to apply class-scope platform
    /// suppression so member-level <c>[SupportedOSPlatform]</c> attributes don't repeat
    /// information already on the enclosing type. Set via <see cref="EnterPlatformSuppressionScope(string)"/>.
    /// </summary>
    public bool CheckPlatform { get; private set; }

    /// <summary>Gets the active platform string for the platform-attribute suppression mode.</summary>
    /// <remarks>
    /// The setter is internal and is used by both the scope helper (to install/restore the
    /// surrounding scope's platform) and the platform-attribute algorithm itself (which seeds
    /// the platform on the first non-empty observation within an active scope).
    /// </remarks>
    public string Platform { get; internal set; } = string.Empty;

    /// <summary>
    /// Enters the ABI namespace mode. Returns an <see cref="IDisposable"/> token that resets the
    /// mode on dispose. Use as <c>using (context.EnterAbiNamespace()) { ... }</c>.
    /// </summary>
    /// <returns>The scope token.</returns>
    public AbiNamespaceScope EnterAbiNamespace()
    {
        InAbiNamespace = true;
        return new AbiNamespaceScope(this);
    }

    /// <summary>
    /// Enters the ABI.Impl namespace mode. Returns an <see cref="IDisposable"/> token that resets
    /// the mode on dispose. Use as <c>using (context.EnterAbiImplNamespace()) { ... }</c>.
    /// </summary>
    /// <returns>The scope token.</returns>
    public AbiImplNamespaceScope EnterAbiImplNamespace()
    {
        InAbiImplNamespace = true;
        return new AbiImplNamespaceScope(this);
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

    /// <summary>Scope token for <see cref="EnterAbiNamespace"/>.</summary>
    public struct AbiNamespaceScope : IDisposable
    {
        private ProjectionEmitContext? _context;

        internal AbiNamespaceScope(ProjectionEmitContext context) { _context = context; }

        /// <summary>Resets the ABI namespace mode.</summary>
        public void Dispose()
        {
            if (_context is { } context)
            {
                context.InAbiNamespace = false;
                _context = null;
            }
        }
    }

    /// <summary>Scope token for <see cref="EnterAbiImplNamespace"/>.</summary>
    public struct AbiImplNamespaceScope : IDisposable
    {
        private ProjectionEmitContext? _context;

        internal AbiImplNamespaceScope(ProjectionEmitContext context) { _context = context; }

        /// <summary>Resets the ABI.Impl namespace mode.</summary>
        public void Dispose()
        {
            if (_context is { } context)
            {
                context.InAbiImplNamespace = false;
                _context = null;
            }
        }
    }

    /// <summary>Scope token for <see cref="EnterPlatformSuppressionScope(string)"/>.</summary>
    public struct PlatformSuppressionScope : IDisposable
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

        /// <summary>Restores the prior platform-suppression state.</summary>
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

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Generation;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="Settings"/>.
/// </summary>
internal static class SettingsExtensions
{
    extension(Settings settings)
    {
        /// <summary>
        /// Gets the accessibility modifier (<c>"public"</c> or <c>"internal"</c>) used for
        /// generated types based on the <see cref="Settings.Internal"/> and
        /// <see cref="Settings.Embedded"/> flags.
        /// </summary>
        public string InternalAccessibility => settings.Internal || settings.Embedded ? "internal" : "public";
    }
}

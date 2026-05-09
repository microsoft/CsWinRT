// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Helpers for choosing the right C# accessibility modifier based on projection settings.
/// </summary>
internal static class AccessibilityHelper
{
    /// <summary>
    /// Returns the accessibility modifier (<c>"public"</c> or <c>"internal"</c>) used for
    /// generated types based on the <see cref="Settings.Internal"/> and
    /// <see cref="Settings.Embedded"/> flags. Mirrors C++ <c>internal_accessibility</c>.
    /// </summary>
    /// <param name="settings">The active projection settings.</param>
    /// <returns><c>"internal"</c> if <see cref="Settings.Internal"/> or <see cref="Settings.Embedded"/> is set; otherwise <c>"public"</c>.</returns>
    public static string InternalAccessibility(Settings settings)
        => settings.Internal || settings.Embedded ? "internal" : "public";
}

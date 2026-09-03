// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// A namespace addition: the content of a <c>.cs</c> file that gets appended to the projection of a
/// namespace, together with the Windows Runtime types it is defined in terms of.
/// </summary>
/// <param name="ResourceName">
/// The embedded-resource manifest name of the addition, as resolved by a
/// <see cref="System.Reflection.Assembly.GetManifestResourceStream(string)"/> call.
/// </param>
/// <param name="RequiredTypes">
/// The full names of the Windows Runtime types the addition is defined in terms of, either because
/// it augments them (eg. <c>partial struct Thickness</c>) or because it is written against them
/// (eg. <c>Duration</c>, which has a field of the metadata-only <c>DurationType</c> enum). The
/// addition is only emitted when the projected metadata declares all of them.
/// </param>
internal readonly record struct Addition(string ResourceName, string[] RequiredTypes);

/// <summary>
/// Registry of namespace addition files.
/// Each addition is the content of a <c>.cs</c> file that gets appended to the
/// projection of the matching namespace.
/// </summary>
internal static class Additions
{
    /// <summary>
    /// Lookup of the additions registered for a given target namespace, in the order they should be
    /// appended to the projection of that namespace.
    /// </summary>
    /// <remarks>
    /// A namespace is not owned by a single metadata source: WinUI 2 for instance reuses the
    /// <c>Microsoft.UI.Xaml</c> namespace but declares none of the XAML value types in it. That is
    /// why each addition also declares the metadata types it needs (see
    /// <see cref="Addition.RequiredTypes"/>), rather than keying off the namespace name alone.
    /// </remarks>
    private static readonly FrozenDictionary<string, Addition[]> ByNamespace = new Dictionary<string, Addition[]>
    {
        ["Microsoft.UI.Dispatching"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Dispatching.Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext.cs",
                ["Microsoft.UI.Dispatching.DispatcherQueue"]),
        ],
        ["Microsoft.UI.Xaml"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.CornerRadius.cs",
                ["Microsoft.UI.Xaml.CornerRadius"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.Duration.cs",
                ["Microsoft.UI.Xaml.Duration", "Microsoft.UI.Xaml.DurationType"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.GridLength.cs",
                ["Microsoft.UI.Xaml.GridLength", "Microsoft.UI.Xaml.GridUnitType"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.SR.cs",
                ["Microsoft.UI.Xaml.CornerRadius", "Microsoft.UI.Xaml.GridLength"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.Thickness.cs",
                ["Microsoft.UI.Xaml.Thickness"]),
        ],
        ["Microsoft.UI.Xaml.Controls.Primitives"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Controls.Primitives.Microsoft.UI.Xaml.Controls.Primitives.GeneratorPosition.cs",
                ["Microsoft.UI.Xaml.Controls.Primitives.GeneratorPosition"]),
        ],
        ["Microsoft.UI.Xaml.Media"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Microsoft.UI.Xaml.Media.Matrix.cs",
                ["Microsoft.UI.Xaml.Media.Matrix"]),
        ],
        ["Microsoft.UI.Xaml.Media.Animation"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Animation.Microsoft.UI.Xaml.Media.Animation.KeyTime.cs",
                ["Microsoft.UI.Xaml.Media.Animation.KeyTime"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Animation.Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.cs",
                ["Microsoft.UI.Xaml.Media.Animation.RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation.RepeatBehaviorType"]),
        ],
        ["Microsoft.UI.Xaml.Media.Media3D"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Media3D.Microsoft.UI.Xaml.Media.Media3D.Matrix3D.cs",
                ["Microsoft.UI.Xaml.Media.Media3D.Matrix3D"]),
        ],
        ["Windows.Storage"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.Storage.WindowsRuntimeStorageExtensions.cs",
                ["Windows.Storage.IStorageFile", "Windows.Storage.IStorageFolder"]),
        ],
        ["Windows.UI"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Windows.UI.Color.cs",
                ["Windows.UI.Color"]),
        ],
        ["Windows.UI.Xaml"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.System.DispatcherQueueSynchronizationContext.cs",
                ["Windows.System.DispatcherQueue"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.CornerRadius.cs",
                ["Windows.UI.Xaml.CornerRadius"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.Duration.cs",
                ["Windows.UI.Xaml.Duration", "Windows.UI.Xaml.DurationType"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.GridLength.cs",
                ["Windows.UI.Xaml.GridLength", "Windows.UI.Xaml.GridUnitType"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.SR.cs",
                ["Windows.UI.Xaml.CornerRadius", "Windows.UI.Xaml.GridLength"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.Thickness.cs",
                ["Windows.UI.Xaml.Thickness"]),
        ],
        ["Windows.UI.Xaml.Controls.Primitives"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Controls.Primitives.Windows.UI.Xaml.Controls.Primitives.GeneratorPosition.cs",
                ["Windows.UI.Xaml.Controls.Primitives.GeneratorPosition"]),
        ],
        ["Windows.UI.Xaml.Media"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Windows.UI.Xaml.Media.Matrix.cs",
                ["Windows.UI.Xaml.Media.Matrix"]),
        ],
        ["Windows.UI.Xaml.Media.Animation"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Animation.Windows.UI.Xaml.Media.Animation.KeyTime.cs",
                ["Windows.UI.Xaml.Media.Animation.KeyTime"]),
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Animation.Windows.UI.Xaml.Media.Animation.RepeatBehavior.cs",
                ["Windows.UI.Xaml.Media.Animation.RepeatBehavior", "Windows.UI.Xaml.Media.Animation.RepeatBehaviorType"]),
        ],
        ["Windows.UI.Xaml.Media.Media3D"] =
        [
            new(
                "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Media3D.Windows.UI.Xaml.Media.Media3D.Matrix3D.cs",
                ["Windows.UI.Xaml.Media.Media3D.Matrix3D"]),
        ],
    }.ToFrozenDictionary();

    /// <summary>
    /// Enumerates the additions registered for the given target namespace, in the order they should
    /// be appended to the projection of that namespace. Returns an empty span when no additions are
    /// registered for <paramref name="ns"/>.
    /// </summary>
    /// <param name="ns">The target namespace.</param>
    /// <returns>The registered additions; an empty span if none.</returns>
    public static ReadOnlySpan<Addition> EnumerateByNamespace(string ns)
    {
        return ByNamespace.TryGetValue(ns, out Addition[]? additions) ? additions : [];
    }
}

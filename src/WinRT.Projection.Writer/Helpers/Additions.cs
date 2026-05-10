// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Registry of namespace addition files. array.
/// Each addition is the content of a <c>.cs</c> file that gets appended to the
/// projection of the matching namespace.
/// </summary>
internal static class Additions
{
    /// <summary>
    /// (namespace, embedded-resource-manifest-name) pairs. The manifest-name resolves to a
    /// <see cref="System.Reflection.Assembly.GetManifestResourceStream(string)"/> call.
    /// </summary>
    public static readonly IReadOnlyList<(string Namespace, string ResourceName)> All =
    [
        ("Microsoft.UI.Dispatching", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Dispatching.Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext.cs"),
        ("Microsoft.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.CornerRadius.cs"),
        ("Microsoft.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.Duration.cs"),
        ("Microsoft.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.GridLength.cs"),
        ("Microsoft.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.SR.cs"),
        ("Microsoft.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Microsoft.UI.Xaml.Thickness.cs"),
        ("Microsoft.UI.Xaml.Controls.Primitives", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Controls.Primitives.Microsoft.UI.Xaml.Controls.Primitives.GeneratorPosition.cs"),
        ("Microsoft.UI.Xaml.Media", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Microsoft.UI.Xaml.Media.Matrix.cs"),
        ("Microsoft.UI.Xaml.Media.Animation", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Animation.Microsoft.UI.Xaml.Media.Animation.KeyTime.cs"),
        ("Microsoft.UI.Xaml.Media.Animation", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Animation.Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.cs"),
        ("Microsoft.UI.Xaml.Media.Media3D", "WindowsRuntime.ProjectionWriter.Resources.Additions.Microsoft.UI.Xaml.Media.Media3D.Microsoft.UI.Xaml.Media.Media3D.Matrix3D.cs"),
        ("Windows.Storage", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.Storage.WindowsRuntimeStorageExtensions.cs"),
        ("Windows.UI", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Windows.UI.Color.cs"),
        ("Windows.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.System.DispatcherQueueSynchronizationContext.cs"),
        ("Windows.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.CornerRadius.cs"),
        ("Windows.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.Duration.cs"),
        ("Windows.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.GridLength.cs"),
        ("Windows.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.SR.cs"),
        ("Windows.UI.Xaml", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Windows.UI.Xaml.Thickness.cs"),
        ("Windows.UI.Xaml.Controls.Primitives", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Controls.Primitives.Windows.UI.Xaml.Controls.Primitives.GeneratorPosition.cs"),
        ("Windows.UI.Xaml.Media", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Windows.UI.Xaml.Media.Matrix.cs"),
        ("Windows.UI.Xaml.Media.Animation", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Animation.Windows.UI.Xaml.Media.Animation.KeyTime.cs"),
        ("Windows.UI.Xaml.Media.Animation", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Animation.Windows.UI.Xaml.Media.Animation.RepeatBehavior.cs"),
        ("Windows.UI.Xaml.Media.Media3D", "WindowsRuntime.ProjectionWriter.Resources.Additions.Windows.UI.Xaml.Media.Media3D.Windows.UI.Xaml.Media.Media3D.Matrix3D.cs"),
    ];
}
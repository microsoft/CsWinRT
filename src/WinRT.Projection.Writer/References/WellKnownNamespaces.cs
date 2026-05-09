// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.References;

/// <summary>
/// Well-known WinRT metadata namespaces.
/// </summary>
internal static class WellKnownNamespaces
{
    /// <summary>The <c>Windows.Foundation</c> namespace.</summary>
    public const string WindowsFoundation = "Windows.Foundation";

    /// <summary>The <c>Windows.Foundation.Metadata</c> namespace where WinRT metadata attributes live.</summary>
    public const string WindowsFoundationMetadata = "Windows.Foundation.Metadata";

    /// <summary>The <c>Windows.Foundation.Collections</c> namespace.</summary>
    public const string WindowsFoundationCollections = "Windows.Foundation.Collections";

    /// <summary>The <c>System</c> namespace (BCL primitives + special types).</summary>
    public const string System = "System";

    /// <summary>The <c>WindowsRuntime.Internal</c> namespace (internal interop interfaces).</summary>
    public const string WindowsRuntimeInternal = "WindowsRuntime.Internal";

    /// <summary>The <c>Windows.UI.Xaml.Interop</c> namespace.</summary>
    public const string WindowsUIXamlInterop = "Windows.UI.Xaml.Interop";
}

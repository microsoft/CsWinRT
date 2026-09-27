// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.Generator.References;

/// <summary>
/// The generator-owned metadata contract carried by Windows Runtime reference projections.
/// </summary>
internal static class WindowsRuntimeReferenceAssemblyMetadata
{
    /// <summary>
    /// The fully qualified name of the key/value metadata attribute.
    /// </summary>
    public const string AttributeTypeName = "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyMetadataAttribute";

    /// <summary>
    /// The key for one fully qualified exclusive-to interface name requiring dynamic interface casting.
    /// Repeated entries form the effective selection; no entries means no exclusive-to IDIC.
    /// </summary>
    public const string IdicExclusiveTo = "CsWinRT.IdicExclusiveTo.v1";
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Carries a key/value metadata pair for a generated Windows Runtime reference projection.
/// </summary>
/// <remarks>
/// CsWinRT emits and consumes this build-time metadata to preserve projection requirements across package boundaries.
/// Like <see cref="WindowsRuntimeReferenceAssemblyAttribute"/>, this attribute remains in the
/// <c>WinRT.Runtime.dll</c> reference assembly so generated reference projections can use it.
/// It is not intended for direct use in user code.
/// </remarks>
/// <seealso cref="System.Reflection.AssemblyMetadataAttribute"/>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeReferenceAssemblyObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeReferenceAssemblyObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class WindowsRuntimeReferenceAssemblyMetadataAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeReferenceAssemblyMetadataAttribute"/> instance.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    public WindowsRuntimeReferenceAssemblyMetadataAttribute(string key, string? value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the metadata value.
    /// </summary>
    public string? Value { get; }
}

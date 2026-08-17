// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.References;

/// <summary>
/// Well-known WinRT type names referenced by the projection writer.
/// </summary>
internal static class WellKnownTypeNames
{
    /// <summary>
    /// The WinRT <c>HResult</c> struct (mapped to <see cref="System.Exception"/>).
    /// </summary>
    public const string HResult = "HResult";

    /// <summary>
    /// The WinRT <c>DateTime</c> struct (mapped to <see cref="System.DateTimeOffset"/>).
    /// </summary>
    public const string DateTime = "DateTime";

    /// <summary>
    /// The WinRT <c>TimeSpan</c> struct (mapped to <see cref="System.TimeSpan"/>).
    /// </summary>
    public const string TimeSpan = "TimeSpan";

    /// <summary>
    /// The WinRT <c>IReference&lt;T&gt;</c> generic interface (mapped to <see cref="System.Nullable{T}"/>).
    /// </summary>
    public const string IReferenceGeneric = "IReference`1";

    /// <summary>
    /// The BCL <c>Nullable&lt;T&gt;</c> generic struct.
    /// </summary>
    public const string NullableGeneric = "Nullable`1";

    /// <summary>
    /// The BCL <see cref="System.Type"/> type name.
    /// </summary>
    public const string Type = "Type";

    /// <summary>
    /// The BCL <see cref="System.Exception"/> type name.
    /// </summary>
    public const string Exception = "Exception";

    /// <summary>
    /// The BCL <see cref="object"/> type name.
    /// </summary>
    public const string Object = "Object";

    /// <summary>
    /// The Windows SDK XAML <c>TypeName</c> struct (the WinMD source for <see cref="System.Type"/>).
    /// </summary>
    public const string TypeName = "TypeName";

    /// <summary>
    /// The XAML <c>DependencyProperty</c> type (in either <c>Microsoft.UI.Xaml</c> for WinUI or
    /// <c>Windows.UI.Xaml</c> for UWP XAML). Authored types register dependency properties as
    /// <c>static</c> fields of this type.
    /// </summary>
    public const string DependencyProperty = "DependencyProperty";
}

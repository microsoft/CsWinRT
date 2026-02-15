// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Specifies the marshaling type to use to marshal a given Windows Runtime object, specifically when creating a <see cref="WindowsRuntimeObjectReference"/> instance.
/// </summary>
/// <seealso cref="Windows.Foundation.Metadata.MarshalingType"/>
internal enum CreateObjectReferenceMarshalingType
{
    /// <summary>
    /// No static type information is available to known in advance the marshaling type for the class.
    /// </summary>
    Unknown,

    /// <inheritdoc cref="Windows.Foundation.Metadata.MarshalingType.Agile"/>
    Agile,

    /// <inheritdoc cref="Windows.Foundation.Metadata.MarshalingType.Standard"/>
    Standard,
}
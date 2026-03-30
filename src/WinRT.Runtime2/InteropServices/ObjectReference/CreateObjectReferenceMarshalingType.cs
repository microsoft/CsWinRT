// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Specifies the marshaling type to use to marshal a given Windows Runtime object, specifically when creating a <see cref="WindowsRuntimeObjectReference"/> instance.
/// </summary>
/// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.metadata.marshalingtype"/>
public enum CreateObjectReferenceMarshalingType
{
    /// <summary>
    /// No static type information is available to know in advance the marshaling type for the class.
    /// </summary>
    Unknown,

    /// <summary>
    /// The class marshals and unmarshals to the same pointer value on all interfaces.
    /// </summary>
    Agile,

    /// <summary>
    /// The class does not implement <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"><c>IMarshal</c></see> or forwards to
    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cogetstandardmarshal"><c>CoGetStandardMarshal</c></see> on all interfaces.
    /// </summary>
    Standard
}
#endif
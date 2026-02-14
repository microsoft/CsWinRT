// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using WindowsRuntime;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies the marshaling type for the class.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.metadata.marshalingtype"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.Foundation.Metadata.MarshalingType>")]
[WindowsRuntimeReferenceType(typeof(MarshalingType?))]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public enum MarshalingType
{
    /// <summary>
    /// The class can't be marshaled.
    /// </summary>
    InvalidMarshaling = 0,

    /// <summary>
    /// The class prevents marshaling on all interfaces.
    /// </summary>
    None = 1,

    /// <summary>
    /// The class marshals and unmarshals to the same pointer value on all interfaces.
    /// </summary>
    Agile = 2,

    /// <summary>
    /// The class does not implement <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"><c>IMarshal</c></see> or forwards to
    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cogetstandardmarshal"><c>CoGetStandardMarshal</c></see> on all interfaces.
    /// </summary>
    Standard = 3,
}
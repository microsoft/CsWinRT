// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime.InteropServices;

namespace Windows.UI.Xaml.Interop;

/// <summary>
/// Provides basic guidance about the origin of a type.
/// </summary>
/// <remarks>
/// This type is required for ABI projection of the <see cref="System.Type"/> class, but marshalling it is not supported.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.typekind"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[EditorBrowsable(EditorBrowsableState.Never)]
public enum TypeKind
{
    /// <summary>
    /// The type is a language-level primitive.
    /// </summary>
    Primitive,

    /// <summary>
    /// The type is declared through WinMD (Windows Runtime metadata).
    /// </summary>
    Metadata,

    /// <summary>
    /// The type is a custom type declared by means other than WinMD.
    /// </summary>
    Custom
}

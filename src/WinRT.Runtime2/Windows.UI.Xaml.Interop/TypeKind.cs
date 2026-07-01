// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime;

namespace Windows.UI.Xaml.Interop;

/// <summary>
/// Provides basic guidance about the origin of a type.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.typekind"/>
[WindowsRuntimeImplementationOnlyMember]
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

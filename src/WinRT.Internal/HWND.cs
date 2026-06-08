// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable IDE1006

namespace WindowsRuntime.Internal;

/// <summary>
/// Windows Runtime metadata representation of the Win32 <c>HWND</c> type.
/// </summary>
/// <remarks>
/// CsWinRT applies a custom mapping that projects <see cref="HWND"/> to <see cref="nint"/>
/// in generated C# code, so that interop methods on the runtime classes appear with normal handle
/// parameters. The <see cref="__Reserved"/> field exists only to give the struct a representable layout
/// in the Windows Runtime type system.
/// </remarks>
/// <see href="https://learn.microsoft.com/windows/win32/winprog/windows-data-types"/>
public struct HWND
{
    /// <summary>
    /// Reserved field. Not used by callers: the type is mapped to <see cref="nint"/> at the projection layer.
    /// </summary>
    public int __Reserved;
}

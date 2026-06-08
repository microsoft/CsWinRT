// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.Internal;

/// <summary>
/// Windows Runtime metadata representation of the Win32 <c>HWND</c> type.
/// </summary>
/// <remarks>
/// CsWinRT applies a custom mapping that projects <see cref="HWND"/> to <see cref="System.IntPtr"/>
/// in generated C# code, so that interop methods on the runtime classes appear with normal handle
/// parameters. The <see cref="unused"/> field exists only to give the struct a representable layout
/// in the Windows Runtime type system.
/// </remarks>
public struct HWND
{
    /// <summary>
    /// Reserved field. Not used by callers; the type is mapped to <see cref="System.IntPtr"/>
    /// at the projection layer.
    /// </summary>
#pragma warning disable IDE1006 // Naming Styles
    public int unused;
#pragma warning restore IDE1006
}

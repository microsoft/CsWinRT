// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// The compiler-independent result of analyzing a call, for both XAML modes.
/// </summary>
/// <param name="WinUIIid">The WinUI IID, if available.</param>
/// <param name="UwpIid">The UWP IID, if available.</param>
/// <param name="Version">The compiler's location encoding version.</param>
/// <param name="Data">The compiler's opaque location encoding.</param>
/// <param name="TypeName">The type argument of the call, formatted to be used in an XML documentation <c>cref</c>.</param>
internal sealed record GetIidCallInfo(
    Guid? WinUIIid,
    Guid? UwpIid,
    int Version,
    string Data,
    string TypeName);

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Controls which assemblies the interop generator analyzes when discovering user-defined (CCW) types and
/// generic instantiations that require marshalling code.
/// </summary>
/// <remarks>
/// Assemblies that reference the Windows Runtime assembly (i.e. those targeting a Windows TFM) are always
/// analyzed, regardless of the selected mode. The mode only affects assemblies that do not reference any
/// CsWinRT assembly, which lets projects that don't target a Windows TFM (e.g. a class library with just
/// MVVM viewmodels) still contribute the marshalling code their types need.
/// </remarks>
internal enum CsWinRTMarshallingMode
{
    /// <summary>
    /// Analyzes all assemblies, including those that don't reference any CsWinRT assembly.
    /// </summary>
    All,

    /// <summary>
    /// Analyzes all assemblies except those from the .NET base class library (BCL), to reduce binary size. This is the default.
    /// </summary>
    Minimal,

    /// <summary>
    /// Only analyzes assemblies that reference the Windows Runtime assembly (i.e. those targeting a Windows TFM).
    /// </summary>
    Strict
}

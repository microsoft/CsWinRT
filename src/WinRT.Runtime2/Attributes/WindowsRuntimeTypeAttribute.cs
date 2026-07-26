// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime;

/// <summary>
/// Indicates that a given type is a Windows Runtime type (either a projected type, or a proxy type for a custom-mapped type).
/// </summary>
/// <remarks>
/// <para>
/// This is a marker attribute carrying no data: it only signals that the decorated type participates in Windows Runtime
/// marshalling. The mapping from a projected type to its source Windows Runtime metadata file (.winmd) is instead recorded
/// on the centralized <c>ABI.WindowsRuntimeMetadataTypes</c> lookup type (via <c>WindowsRuntimeMetadataAttribute</c>),
/// so that the (build-time only) metadata can be trimmed away when not needed.
/// </para>
/// <para>
/// This attribute is emitted by CsWinRT, and it is not meant to be used directly. Unlike most other CsWinRT implementation
/// details, it is not stripped from the <c>WinRT.Runtime.dll</c> reference assembly: it is applied to the interface
/// declarations CsWinRT generates into a component's own assembly when implementing Windows Runtime types defined in an
/// existing <c>.winmd</c> (see the <c>CsWinRTImplementWinMDType</c> build item), so it must remain resolvable there.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeTypeObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeTypeObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class WindowsRuntimeTypeAttribute : Attribute;

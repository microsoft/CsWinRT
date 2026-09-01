// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
#endif

namespace WindowsRuntime;

/// <summary>
/// Marks the abstract base classes that CsWinRT generates to let a Windows Runtime class declared in existing
/// Windows Runtime metadata (.winmd) be implemented in C#, so that an implementation deriving from one of them
/// can be recognized without reflection.
/// </summary>
/// <remarks>
/// <para>
/// An implementation does not derive from the projected class (that class is the runtime callable wrapper, and
/// is often <see langword="sealed"/>), so the two are unrelated types. Marshalling has to tell them apart: when
/// an implementation crosses the ABI and comes back, callers expect the projected type, so its COM Callable
/// Wrapper must be wrapped into one rather than unwrapped back to the implementation. This interface is what
/// makes that check a single type test on a marshalling path where reflection would be too expensive.
/// </para>
/// <para>
/// It carries no members, and is deliberately not a Windows Runtime type: it never appears in the COM Callable
/// Wrapper's interface entries, which only include Windows Runtime interfaces.
/// </para>
/// <para>
/// This interface is not meant to be used directly. Like <see cref="WindowsRuntimeImplementableClassAttribute"/>,
/// it is not stripped from the <c>WinRT.Runtime.dll</c> reference assembly, because the reference projections
/// that carry these base classes are compiled against it.
/// </para>
/// </remarks>
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeImplementableClassObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeImplementableClassObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public interface IWindowsRuntimeImplementableClass;

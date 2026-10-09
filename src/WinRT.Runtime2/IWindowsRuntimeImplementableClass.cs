// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

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
/// Only the generated implementation projection declares it, which is what implementations derive from at runtime.
/// Reference projections leave it out, as nothing compiled against them needs it.
/// </para>
/// </remarks>
[WindowsRuntimeImplementationOnlyMember]
public interface IWindowsRuntimeImplementableClass;

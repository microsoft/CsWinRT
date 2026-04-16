// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace WindowsRuntime;

/// <summary>
/// Indicates a member that is an implementation-only member, meaning it should only be included in the <c>WinRT.Runtime.dll</c>
/// assembly when producing the implementation .dll (not the reference assemlby) that will be used by downstream tooling too.
/// </summary>
/// <remarks>
/// This attribute is used to make it clearer which members are implementation-only members (rather than just relying on whether
/// they are excluded from the compilation in some way). Additionally, this attribute is actually only emitted when producing
/// reference assemblies (where such members wouldn't be present anyway), so it can be fully stripped in the actual .dll used.
/// </remarks>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
[Conditional("WINDOWS_RUNTIME_REFERENCE_ASSEMBLY")]
internal sealed class WindowsRuntimeImplementationOnlyMemberAttribute : Attribute;
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Excludes a managed helper's implementation from Windows Runtime interop type discovery.
/// </summary>
/// <remarks>
/// <para>
/// The exclusion covers members declared in the annotated type and its lexically nested types,
/// including compiler-generated helpers. It does not apply to derived types or referenced types.
/// </para>
/// <para>
/// Signatures used by unexcluded callers remain discoverable. This attribute does not disable
/// marshalling of the annotated type or exclude an independently used generic instantiation.
/// </para>
/// <para>
/// The annotated implementation requires no interop support beyond what is discovered from
/// unexcluded code. Reflection-only uses and hidden interop operations are not covered.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class WindowsRuntimeExcludeFromInteropAnalysisAttribute : Attribute;

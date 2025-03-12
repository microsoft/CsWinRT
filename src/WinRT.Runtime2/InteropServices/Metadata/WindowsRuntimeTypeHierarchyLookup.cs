// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices.Metadata;

/// <summary>
/// A hierarchy lookup to use to create managed instances of derived Windows Runtime types.
/// </summary>
/// <param name="runtimeClassName">The runtime class name of some Windows Runtime type.</param>
/// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
/// <returns>Whether <paramref name="baseRuntimeClassName"/> was successfully retrieved.</returns>
/// <remarks>
/// <para>
/// The input value is the runtime class name of some Windows Runtime type (guaranteed by CsWinRT to not be empty).
/// </para>
/// <para>
/// The semantics of target methods should be as follows:
/// <list type="bullet">
///   <item>The <paramref name="baseRuntimeClassName"/> value should be set to the runtime class name of the base type for the input type, if it exists.</item>
///   <item>If the input runtime class name is not recognized, or if there is no base type, the method returns <see langword="false"/>.</item>
/// </list>
/// </para>
/// <para>
/// This API is for use by the output of the CsWinRT source generators, and it should not be used directly by user code.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate bool WindowsRuntimeTypeHierarchyLookup(
    scoped ReadOnlySpan<char> runtimeClassName,
    out ReadOnlySpan<char> baseRuntimeClassName);

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Internal;

/// <summary>
/// Marks a Windows Runtime interface as having an "internal" projection in CsWinRT.
/// </summary>
/// <remarks>
/// <para>
/// CsWinRT generates an <see langword="internal"/> projection for any interface marked with this attribute
/// (rather than the default <see langword="public"/> projection). User-friendly wrappers over the internal
/// projection are exposed via hand-authored extension methods (see e.g. <c>ComInteropExtensions</c>).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionInternalAttribute : Attribute;

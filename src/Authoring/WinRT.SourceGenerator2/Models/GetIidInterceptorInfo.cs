// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// The value-equatable data needed to intercept a call, without retaining its semantic model.
/// </summary>
/// <param name="Iid">The precomputed IID.</param>
/// <param name="Version">The compiler's location encoding version.</param>
/// <param name="Data">The compiler's opaque location encoding.</param>
/// <param name="TypeName">The type argument of the call, formatted to be used in an XML documentation <c>cref</c>.</param>
internal sealed record GetIidInterceptorInfo(
    Guid Iid,
    int Version,
    string Data,
    string TypeName);

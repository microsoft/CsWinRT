// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Immutable record describing the ABI marshalling shape of a single WinRT type signature
/// (the kind of marshalling needed, plus the underlying type signature).
/// </summary>
/// <param name="Kind">The classification of the type signature's ABI shape.</param>
/// <param name="Signature">The original type signature this shape was derived from.</param>
internal sealed record AbiTypeShape(AbiTypeShapeKind Kind, TypeSignature Signature);

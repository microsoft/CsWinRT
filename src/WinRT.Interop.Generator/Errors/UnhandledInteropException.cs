// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.InteropGenerator.Errors;

/// <summary>
/// An unhandled exception for the interop generator.
/// </summary>
/// <inheritdoc cref="UnhandledGeneratorException(string, Exception)"/>
internal sealed class UnhandledInteropException(string phase, Exception exception)
    : UnhandledGeneratorException(phase, exception)
{
    /// <inheritdoc/>
    protected override string ErrorPrefix => WellKnownInteropExceptions.ErrorPrefix;

    /// <inheritdoc/>
    protected override string GeneratorName => "interop";
}

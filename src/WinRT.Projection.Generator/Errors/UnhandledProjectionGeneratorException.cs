// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// An unhandled exception for the projection generator.
/// </summary>
/// <inheritdoc cref="UnhandledGeneratorException(string, Exception)"/>
internal sealed class UnhandledProjectionGeneratorException(string phase, Exception exception)
    : UnhandledGeneratorException(phase, exception)
{
    /// <inheritdoc/>
    protected override string ErrorPrefix => WellKnownProjectionGeneratorExceptions.ErrorPrefix;

    /// <inheritdoc/>
    protected override string GeneratorDescription => "projection generator";
}


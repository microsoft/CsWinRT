// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ReferenceProjectionGenerator.Errors;

/// <summary>
/// An unhandled exception for the reference projection generator.
/// </summary>
/// <inheritdoc cref="UnhandledGeneratorException(string, Exception)"/>
internal sealed class UnhandledReferenceProjectionGeneratorException(string phase, Exception exception)
    : UnhandledGeneratorException(phase, exception)
{
    /// <inheritdoc/>
    protected override string ErrorPrefix => WellKnownReferenceProjectionGeneratorExceptions.ErrorPrefix;

    /// <inheritdoc/>
    protected override string GeneratorDescription => "reference projection generator";
}


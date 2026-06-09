// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ImplGenerator.Errors;

/// <summary>
/// An unhandled exception for the impl generator.
/// </summary>
/// <inheritdoc cref="UnhandledGeneratorException(string, Exception)"/>
internal sealed class UnhandledImplException(string phase, Exception exception)
    : UnhandledGeneratorException(phase, exception)
{
    /// <inheritdoc/>
    protected override string ErrorPrefix => WellKnownImplExceptions.ErrorPrefix;

    /// <inheritdoc/>
    protected override string GeneratorDescription => "impl generator";
}

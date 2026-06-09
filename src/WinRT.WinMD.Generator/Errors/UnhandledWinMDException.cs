// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.WinMDGenerator.Errors;

/// <summary>
/// An unhandled exception for the WinMD generator.
/// </summary>
/// <inheritdoc cref="UnhandledGeneratorException(string, Exception)"/>
internal sealed class UnhandledWinMDException(string phase, Exception exception)
    : UnhandledGeneratorException(phase, exception)
{
    /// <inheritdoc/>
    protected override string ErrorPrefix => WellKnownWinMDExceptions.ErrorPrefix;

    /// <inheritdoc/>
    protected override string GeneratorDescription => "WinMD generator";
}

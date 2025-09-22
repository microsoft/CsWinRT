// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// An unhandled exception for the projection generator.
/// </summary>
internal sealed class UnhandledProjectionException : UnhandledGeneratorException
{
    /// <summary>
    /// Creates a new <see cref="UnhandledProjectionException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="phase">The phase that failed.</param>
    /// <param name="exception">The inner exception.</param>
    public UnhandledProjectionException(string phase, Exception exception)
        : base("projection", phase, exception)
    {
    }
}

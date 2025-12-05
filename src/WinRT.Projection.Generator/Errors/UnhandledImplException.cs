// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// An unhandled exception for the projection generator.
/// </summary>
internal sealed class UnhandledProjectionGeneratorException : Exception
{
    /// <summary>
    /// The phase that failed.
    /// </summary>
    private readonly string _phase;

    /// <summary>
    /// Creates a new <see cref="UnhandledProjectionGeneratorException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="phase">The phase that failed.</param>
    /// <param name="exception">The inner exception.</param>
    public UnhandledProjectionGeneratorException(string phase, Exception exception)
        : base(null, exception)
    {
        _phase = phase;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"""error {WellKnownProjectionGeneratorExceptions.ErrorPrefix}9999: The CsWinRT projection generator failed with an unhandled exception """ +
            $"""('{InnerException!.GetType().Name}': '{InnerException!.Message}') during the {_phase} phase. This might be due to an invalid """ +
            $"""configuration in the current project, but the generator should still correctly identify that and fail gracefully. Please open an """ +
            $"""issue at https://github.com/microsoft/CsWinRT and provide a minimal repro, if possible.""";
    }
}

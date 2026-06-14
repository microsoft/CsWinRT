// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.ProjectionWriter.Errors;

/// <summary>
/// An unhandled exception for the projection writer.
/// </summary>
internal sealed class UnhandledProjectionWriterException : Exception
{
    /// <summary>
    /// The phase that failed.
    /// </summary>
    private readonly string _phase;

    /// <summary>
    /// Creates a new <see cref="UnhandledProjectionWriterException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="phase">The phase that failed.</param>
    /// <param name="exception">The inner exception.</param>
    public UnhandledProjectionWriterException(string phase, Exception exception)
        : base(null, exception)
    {
        _phase = phase;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"""error {WellKnownProjectionWriterExceptions.ErrorPrefix}9999: The CsWinRT projection writer failed with an unhandled exception """ +
            $"""('{InnerException!.GetType().Name}': '{InnerException!.Message}') during the '{_phase}' phase. This might be due to an invalid """ +
            $"""configuration in the current project, but the writer should still correctly identify that and fail gracefully. Please open an """ +
            $"""issue at https://github.com/microsoft/CsWinRT and provide a minimal repro, if possible.""";
    }
}

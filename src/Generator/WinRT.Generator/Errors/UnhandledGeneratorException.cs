// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// An unhandled exception for the generator.
/// </summary>
internal class UnhandledGeneratorException : Exception
{
    /// <summary>
    /// The generator that failed.
    /// </summary>
    private readonly string _generator;

    /// <summary>
    /// The phase that failed.
    /// </summary>
    private readonly string _phase;

    /// <summary>
    /// Creates a new <see cref="UnhandledGeneratorException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="generator">The generator that failed.</param>
    /// <param name="phase">The phase that failed.</param>
    /// <param name="exception">The inner exception.</param>
    public UnhandledGeneratorException(string generator, string phase, Exception exception)
        : base(null, exception)
    {
        _generator = generator;
        _phase = phase;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"""error CSWINRT{_generator.ToUpper()}GEN9999: The CsWinRT {_generator} generator failed with an unhandled exception """ +
            $"""('{InnerException!.GetType().Name}': '{InnerException!.Message}') during the {_phase} phase. This might be due to an invalid """ +
            $"""configuration in the current project, but the generator should still correctly identify that and fail gracefully. Please open an """ +
            $"""issue at https://github.com/microsoft/CsWinRT and provide a minimal repro, if possible.""";
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// An unhandled exception for a CsWinRT CLI generator.
/// </summary>
/// <remarks>
/// Each per-tool unhandled exception inherits from this type and provides its
/// <see cref="ErrorPrefix"/> and <see cref="GeneratorName"/> so that the standardized
/// <see cref="ToString"/> message remains tool-specific.
/// </remarks>
/// <param name="phase">The phase that failed.</param>
/// <param name="exception">The inner exception.</param>
internal abstract class UnhandledGeneratorException(string phase, Exception exception)
    : Exception(null, exception)
{
    /// <summary>
    /// Gets the error prefix for the per-tool exception ID (e.g. <c>"CSWINRTIMPLGEN"</c>).
    /// </summary>
    protected abstract string ErrorPrefix { get; }

    /// <summary>
    /// Gets the name of the generator used in the standard message.
    /// </summary>
    protected abstract string GeneratorName { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"""error {ErrorPrefix}9999: The CsWinRT {GeneratorName} generator failed with an unhandled exception """ +
            $"""('{InnerException!.GetType().Name}': '{InnerException!.Message}') during the '{phase}' phase. This might be due to an invalid """ +
            $"""configuration in the current project, but the generator should still correctly identify that and fail gracefully. Please open an """ +
            $"""issue at https://github.com/microsoft/CsWinRT and provide a minimal repro, if possible.""";
    }
}


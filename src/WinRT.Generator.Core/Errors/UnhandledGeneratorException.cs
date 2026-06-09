// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// An unhandled exception for a CsWinRT CLI generator.
/// </summary>
/// <remarks>
/// Each per-tool unhandled exception inherits from this type and provides its
/// <see cref="ErrorPrefix"/> and <see cref="GeneratorDescription"/> so that the standardized
/// <see cref="ToString"/> message remains tool-specific. <see cref="QuotePhaseInMessage"/>
/// defaults to <see langword="false"/>; the interop generator overrides it to <see langword="true"/>
/// (its historical message wraps the phase name in single quotes).
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
    /// Gets the description of the generator used in the standard message
    /// (e.g. <c>"impl generator"</c>, <c>"interop generator"</c>, <c>"WinMD generator"</c>).
    /// </summary>
    protected abstract string GeneratorDescription { get; }

    /// <summary>
    /// Gets a value indicating whether the phase name should be wrapped in single quotes in the message.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>. The interop generator historically wraps the phase name
    /// in single quotes and overrides this to <see langword="true"/> to preserve that behavior.
    /// </remarks>
    protected virtual bool QuotePhaseInMessage => false;

    /// <inheritdoc/>
    public override string ToString()
    {
        string formattedPhase = QuotePhaseInMessage ? $"'{phase}'" : phase;

        return
            $"""error {ErrorPrefix}9999: The CsWinRT {GeneratorDescription} failed with an unhandled exception """ +
            $"""('{InnerException!.GetType().Name}': '{InnerException!.Message}') during the {formattedPhase} phase. This might be due to an invalid """ +
            $"""configuration in the current project, but the generator should still correctly identify that and fail gracefully. Please open an """ +
            $"""issue at https://github.com/microsoft/CsWinRT and provide a minimal repro, if possible.""";
    }
}

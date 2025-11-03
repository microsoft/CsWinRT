// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropGenerator.Errors;

/// <summary>
/// A well known warning for the interop generator.
/// </summary>
internal sealed class WellKnownInteropWarning
{
    /// <summary>
    /// The id of the warning.
    /// </summary>
    private readonly string _id;

    /// <summary>
    /// The warning message.
    /// </summary>
    private readonly string _message;

    /// <summary>
    /// Creates a new <see cref="WellKnownInteropException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="id">The id of the warning.</param>
    /// <param name="message">The warning message.</param>
    public WellKnownInteropWarning(string id, string message)
    {
        _id = id;
        _message = message;
    }

    /// <summary>
    /// Logs the warning or throws, depending on <paramref name="treatWarningsAsErrors"/>.
    /// </summary>
    /// <param name="treatWarningsAsErrors">Whether to treat warnings as errors.</param>
    /// <exception cref="WellKnownInteropException">Thrown if <paramref name="treatWarningsAsErrors"/> is <see langword="true"/>.</exception>
    [StackTraceHidden]
    public void LogOrThrow([DoesNotReturnIf(true)] bool treatWarningsAsErrors)
    {
        if (treatWarningsAsErrors)
        {
            throw new WellKnownInteropException(_id, _message, innerException: null);

        }

        Console.WriteLine($"warning {_id}: {_message}");
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// A well-known informational message shared by all generators.
/// </summary>
/// <remarks>
/// Unlike a warning, a message is purely informational: it is never promoted to an error, and it is surfaced
/// by MSBuild as a build message rather than a warning (the emitted line intentionally omits the
/// <c>warning</c>/<c>error</c> category keyword that MSBuild's output parser looks for). It is distinct from
/// <see cref="WellKnownGeneratorMessages"/>, which holds the message templates for the shared logical errors.
/// </remarks>
internal sealed class WellKnownGeneratorMessage
{
    /// <summary>
    /// The id of the message.
    /// </summary>
    private readonly string _id;

    /// <summary>
    /// The message text.
    /// </summary>
    private readonly string _message;

    /// <summary>
    /// Creates a new <see cref="WellKnownGeneratorMessage"/> instance with the specified parameters.
    /// </summary>
    /// <param name="id">The id of the message.</param>
    /// <param name="message">The message text.</param>
    public WellKnownGeneratorMessage(string id, string message)
    {
        _id = id;
        _message = message;
    }

    /// <summary>
    /// Logs the message through the provided logger.
    /// </summary>
    /// <param name="log">The logger to write the message to (typically <c>ConsoleApp.Log</c> from ConsoleAppFramework).</param>
    public void Log(Action<string> log)
    {
        log($"{_id}: {_message}");
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator.Errors;

/// <summary>
/// A well-known informational message for the interop generator.
/// </summary>
/// <remarks>
/// Unlike <see cref="WellKnownInteropWarning"/>, a message is purely informational: it is never promoted to an
/// error, and it is surfaced by MSBuild as a build message rather than a warning (the emitted line intentionally
/// omits the <c>warning</c>/<c>error</c> category keyword that MSBuild's output parser looks for).
/// </remarks>
internal sealed class WellKnownInteropMessage
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
    /// Creates a new <see cref="WellKnownInteropMessage"/> instance with the specified parameters.
    /// </summary>
    /// <param name="id">The id of the message.</param>
    /// <param name="message">The message text.</param>
    public WellKnownInteropMessage(string id, string message)
    {
        _id = id;
        _message = message;
    }

    /// <summary>
    /// Logs the message to the standard output.
    /// </summary>
    public void Log()
    {
        Console.WriteLine($"{_id}: {_message}");
    }
}

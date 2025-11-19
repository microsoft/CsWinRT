// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.ImplGenerator.Errors;

/// <summary>
/// A well known exceptions for the interop generator.
/// </summary>
internal sealed class WellKnownImplException : Exception
{
    /// <summary>
    /// Creates a new <see cref="WellKnownImplException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="id">The id of the exception.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WellKnownImplException(string id, string message, Exception? innerException)
        : base(message, innerException)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the id of the exception.
    /// </summary>
    public string Id { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return InnerException is not null
            ? $"""error {Id}: {Message} Inner exception: '{InnerException!.GetType().Name}': '{InnerException!.Message}'."""
            : $"""error {Id}: {Message}""";
    }
}
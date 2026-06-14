// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// A well-known exception for a CsWinRT CLI generator.
/// </summary>
/// <remarks>
/// Each per-tool well-known exception inherits from this type and adds a marker
/// (e.g. <c>WellKnownImplException</c>) so the concrete runtime type remains tool-specific.
/// The base class provides the shared <see cref="Id"/> field and <see cref="ToString"/> logic,
/// which matches the simple per-tool format <c>error {Id}: {Message}[ Inner exception: ...]</c>.
/// </remarks>
/// <param name="id">The id of the exception (e.g. <c>"CSWINRTIMPLGEN0001"</c>).</param>
/// <param name="message">The exception message.</param>
/// <param name="innerException">The inner exception, if any.</param>
internal abstract class WellKnownGeneratorException(string id, string message, Exception? innerException)
    : Exception(message, innerException)
{
    /// <summary>
    /// Gets the id of the exception (e.g. <c>"CSWINRTIMPLGEN0001"</c>).
    /// </summary>
    public string Id { get; } = id;

    /// <inheritdoc/>
    public override string ToString()
    {
        return InnerException is not null
            ? $"""error {Id}: {Message} Inner exception: '{InnerException.GetType().Name}': '{InnerException.Message}'."""
            : $"""error {Id}: {Message}""";
    }
}

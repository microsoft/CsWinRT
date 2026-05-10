// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace WindowsRuntime.ProjectionWriter.Errors;

/// <summary>
/// Well-known exceptions produced by the projection writer.
/// </summary>
internal static class WellKnownProjectionWriterExceptions
{
    /// <summary>The prefix for all error IDs produced by this tool.</summary>
    public const string ErrorPrefix = "CSWINRTPROJECTIONGEN";

    /// <summary>
    /// Raised when an internal invariant about a referenced type fails (e.g. an
    /// expected type definition cannot be resolved). Replaces ad-hoc
    /// <see cref="InvalidOperationException"/> throws scattered through the writer.
    /// </summary>
    /// <param name="message">The message describing the failed invariant.</param>
    /// <returns>The constructed exception (callers are expected to <c>throw</c> the result).</returns>
    public static WellKnownProjectionWriterException InternalInvariantFailed(string message)
    {
        return Exception(1, message);
    }

    /// <summary>
    /// Raised when a metadata type referenced from an emission helper cannot be resolved.
    /// </summary>
    /// <param name="typeName">The fully-qualified name of the unresolved type.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException CannotResolveType(string typeName)
    {
        return Exception(2, $"The type '{typeName}' could not be resolved against the metadata cache.");
    }

    private static WellKnownProjectionWriterException Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownProjectionWriterException(
            ErrorPrefix + id.ToString("D4", CultureInfo.InvariantCulture),
            message,
            innerException);
    }
}
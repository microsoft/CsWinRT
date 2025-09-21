// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// Well known exceptions for the generator.
/// </summary>
internal static class WellKnownGeneratorExceptions
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTGEN";

    /// <summary>
    /// Some exception was thrown when trying to read the response file.
    /// </summary>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(28, "Failed to read the response file to run 'cswinrtgen'.", exception);
    }

    /// <summary>
    /// Failed to parse an argument from the response file.
    /// </summary>
    public static Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(29, $"Failed to parse argument '{argumentName}' from response file.", exception);
    }

    /// <summary>
    /// The input response file is malformed.
    /// </summary>
    public static Exception MalformedResponseFile()
    {
        return Exception(30, "The response file is malformed and contains invalid content.");
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    /// <param name="id">The exception id.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>The resulting exception.</returns>
    private static Exception Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownGeneratorException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}

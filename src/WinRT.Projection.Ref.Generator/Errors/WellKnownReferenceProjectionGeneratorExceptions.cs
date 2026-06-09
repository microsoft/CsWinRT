// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.GeneratorCli.Errors;

namespace WindowsRuntime.ReferenceProjectionGenerator.Errors;

/// <summary>
/// Well known exceptions for the reference projection generator.
/// </summary>
internal sealed class WellKnownReferenceProjectionGeneratorExceptions : IGeneratorErrorFactory
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTPROJECTIONREFGEN";

    /// <summary>
    /// Prevents external instantiation; this type is only used to dispatch through <see cref="IGeneratorErrorFactory"/>.
    /// </summary>
    private WellKnownReferenceProjectionGeneratorExceptions()
    {
    }

    /// <summary>
    /// Some exception was thrown when trying to read the response file.
    /// </summary>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, "Failed to read the response file to run 'cswinrtprojectionrefgen'.", exception);
    }

    /// <summary>
    /// Failed to parse an argument from the response file.
    /// </summary>
    public static Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(2, $"Failed to parse argument '{argumentName}' from response file.", exception);
    }

    /// <summary>
    /// The input response file is malformed.
    /// </summary>
    public static Exception MalformedResponseFile()
    {
        return Exception(3, "The response file is malformed and contains invalid content.");
    }

    /// <summary>
    /// The supplied target framework is not supported by CsWinRT 3.0.
    /// </summary>
    public static Exception UnsupportedTargetFramework(string targetFramework)
    {
        return Exception(4, $"The target framework '{targetFramework}' is not supported. CsWinRT 3.0 requires .NET 10 or later.");
    }

    /// <summary>
    /// The projection writer failed during source generation.
    /// </summary>
    public static Exception CsWinRTProcessError(Exception exception)
    {
        return Exception(5, "The projection writer failed during source generation.", exception);
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(6, $"The debug repro directory '{path}' does not exist.");
    }

    /// <summary>
    /// The debug repro contains a file entry that has no mapping.
    /// </summary>
    public static Exception DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(7, $"The debug repro file entry with path '{path}' is missing its assembly path mapping.");
    }

    /// <summary>
    /// The debug repro contains a file entry that was not recognized.
    /// </summary>
    public static Exception DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(8, $"The debug repro file entry with path '{path}' was not recognized.");
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
        return new WellKnownReferenceProjectionGeneratorException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}

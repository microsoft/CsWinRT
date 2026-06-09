// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

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

    /// <inheritdoc cref="IGeneratorErrorFactory.ResponseFileReadError(Exception)"/>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, WellKnownGeneratorMessages.ResponseFileReadError("cswinrtprojectionrefgen"), exception);
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.ResponseFileArgumentParsingError(string, Exception?)"/>
    public static Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(2, WellKnownGeneratorMessages.ResponseFileArgumentParsingError(argumentName), exception);
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.MalformedResponseFile"/>
    public static Exception MalformedResponseFile()
    {
        return Exception(3, WellKnownGeneratorMessages.MalformedResponseFile);
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

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproDirectoryDoesNotExist(string)"/>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(6, WellKnownGeneratorMessages.DebugReproDirectoryDoesNotExist(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproMissingFileEntryMapping(string)"/>
    public static Exception DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(7, WellKnownGeneratorMessages.DebugReproMissingFileEntryMapping(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproUnrecognizedFileEntry(string)"/>
    public static Exception DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(8, WellKnownGeneratorMessages.DebugReproUnrecognizedFileEntry(path));
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

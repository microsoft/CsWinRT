// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// Well known exceptions for the projection generator.
/// </summary>
internal sealed class WellKnownProjectionGeneratorExceptions : IGeneratorErrorFactory
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTPROJECTIONGEN";

    /// <summary>
    /// Prevents external instantiation; this type is only used to dispatch through <see cref="IGeneratorErrorFactory"/>.
    /// </summary>
    private WellKnownProjectionGeneratorExceptions()
    {
    }

    /// <summary>
    /// Some exception was thrown when trying to read the response file.
    /// </summary>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, "Failed to read the response file to run 'cswinrtprojectiongen'.", exception);
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
    /// Diagnostics when emitting the projection .dll to disk.
    /// </summary>
    public static Exception EmitDllError(IEnumerable<Diagnostic> diagnostics)
    {
        string combinedDiagnostics = string.Join("\n", diagnostics.Select(static diagnostic => diagnostic.ToString()));

        return Exception(4, $"Failed to emit the projection dll.\n{combinedDiagnostics}");
    }

    /// <summary>
    /// Exception when emitting the projection .dll to disk.
    /// </summary>
    public static Exception EmitDllError(Exception exception)
    {
        return Exception(5, "Failed to emit the projection dll.", exception);
    }

    /// <summary>
    /// Exception when emitting the projection .dll to disk.
    /// </summary>
    public static Exception CreateCompilationError(Exception exception)
    {
        return Exception(6, "Failed to create the compilation dll.", exception);
    }

    /// <summary>
    /// The projection writer failed to start.
    /// </summary>
    public static Exception CsWinRTProcessStartError()
    {
        return Exception(7, "Failed to invoke the projection writer.");
    }

    /// <summary>
    /// The projection writer failed to start.
    /// </summary>
    public static Exception CsWinRTProcessStartError(Exception exception)
    {
        return Exception(7, "Failed to invoke the projection writer.", exception);
    }

    /// <summary>
    /// The projection writer failed during source generation.
    /// </summary>
    public static Exception CsWinRTProcessError(int exitCode, Exception exception)
    {
        return Exception(8, $"The projection writer failed during source generation (exit code {exitCode}).", exception);
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(9, $"The debug repro directory '{path}' does not exist.");
    }

    /// <summary>
    /// The debug repro contains a file entry that has no mapping.
    /// </summary>
    public static Exception DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(10, $"The debug repro file entry with path '{path}' is missing its assembly path mapping.");
    }

    /// <summary>
    /// The debug repro contains a file entry that was not recognized.
    /// </summary>
    public static Exception DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(11, $"The debug repro file entry with path '{path}' was not recognized.");
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
        return new WellKnownProjectionGeneratorException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}
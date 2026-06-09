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

    /// <inheritdoc cref="IGeneratorErrorFactory.ResponseFileReadError(Exception)"/>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, WellKnownGeneratorMessages.ResponseFileReadError("cswinrtprojectiongen"), exception);
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

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproDirectoryDoesNotExist(string)"/>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(9, WellKnownGeneratorMessages.DebugReproDirectoryDoesNotExist(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproMissingFileEntryMapping(string)"/>
    public static Exception DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(10, WellKnownGeneratorMessages.DebugReproMissingFileEntryMapping(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproUnrecognizedFileEntry(string)"/>
    public static Exception DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(11, WellKnownGeneratorMessages.DebugReproUnrecognizedFileEntry(path));
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
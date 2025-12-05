// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// Well known exceptions for the interop generator.
/// </summary>
internal static class WellKnownProjectionGeneratorExceptions
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTPROJECTIONGEN";

    /// <summary>
    /// Some exception was thrown when trying to read the response file.
    /// </summary>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, "Failed to read the response file to run 'cswinrtgen'.", exception);
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
    /// Diagnostics when emitting the impl .dll to disk.
    /// </summary>
    public static Exception EmitDllError(IEnumerable<Diagnostic> diagnostics)
    {
        string combinedDiagnostics = string.Join("\n", diagnostics.Select(static diagnostic => $"{diagnostic.Severity}: '{diagnostic.GetMessage()}'"));

        return Exception(4, $"Failed to emit the projection dll.\n{combinedDiagnostics}");
    }

    /// <summary>
    /// Exception when emitting the impl .dll to disk.
    /// </summary>
    public static Exception EmitDllError(Exception exception)
    {
        return Exception(5, "Failed to emit the projection dll.", exception);
    }

    /// <summary>
    /// Exception when emitting the impl .dll to disk.
    /// </summary>
    public static Exception CreateCompilationError(Exception exception)
    {
        return Exception(6, "Failed to create the compilation dll.", exception);
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
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// Well known exceptions for the interop generator.
/// </summary>
internal static class WellKnownProjectionExceptions
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTPROJECTIONGEN";

    /// <summary>
    /// Diagnostics when emitting the impl .dll to disk.
    /// </summary>
    public static Exception EmitDllError(IEnumerable<Diagnostic> diagnostics)
    {
        string combinedDiagnostics = string.Join("\n", diagnostics.Select(static diagnostic => $"{diagnostic.Severity}: '{diagnostic.GetMessage()}'"));

        return Exception(1, $"Failed to emit the projection dll.\n{combinedDiagnostics}");
    }

    /// <summary>
    /// Exception when emitting the impl .dll to disk.
    /// </summary>
    public static Exception EmitDllError(Exception exception)
    {
        return Exception(2, "Failed to emit the projection dll.", exception);
    }

    /// <summary>
    /// Exception when emitting the impl .dll to disk.
    /// </summary>
    public static Exception CreateCompilationError(Exception exception)
    {
        return Exception(2, "Failed to create the compilation dll.", exception);
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


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.WinMDGenerator.Errors;

/// <summary>
/// Well-known exceptions for the WinMD generator.
/// </summary>
internal static class WellKnownWinMDExceptions
{
    /// <summary>
    /// Creates an exception for a response file read error.
    /// </summary>
    public static Exception ResponseFileReadError(Exception inner)
    {
        return new InvalidOperationException("Failed to read the response file.", inner);
    }

    /// <summary>
    /// Creates an exception for a malformed response file.
    /// </summary>
    public static Exception MalformedResponseFile()
    {
        return new InvalidOperationException("The response file is malformed.");
    }

    /// <summary>
    /// Creates an exception for a response file argument parsing error.
    /// </summary>
    public static Exception ResponseFileArgumentParsingError(string propertyName, Exception? inner = null)
    {
        return new InvalidOperationException($"Failed to parse the '{propertyName}' argument from the response file.", inner);
    }

    /// <summary>
    /// Creates an exception for a WinMD generation error.
    /// </summary>
    public static Exception WinMDGenerationError(Exception inner)
    {
        return new InvalidOperationException("Failed to generate the WinMD file.", inner);
    }

    /// <summary>
    /// Creates an exception for a WinMD write error.
    /// </summary>
    public static Exception WinMDWriteError(Exception inner)
    {
        return new InvalidOperationException("Failed to write the WinMD file.", inner);
    }
}

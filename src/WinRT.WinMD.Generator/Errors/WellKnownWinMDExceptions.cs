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
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTWINMDGEN";

    /// <summary>
    /// Some exception was thrown when trying to read the response file.
    /// </summary>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, "Failed to read the response file to run 'cswinrtwinmdgen'.", exception);
    }

    /// <summary>
    /// The input response file is malformed.
    /// </summary>
    public static Exception MalformedResponseFile()
    {
        return Exception(2, "The response file is malformed and contains invalid content.");
    }

    /// <summary>
    /// Failed to parse an argument from the response file.
    /// </summary>
    public static Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(3, $"Failed to parse argument '{argumentName}' from response file.", exception);
    }

    /// <summary>
    /// Some exception was thrown when trying to load the input assembly.
    /// </summary>
    public static Exception InputAssemblyLoadError(Exception exception)
    {
        return Exception(4, "Failed to load the input assembly.", exception);
    }

    /// <summary>
    /// Failed to generate the WinMD file.
    /// </summary>
    public static Exception WinMDGenerationError(Exception exception)
    {
        return Exception(5, "Failed to generate the WinMD file.", exception);
    }

    /// <summary>
    /// Failed to write the WinMD file to disk.
    /// </summary>
    public static Exception WinMDWriteError(Exception exception)
    {
        return Exception(6, "Failed to write the WinMD file to disk.", exception);
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    private static Exception Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownWinMDException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}
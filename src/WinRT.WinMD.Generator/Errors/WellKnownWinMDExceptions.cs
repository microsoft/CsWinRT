// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.GeneratorCli.Errors;

namespace WindowsRuntime.WinMDGenerator.Errors;

/// <summary>
/// Well-known exceptions for the WinMD generator.
/// </summary>
internal sealed class WellKnownWinMDExceptions : IGeneratorErrorFactory
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTWINMDGEN";

    /// <summary>
    /// Prevents external instantiation; this type is only used to dispatch through <see cref="IGeneratorErrorFactory"/>.
    /// </summary>
    private WellKnownWinMDExceptions()
    {
    }

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
    /// Failed to probe the .NET runtime version from the input assembly.
    /// </summary>
    public static Exception InputAssemblyRuntimeVersionNotFound(string path)
    {
        return Exception(7, $"Failed to probe the .NET runtime version from the input assembly '{path}'.");
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(8, $"The debug repro directory '{path}' does not exist.");
    }

    /// <summary>
    /// The debug repro contains a file entry that has no mapping.
    /// </summary>
    public static Exception DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(9, $"The debug repro file entry with path '{path}' is missing its assembly path mapping.");
    }

    /// <summary>
    /// The debug repro contains a file entry that was not recognized.
    /// </summary>
    public static Exception DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(10, $"The debug repro file entry with path '{path}' was not recognized.");
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    private static Exception Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownWinMDException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}
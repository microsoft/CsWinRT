// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.WinMDGenerator.Errors;

/// <summary>
/// Well-known exceptions for the WinMD generator.
/// </summary>
internal sealed class WellKnownWinMDExceptions : IGeneratorErrorFactory, IWindowsMetadataErrorFactory
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

    /// <inheritdoc cref="IGeneratorErrorFactory.ResponseFileReadError(Exception)"/>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, WellKnownGeneratorMessages.ResponseFileReadError, exception);
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.MalformedResponseFile"/>
    public static Exception MalformedResponseFile()
    {
        return Exception(2, WellKnownGeneratorMessages.MalformedResponseFile);
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.ResponseFileArgumentParsingError(string, Exception?)"/>
    public static Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(3, WellKnownGeneratorMessages.ResponseFileArgumentParsingError(argumentName), exception);
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

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproDirectoryDoesNotExist(string)"/>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(8, WellKnownGeneratorMessages.DebugReproDirectoryDoesNotExist(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproMissingFileEntryMapping(string)"/>
    public static Exception DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(9, WellKnownGeneratorMessages.DebugReproMissingFileEntryMapping(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproUnrecognizedFileEntry(string)"/>
    public static Exception DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(10, WellKnownGeneratorMessages.DebugReproUnrecognizedFileEntry(path));
    }

    /// <inheritdoc cref="IWindowsMetadataErrorFactory.WindowsSdkNotFound"/>
    public static Exception WindowsSdkNotFound()
    {
        return Exception(11, WellKnownGeneratorMessages.WindowsSdkNotFound);
    }

    /// <inheritdoc cref="IWindowsMetadataErrorFactory.CannotReadWindowsSdkXml(string)"/>
    public static Exception CannotReadWindowsSdkXml(string path)
    {
        return Exception(12, WellKnownGeneratorMessages.CannotReadWindowsSdkXml(path));
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    private static Exception Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownWinMDException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}
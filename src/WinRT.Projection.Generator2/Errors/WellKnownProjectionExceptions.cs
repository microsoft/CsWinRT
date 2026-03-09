// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// Well known exceptions for the projection generator.
/// </summary>
internal static class WellKnownProjectionExceptions
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTPROJGEN";

    /// <summary>
    /// The input file path is not valid.
    /// </summary>
    /// <param name="path">The invalid file path.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException InvalidInputFilePath(string path)
    {
        return Exception(1, $"The input file path '{path}' is not valid.");
    }

    /// <summary>
    /// The input file was not found.
    /// </summary>
    /// <param name="path">The path to the missing file.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException InputFileNotFound(string path)
    {
        return Exception(2, $"The input file '{path}' was not found.");
    }

    /// <summary>
    /// The input file is not a .winmd file.
    /// </summary>
    /// <param name="path">The path to the non-.winmd file.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException InputFileNotWinMd(string path)
    {
        return Exception(3, $"The input file '{path}' is not a .winmd file.");
    }

    /// <summary>
    /// Failed to create the output directory.
    /// </summary>
    /// <param name="path">The path to the output directory.</param>
    /// <param name="exception">The inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException OutputDirectoryCreationFailed(string path, Exception exception)
    {
        return Exception(4, $"Failed to create output directory '{path}'.", exception);
    }

    /// <summary>
    /// Failed to load a module from the specified path.
    /// </summary>
    /// <param name="path">The path to the module that failed to load.</param>
    /// <param name="exception">The inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException ModuleLoadFailed(string path, Exception exception)
    {
        return Exception(5, $"Failed to load module from '{path}'.", exception);
    }

    /// <summary>
    /// Failed to process a type.
    /// </summary>
    /// <param name="namespaceName">The namespace of the type.</param>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="exception">The inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException TypeProcessingFailed(string namespaceName, string typeName, Exception exception)
    {
        return Exception(6, $"Failed to process type '{namespaceName}.{typeName}'.", exception);
    }

    /// <summary>
    /// Failed to write a file.
    /// </summary>
    /// <param name="path">The path to the file that failed to write.</param>
    /// <param name="exception">The inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException FileWriteFailed(string path, Exception exception)
    {
        return Exception(7, $"Failed to write file '{path}'.", exception);
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    /// <param name="path">The path to the missing debug repro directory.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(8, $"The debug repro directory '{path}' does not exist.");
    }

    /// <summary>
    /// Failed to save the debug repro.
    /// </summary>
    /// <param name="path">The path to the debug repro that failed to save.</param>
    /// <param name="exception">The inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException DebugReproSaveFailed(string path, Exception exception)
    {
        return Exception(9, $"Failed to save debug repro to '{path}'.", exception);
    }

    /// <summary>
    /// Failed to unpack the debug repro.
    /// </summary>
    /// <param name="path">The path to the debug repro that failed to unpack.</param>
    /// <param name="exception">The inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException DebugReproUnpackFailed(string path, Exception exception)
    {
        return Exception(10, $"Failed to unpack debug repro from '{path}'.", exception);
    }

    /// <summary>
    /// A file in the debug repro archive does not have a corresponding path mapping entry.
    /// </summary>
    /// <param name="path">The path of the file without a mapping entry.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(11, $"A file in the debug repro archive does not have a corresponding path mapping entry: '{path}'.");
    }

    /// <summary>
    /// An unrecognized file entry was found in the debug repro archive.
    /// </summary>
    /// <param name="path">The path of the unrecognized file entry.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(12, $"An unrecognized file entry was found in the debug repro archive: '{path}'.");
    }

    /// <summary>
    /// Failed to read the response file.
    /// </summary>
    /// <param name="exception">The inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException ResponseFileReadError(Exception exception)
    {
        return Exception(13, "Failed to read the response file.", exception);
    }

    /// <summary>
    /// The response file is malformed.
    /// </summary>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException MalformedResponseFile()
    {
        return Exception(14, "The response file is malformed.");
    }

    /// <summary>
    /// Failed to parse a response file argument.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed to parse.</param>
    /// <param name="exception">The optional inner exception.</param>
    /// <returns>A <see cref="WellKnownProjectionException"/> for this error.</returns>
    public static WellKnownProjectionException ResponseFileArgumentParsingError(string propertyName, Exception? exception = null)
    {
        return Exception(15, $"Failed to parse the response file argument for '{propertyName}'.", exception);
    }

    /// <summary>
    /// Creates a new <see cref="WellKnownProjectionException"/> instance with the formatted error message.
    /// </summary>
    /// <param name="id">The error id.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The optional inner exception.</param>
    /// <returns>The resulting <see cref="WellKnownProjectionException"/>.</returns>
    private static WellKnownProjectionException Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownProjectionException($"{ErrorPrefix}{id:0000}: {message}", innerException);
    }
}

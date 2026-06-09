// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// Shared message templates for the well-known logical errors defined by <see cref="IGeneratorErrorFactory"/>.
/// </summary>
/// <remarks>
/// Each per-tool <c>WellKnown*Exceptions</c> factory uses these helpers to format its own
/// instance of every <see cref="IGeneratorErrorFactory"/> method, ensuring that the message
/// text stays identical across all generators while the per-tool error ID prefix (e.g.
/// <c>CSWINRTIMPLGEN</c>) and concrete exception type are still chosen per-tool.
/// </remarks>
internal static class WellKnownGeneratorMessages
{
    /// <summary>
    /// Builds the message for <see cref="IGeneratorErrorFactory.ResponseFileReadError"/>.
    /// </summary>
    /// <param name="toolName">The CLI tool name embedded in the message (e.g. <c>"cswinrtimplgen"</c>).</param>
    /// <returns>The formatted error message.</returns>
    public static string ResponseFileReadError(string toolName)
    {
        return $"Failed to read the response file to run '{toolName}'.";
    }

    /// <summary>
    /// Builds the message for <see cref="IGeneratorErrorFactory.ResponseFileArgumentParsingError"/>.
    /// </summary>
    /// <param name="argumentName">The name of the response-file argument that failed to parse.</param>
    /// <returns>The formatted error message.</returns>
    public static string ResponseFileArgumentParsingError(string argumentName)
    {
        return $"Failed to parse argument '{argumentName}' from response file.";
    }

    /// <summary>
    /// The message for <see cref="IGeneratorErrorFactory.MalformedResponseFile"/>.
    /// </summary>
    public const string MalformedResponseFile = "The response file is malformed and contains invalid content.";

    /// <summary>
    /// Builds the message for <see cref="IGeneratorErrorFactory.DebugReproDirectoryDoesNotExist"/>.
    /// </summary>
    /// <param name="path">The directory path that does not exist.</param>
    /// <returns>The formatted error message.</returns>
    public static string DebugReproDirectoryDoesNotExist(string path)
    {
        return $"The debug repro directory '{path}' does not exist.";
    }

    /// <summary>
    /// Builds the message for <see cref="IGeneratorErrorFactory.DebugReproMissingFileEntryMapping"/>.
    /// </summary>
    /// <param name="path">The debug-repro file entry path that has no mapping.</param>
    /// <returns>The formatted error message.</returns>
    public static string DebugReproMissingFileEntryMapping(string path)
    {
        return $"The debug repro file entry with path '{path}' is missing its assembly path mapping.";
    }

    /// <summary>
    /// Builds the message for <see cref="IGeneratorErrorFactory.DebugReproUnrecognizedFileEntry"/>.
    /// </summary>
    /// <param name="path">The debug-repro file entry path that was not recognized.</param>
    /// <returns>The formatted error message.</returns>
    public static string DebugReproUnrecognizedFileEntry(string path)
    {
        return $"The debug repro file entry with path '{path}' was not recognized.";
    }
}

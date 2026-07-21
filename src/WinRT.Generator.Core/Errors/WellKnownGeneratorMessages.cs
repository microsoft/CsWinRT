// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// Shared message templates for the well-known logical errors defined by <see cref="IGeneratorErrorFactory"/>
/// and <see cref="IWindowsMetadataErrorFactory"/>.
/// </summary>
/// <remarks>
/// Each per-tool <c>WellKnown*Exceptions</c> factory uses these helpers to format its own
/// instance of every shared error factory method, ensuring that the message
/// text stays identical across all generators while the per-tool error ID prefix (e.g.
/// <c>CSWINRTIMPLGEN</c>) and concrete exception type are still chosen per-tool.
/// </remarks>
internal static class WellKnownGeneratorMessages
{
    /// <see cref="IGeneratorErrorFactory.ResponseFileReadError"/>
    public const string ResponseFileReadError = "Failed to read the response file (e.g. it may be missing or not accessible).";

    /// <see cref="IGeneratorErrorFactory.ResponseFileArgumentParsingError"/>
    /// <param name="argumentName">The name of the response-file argument that failed to parse.</param>
    public static string ResponseFileArgumentParsingError(string argumentName)
    {
        return $"Failed to parse argument '{argumentName}' from response file.";
    }

    /// <see cref="IGeneratorErrorFactory.MalformedResponseFile"/>
    public const string MalformedResponseFile = "The response file is malformed and contains invalid content.";

    /// <see cref="IGeneratorErrorFactory.DebugReproDirectoryDoesNotExist"/>
    /// <param name="path">The directory path that does not exist.</param>
    public static string DebugReproDirectoryDoesNotExist(string path)
    {
        return $"The debug repro directory '{path}' does not exist.";
    }

    /// <see cref="IGeneratorErrorFactory.DebugReproMissingFileEntryMapping"/>
    /// <param name="path">The debug-repro file entry path that has no mapping.</param>
    public static string DebugReproMissingFileEntryMapping(string path)
    {
        return $"The debug repro file entry with path '{path}' is missing its assembly path mapping.";
    }

    /// <see cref="IGeneratorErrorFactory.DebugReproUnrecognizedFileEntry"/>
    /// <param name="path">The debug-repro file entry path that was not recognized.</param>
    public static string DebugReproUnrecognizedFileEntry(string path)
    {
        return $"The debug repro file entry with path '{path}' was not recognized.";
    }

    /// <see cref="IWindowsMetadataErrorFactory.WindowsSdkNotFound"/>
    public const string WindowsSdkNotFound = "Could not find the Windows SDK in the registry.";

    /// <see cref="IWindowsMetadataErrorFactory.CannotReadWindowsSdkXml"/>
    /// <param name="path">The Windows SDK XML path that could not be read.</param>
    public static string CannotReadWindowsSdkXml(string path)
    {
        return $"Could not read the Windows SDK's XML at '{path}'.";
    }
}

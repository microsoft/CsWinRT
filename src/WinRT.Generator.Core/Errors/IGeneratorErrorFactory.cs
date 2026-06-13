// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// Routes shared logical errors through the per-tool well-known exception factory.
/// </summary>
/// <remarks>
/// Shared infrastructure (response-file parsing, debug-repro packing, etc.) is generic over an
/// implementation of this interface to preserve per-tool exception identity exactly: each factory
/// continues to assign its own numeric error IDs, format its own messages (including embedded tool names),
/// and construct its own concrete <see cref="WellKnownGeneratorException"/> subtype.
/// </remarks>
internal interface IGeneratorErrorFactory
{
    /// <summary>Some exception was thrown when trying to read the response file.</summary>
    static abstract Exception ResponseFileReadError(Exception exception);

    /// <summary>Failed to parse an argument from the response file.</summary>
    static abstract Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception);

    /// <summary>The input response file is malformed.</summary>
    static abstract Exception MalformedResponseFile();

    /// <summary>The debug repro directory does not exist.</summary>
    static abstract Exception DebugReproDirectoryDoesNotExist(string path);

    /// <summary>The debug repro contains a file entry that has no mapping.</summary>
    static abstract Exception DebugReproMissingFileEntryMapping(string path);

    /// <summary>The debug repro contains a file entry that was not recognized.</summary>
    static abstract Exception DebugReproUnrecognizedFileEntry(string path);
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ImplGenerator.Errors;

/// <summary>
/// Well known exceptions for the impl generator.
/// </summary>
internal sealed class WellKnownImplExceptions : IGeneratorErrorFactory
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTIMPLGEN";

    /// <summary>
    /// Prevents external instantiation; this type is only used to dispatch through <see cref="IGeneratorErrorFactory"/>.
    /// </summary>
    private WellKnownImplExceptions()
    {
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.ResponseFileReadError(Exception)"/>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(1, WellKnownGeneratorMessages.ResponseFileReadError("cswinrtimplgen"), exception);
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.ResponseFileArgumentParsingError(string, Exception?)"/>
    public static Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(2, WellKnownGeneratorMessages.ResponseFileArgumentParsingError(argumentName), exception);
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.MalformedResponseFile"/>
    public static Exception MalformedResponseFile()
    {
        return Exception(3, WellKnownGeneratorMessages.MalformedResponseFile);
    }

    /// <summary>
    /// Some exception was thrown when trying to read the output assembly file.
    /// </summary>
    public static Exception OutputAssemblyFileReadError(string filename, Exception? exception = null)
    {
        return Exception(4, $"Failed to read the output assembly file '{filename}'.", exception);
    }

    /// <summary>
    /// Failed to define the impl assembly.
    /// </summary>
    public static Exception DefineImplAssemblyError(Exception exception)
    {
        return Exception(5, "Failed to define the impl module and assembly.", exception);
    }

    /// <summary>
    /// Exception when emitting the impl .dll to disk.
    /// </summary>
    public static Exception EmitDllError(Exception exception)
    {
        return Exception(6, "Failed to emit the impl .dll to disk.", exception);
    }

    /// <summary>
    /// Failed to emit the assembly attributes.
    /// </summary>
    public static Exception EmitAssemblyAttributes(Exception exception)
    {
        return Exception(7, "Failed to emit the assembly attributes for impl module.", exception);
    }

    /// <summary>
    /// Failed to emit the type forwards.
    /// </summary>
    public static Exception EmitTypeForwards(Exception exception)
    {
        return Exception(8, "Failed to emit the '[TypeForwardedTo]' attributes for the impl module.", exception);
    }

    /// <summary>
    /// Exception when trying to read an .snk file.
    /// </summary>
    public static Exception SnkLoadError(Exception exception)
    {
        return Exception(9, "Failed to read the .snk file to sign the impl .dll on disk.", exception);
    }

    /// <summary>
    /// Exception when signing the impl .dll to disk.
    /// </summary>
    public static Exception SignDllError(Exception exception)
    {
        return Exception(10, "Failed to sign the impl .dll on disk.", exception);
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproDirectoryDoesNotExist(string)"/>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(11, WellKnownGeneratorMessages.DebugReproDirectoryDoesNotExist(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproMissingFileEntryMapping(string)"/>
    public static Exception DebugReproMissingFileEntryMapping(string path)
    {
        return Exception(12, WellKnownGeneratorMessages.DebugReproMissingFileEntryMapping(path));
    }

    /// <inheritdoc cref="IGeneratorErrorFactory.DebugReproUnrecognizedFileEntry(string)"/>
    public static Exception DebugReproUnrecognizedFileEntry(string path)
    {
        return Exception(13, WellKnownGeneratorMessages.DebugReproUnrecognizedFileEntry(path));
    }

    /// <summary>
    /// Failed to probe the .NET runtime version from the output assembly.
    /// </summary>
    public static Exception OutputAssemblyRuntimeVersionNotFound(string path)
    {
        return Exception(14, $"Failed to probe the .NET runtime version from the output assembly '{path}'.");
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
        return new WellKnownImplException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}
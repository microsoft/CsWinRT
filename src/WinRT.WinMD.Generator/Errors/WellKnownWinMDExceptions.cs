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
    /// A method has a <c>ref</c> or <c>in</c> array parameter, which is not a valid Windows Runtime array convention.
    /// </summary>
    public static Exception ByReferenceArrayParameterNotSupported(string declaringTypeName, string methodName, string parameterName)
    {
        return Exception(13,
            $"Method '{declaringTypeName}.{methodName}' has by-reference array parameter '{parameterName}' passed by 'ref' or 'in'. " +
            $"Windows Runtime arrays use one of three conventions: 'ReadOnlySpan<T>' for a read-only input array (PassArray), " +
            $"'Span<T>' for a caller-allocated array (FillArray), or 'out T[]' for a callee-allocated array (ReceiveArray).");
    }

    /// <summary>
    /// A method has a by-reference span parameter (e.g. <c>out Span&lt;T&gt;</c>), which has no Windows Runtime representation.
    /// </summary>
    public static Exception ByReferenceSpanParameterNotSupported(string declaringTypeName, string methodName, string parameterName)
    {
        return Exception(14,
            $"Method '{declaringTypeName}.{methodName}' has by-reference span parameter '{parameterName}'." +
            $"Windows Runtime spans are passed by value: use 'ReadOnlySpan<T>' (PassArray) or 'Span<T>' " +
            $"(FillArray) by value, or 'out T[]' for a callee-allocated array (ReceiveArray).");
    }

    /// <summary>
    /// An unsealed (composable) authored class implements an interface that cannot take part in COM aggregation.
    /// </summary>
    public static Exception ComposableClassInterfaceNotSupported(string className, string interfaceNames)
    {
        return Exception(15,
            $"Class '{className}' is unsealed and has at least one public constructor, so it is projected as a composable Windows Runtime " +
            $"class that native code can derive from, but it implements interface(s) '{interfaceNames}' that cannot take part in COM " +
            $"aggregation. Only Windows Runtime interfaces authored in the same component can be exposed by a composable class: " +
            $"custom-mapped interfaces (such as 'IDisposable', 'IList<T>' or 'INotifyPropertyChanged'), generic instantiations, and " +
            $"interfaces from the Windows SDK or from another component all get their COM vtables from shared infrastructure, so no " +
            $"per-aggregate copy delegating to the controlling outer object can be made for them. Either mark '{className}' as 'sealed' " +
            $"(making it a normal activatable runtime class), make all of its constructors non-public (making it a non-composable base " +
            $"type), or remove the offending interface implementations from it.");
    }

    /// <summary>
    /// A composition factory method of an unsealed (composable) authored class takes an unsupported parameter.
    /// </summary>
    public static Exception ComposableClassConstructorParameterNotSupported(string className, string parameterTypeName)
    {
        return Exception(16,
            $"Class '{className}' is unsealed and has at least one public constructor, so its constructors are projected as composition " +
            $"factory methods, but one of them takes a parameter of type '{parameterTypeName}'. Array and generic parameters are not " +
            $"supported on composition factory methods. Either mark '{className}' as 'sealed' (making it a normal activatable runtime " +
            $"class), make all of its constructors non-public (making it a non-composable base type), or change the constructor to not " +
            $"take array or generic parameters.");
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    private static Exception Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownWinMDException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}
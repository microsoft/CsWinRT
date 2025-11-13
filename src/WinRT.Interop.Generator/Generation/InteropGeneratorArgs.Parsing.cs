// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using WindowsRuntime.InteropGenerator.Attributes;
using WindowsRuntime.InteropGenerator.Errors;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGeneratorArgs"/>
internal partial class InteropGeneratorArgs
{
    /// <summary>
    /// Parses an <see cref="InteropGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="InteropGeneratorArgs"/> instance.</returns>
    public static InteropGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        // If the path is a response file, it will start with the '@' character.
        // This matches the default escaping 'ToolTask' uses for response files.
        if (path is ['@', .. string escapedPath])
        {
            path = escapedPath;
        }

        string[] lines;

        // Read all lines in the response file (each line contains a single command line argument)
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e)
        {
            throw WellKnownInteropExceptions.ResponseFileReadError(e);
        }

        return ParseFromResponseFile(lines, token);
    }

    /// <summary>
    /// Parses an <see cref="InteropGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="stream">The stream to the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="InteropGeneratorArgs"/> instance.</returns>
    public static InteropGeneratorArgs ParseFromResponseFile(Stream stream, CancellationToken token)
    {
        string[] responseArgs = File.ReadAllLines(stream);

        return ParseFromResponseFile(responseArgs, token);
    }

    /// <summary>
    /// Parses an <see cref="InteropGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="lines">The lines read from the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="InteropGeneratorArgs"/> instance.</returns>
    private static InteropGeneratorArgs ParseFromResponseFile(string[] lines, CancellationToken token)
    {
        Dictionary<string, string> argsMap = [];

        // Build a map with all the commands and their values
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Each line has the command line argument name followed by a space, and then the
            // argument value. If there are no spaces on any given line, the file is malformed.
            int indexOfSpace = trimmedLine.IndexOf(' ');

            if (indexOfSpace == -1)
            {
                throw WellKnownInteropExceptions.MalformedResponseFile();
            }

            // Now we can parse the actual command line argument name and value
            string argumentName = trimmedLine.AsSpan()[..indexOfSpace].ToString();
            string argumentValue = trimmedLine.AsSpan()[(indexOfSpace + 1)..].TrimEnd().ToString();

            // We should never have duplicate commands
            if (!argsMap.TryAdd(argumentName, argumentValue))
            {
                throw WellKnownInteropExceptions.MalformedResponseFile();
            }
        }

        // Parse all commands to create the managed arguments to use
        return new()
        {
            ReferenceAssemblyPaths = GetStringArrayArgument(argsMap, nameof(ReferenceAssemblyPaths)),
            OutputAssemblyPath = GetStringArgument(argsMap, nameof(OutputAssemblyPath)),
            GeneratedAssemblyDirectory = GetStringArgument(argsMap, nameof(GeneratedAssemblyDirectory)),
            UseWindowsUIXamlProjections = GetBooleanArgument(argsMap, nameof(UseWindowsUIXamlProjections)),
            ValidateWinRTRuntimeAssemblyVersion = GetBooleanArgument(argsMap, nameof(ValidateWinRTRuntimeAssemblyVersion)),
            ValidateWinRTRuntimeDllVersion2References = GetBooleanArgument(argsMap, nameof(ValidateWinRTRuntimeDllVersion2References)),
            EnableIncrementalGeneration = GetBooleanArgument(argsMap, nameof(EnableIncrementalGeneration)),
            TreatWarningsAsErrors = GetBooleanArgument(argsMap, nameof(TreatWarningsAsErrors)),
            MaxDegreesOfParallelism = GetInt32Argument(argsMap, nameof(MaxDegreesOfParallelism)),
            DebugReproDirectory = GetNullableStringArgument(argsMap, nameof(DebugReproDirectory)),
            Token = token
        };
    }

    /// <summary>
    /// Gets the command line argument name for a property.
    /// </summary>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The command line argument name for <paramref name="propertyName"/>.</returns>
    public static string GetCommandLineArgumentName(string propertyName)
    {
        try
        {
            return typeof(InteropGeneratorArgs).GetProperty(propertyName)!.GetCustomAttribute<CommandLineArgumentNameAttribute>()!.Name;
        }
        catch (Exception e)
        {
            throw WellKnownInteropExceptions.ResponseFileArgumentParsingError(propertyName, e);
        }
    }

    /// <summary>
    /// Parses a <see cref="string"/> array argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    private static string[] GetStringArrayArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return argumentValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        throw WellKnownInteropExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a <see cref="string"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    private static string GetStringArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return argumentValue;
        }

        throw WellKnownInteropExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a nullable (optional) <see cref="string"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    private static string? GetNullableStringArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return argumentValue;
        }

        return null;
    }

    /// <summary>
    /// Parses an <see cref="int"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    private static int GetInt32Argument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            if (int.TryParse(argumentValue, out int parsedValue))
            {
                return parsedValue;
            }
        }

        throw WellKnownInteropExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a <see cref="bool"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    private static bool GetBooleanArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            if (bool.TryParse(argumentValue, out bool parsedValue))
            {
                return parsedValue;
            }
        }

        throw WellKnownInteropExceptions.ResponseFileArgumentParsingError(propertyName);
    }
}

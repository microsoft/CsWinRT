// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WindowsRuntime.WinMDGenerator.Attributes;
using WindowsRuntime.WinMDGenerator.Errors;

#pragma warning disable IDE0046

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDGeneratorArgs"/>
internal partial class WinMDGeneratorArgs
{
    /// <summary>
    /// Parses a <see cref="WinMDGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <remarks>
    /// The path may be prefixed with <c>@</c> (matching the default escaping <c>ToolTask</c> uses
    /// for response files), which is stripped before reading the file.
    /// </remarks>
    /// <param name="path">The path to the response file (optionally prefixed with <c>@</c>).</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="WinMDGeneratorArgs"/> instance.</returns>
    public static WinMDGeneratorArgs ParseFromResponseFile(string path, System.Threading.CancellationToken token)
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
            throw WellKnownWinMDExceptions.ResponseFileReadError(e);
        }

        return ParseFromResponseFile(lines, token);
    }

    /// <summary>
    /// Parses a <see cref="WinMDGeneratorArgs"/> instance from the lines of a response file.
    /// </summary>
    /// <remarks>
    /// Each line in the response file contains a single command-line argument in the form
    /// <c>--argument-name value</c>. Lines are parsed into a key-value map, and each property
    /// of <see cref="WinMDGeneratorArgs"/> is populated using its <see cref="Attributes.CommandLineArgumentNameAttribute"/>.
    /// Duplicate argument names cause a malformed response file error.
    /// </remarks>
    /// <param name="lines">The lines read from the response file.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="WinMDGeneratorArgs"/> instance.</returns>
    private static WinMDGeneratorArgs ParseFromResponseFile(string[] lines, System.Threading.CancellationToken token)
    {
        Dictionary<string, string> argsMap = [];

        // Build a map with all the commands and their values
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            // Each line has the command line argument name followed by a space, and then the
            // argument value. If there are no spaces on any given line, the file is malformed.
            int indexOfSpace = trimmedLine.IndexOf(' ');

            if (indexOfSpace == -1)
            {
                throw WellKnownWinMDExceptions.MalformedResponseFile();
            }

            // Now we can parse the actual command line argument name and value
            string argumentName = trimmedLine.AsSpan()[..indexOfSpace].ToString();
            string argumentValue = trimmedLine.AsSpan()[(indexOfSpace + 1)..].TrimEnd().ToString();

            // We should never have duplicate commands
            if (!argsMap.TryAdd(argumentName, argumentValue))
            {
                throw WellKnownWinMDExceptions.MalformedResponseFile();
            }
        }

        // Parse all commands to create the managed arguments to use
        return new()
        {
            InputAssemblyPath = GetStringArgument(argsMap, nameof(InputAssemblyPath)),
            ReferenceAssemblyPaths = GetStringArrayArgument(argsMap, nameof(ReferenceAssemblyPaths)),
            OutputWinmdPath = GetStringArgument(argsMap, nameof(OutputWinmdPath)),
            AssemblyVersion = GetStringArgument(argsMap, nameof(AssemblyVersion)),
            UseWindowsUIXamlProjections = GetBooleanArgument(argsMap, nameof(UseWindowsUIXamlProjections)),
            Token = token
        };
    }

    /// <summary>
    /// Gets the command-line argument name for a property by reading its
    /// <see cref="Attributes.CommandLineArgumentNameAttribute"/>.
    /// </summary>
    /// <param name="propertyName">The target property name on <see cref="WinMDGeneratorArgs"/>.</param>
    /// <returns>The command-line argument name (e.g., <c>"--input-assembly-path"</c>).</returns>
    public static string GetCommandLineArgumentName(string propertyName)
    {
        try
        {
            return typeof(WinMDGeneratorArgs).GetProperty(propertyName)!.GetCustomAttribute<CommandLineArgumentNameAttribute>()!.Name;
        }
        catch (Exception e)
        {
            throw WellKnownWinMDExceptions.ResponseFileArgumentParsingError(propertyName, e);
        }
    }

    /// <summary>
    /// Parses a comma-separated <see cref="string"/> array argument from the arguments map.
    /// </summary>
    /// <param name="argsMap">The parsed arguments map.</param>
    /// <param name="propertyName">The property name to look up.</param>
    /// <returns>An array of trimmed, non-empty string values.</returns>
    private static string[] GetStringArrayArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return argumentValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        throw WellKnownWinMDExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a <see cref="string"/> argument from the arguments map.
    /// </summary>
    /// <param name="argsMap">The parsed arguments map.</param>
    /// <param name="propertyName">The property name to look up.</param>
    /// <returns>The string value of the argument.</returns>
    private static string GetStringArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return argumentValue;
        }

        throw WellKnownWinMDExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a <see cref="bool"/> argument from the arguments map.
    /// </summary>
    /// <param name="argsMap">The parsed arguments map.</param>
    /// <param name="propertyName">The property name to look up.</param>
    /// <returns>The parsed boolean value.</returns>
    private static bool GetBooleanArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            if (bool.TryParse(argumentValue, out bool parsedValue))
            {
                return parsedValue;
            }
        }

        throw WellKnownWinMDExceptions.ResponseFileArgumentParsingError(propertyName);
    }
}
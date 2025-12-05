// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using WindowsRuntime.ProjectionGenerator.Attributes;
using WindowsRuntime.ProjectionGenerator.Errors;

#pragma warning disable IDE0046

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGeneratorArgs"/>
internal partial class ProjectionGeneratorArgs
{
    /// <summary>
    /// Parses an <see cref="ProjectionGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="ProjectionGeneratorArgs"/> instance.</returns>
    public static ProjectionGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        // If the path is a response file, it will start with the '@' character.
        // This matches the default escaping 'ToolTask' uses for response files.
        if (path is ['@', .. string escapedPath])
        {
            path = escapedPath;
        }

        string[] responseArgs;

        // Read all lines in the response file (each line contains a single command line argument)
        try
        {
            responseArgs = File.ReadAllLines(path);
        }
        catch (Exception e)
        {
            throw WellKnownProjectionGeneratorExceptions.ResponseFileReadError(e);
        }

        Dictionary<string, string> argsMap = [];

        // Build a map with all the commands and their values
        foreach (string line in responseArgs)
        {
            string trimmedLine = line.Trim();

            // Each line has the command line argument name followed by a space, and then the
            // argument value. If there are no spaces on any given line, the file is malformed.
            int indexOfSpace = trimmedLine.IndexOf(' ');

            if (indexOfSpace == -1)
            {
                throw WellKnownProjectionGeneratorExceptions.MalformedResponseFile();
            }

            // Now we can parse the actual command line argument name and value
            string argumentName = trimmedLine.AsSpan()[..indexOfSpace].ToString();
            string argumentValue = trimmedLine.AsSpan()[(indexOfSpace + 1)..].TrimEnd().ToString();

            // We should never have duplicate commands
            if (!argsMap.TryAdd(argumentName, argumentValue))
            {
                throw WellKnownProjectionGeneratorExceptions.MalformedResponseFile();
            }
        }

        // Parse all commands to create the managed arguments to use
        return new()
        {
            ReferenceAssemblyPaths = GetStringArrayArgument(argsMap, nameof(ReferenceAssemblyPaths)),
            GeneratedAssemblyDirectory = GetStringArgument(argsMap, nameof(GeneratedAssemblyDirectory)),
            WinMDPaths = GetStringArrayArgument(argsMap, nameof(WinMDPaths)),
            TargetFramework = GetStringArgument(argsMap, nameof(TargetFramework)),
            WindowsMetadata = GetStringArgument(argsMap, nameof(WindowsMetadata)),
            CsWinRTExePath = GetStringArgument(argsMap, nameof(CsWinRTExePath)),
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
            return typeof(ProjectionGeneratorArgs).GetProperty(propertyName)!.GetCustomAttribute<CommandLineArgumentNameAttribute>()!.Name;
        }
        catch (Exception e)
        {
            throw WellKnownProjectionGeneratorExceptions.ResponseFileArgumentParsingError(propertyName, e);
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

        throw WellKnownProjectionGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
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

        throw WellKnownProjectionGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
    }
}

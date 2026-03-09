// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WindowsRuntime.ProjectionGenerator.Attributes;
using WindowsRuntime.ProjectionGenerator.Errors;

#pragma warning disable IDE0046

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGeneratorArgs"/>
internal partial class ProjectionGeneratorArgs
{
    /// <summary>
    /// Parses a <see cref="ProjectionGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <returns>The resulting <see cref="ProjectionGeneratorArgs"/> instance.</returns>
    public static ProjectionGeneratorArgs ParseFromResponseFile(string path)
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
            throw WellKnownProjectionExceptions.ResponseFileReadError(e);
        }

        return ParseFromLines(lines);
    }

    /// <summary>
    /// Parses a <see cref="ProjectionGeneratorArgs"/> instance from command line arguments.
    /// </summary>
    /// <param name="args">The command line arguments to parse.</param>
    /// <returns>The resulting <see cref="ProjectionGeneratorArgs"/> instance.</returns>
    public static ProjectionGeneratorArgs ParseFromCommandLine(string[] args)
    {
        Dictionary<string, string?> argsMap = [];

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw WellKnownProjectionExceptions.MalformedResponseFile();
            }

            // Check if the next argument is a value (not another flag) or if this is the last argument
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                if (!argsMap.TryAdd(argument, args[i + 1]))
                {
                    throw WellKnownProjectionExceptions.MalformedResponseFile();
                }

                i++;
            }
            else
            {
                // Boolean flag with no value
                if (!argsMap.TryAdd(argument, null))
                {
                    throw WellKnownProjectionExceptions.MalformedResponseFile();
                }
            }
        }

        return BuildArgs(argsMap);
    }

    /// <summary>
    /// Parses a <see cref="ProjectionGeneratorArgs"/> instance from lines read from a response file.
    /// </summary>
    /// <param name="lines">The lines read from the response file.</param>
    /// <returns>The resulting <see cref="ProjectionGeneratorArgs"/> instance.</returns>
    private static ProjectionGeneratorArgs ParseFromLines(string[] lines)
    {
        Dictionary<string, string?> argsMap = [];

        // Build a map with all the commands and their values
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Skip empty lines
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            int indexOfSpace = trimmedLine.IndexOf(' ');

            if (indexOfSpace == -1)
            {
                // Boolean flags have no value, just the argument name on the line
                if (!argsMap.TryAdd(trimmedLine, null))
                {
                    throw WellKnownProjectionExceptions.MalformedResponseFile();
                }
            }
            else
            {
                // Parse the command line argument name and value
                string argumentName = trimmedLine.AsSpan()[..indexOfSpace].ToString();
                string argumentValue = trimmedLine.AsSpan()[(indexOfSpace + 1)..].TrimEnd().ToString();

                // We should never have duplicate commands
                if (!argsMap.TryAdd(argumentName, argumentValue))
                {
                    throw WellKnownProjectionExceptions.MalformedResponseFile();
                }
            }
        }

        return BuildArgs(argsMap);
    }

    /// <summary>
    /// Builds a <see cref="ProjectionGeneratorArgs"/> instance from the parsed argument map.
    /// </summary>
    /// <param name="argsMap">The map of argument names to their values.</param>
    /// <returns>The resulting <see cref="ProjectionGeneratorArgs"/> instance.</returns>
    private static ProjectionGeneratorArgs BuildArgs(Dictionary<string, string?> argsMap)
    {
        return new()
        {
            InputFilePaths = GetStringArrayArgument(argsMap, nameof(InputFilePaths)),
            OutputDirectory = GetStringArgument(argsMap, nameof(OutputDirectory)),
            IncludeNamespaces = GetNullableStringArrayArgument(argsMap, nameof(IncludeNamespaces)),
            ExcludeNamespaces = GetNullableStringArrayArgument(argsMap, nameof(ExcludeNamespaces)),
            AdditionExcludeNamespaces = GetNullableStringArrayArgument(argsMap, nameof(AdditionExcludeNamespaces)),
            IsComponent = GetBooleanFlagArgument(argsMap, nameof(IsComponent)),
            IsVerbose = GetBooleanFlagArgument(argsMap, nameof(IsVerbose)),
            IsInternal = GetBooleanFlagArgument(argsMap, nameof(IsInternal)),
            IsEmbedded = GetBooleanFlagArgument(argsMap, nameof(IsEmbedded)),
            PublicEnums = GetBooleanFlagArgument(argsMap, nameof(PublicEnums)),
            PublicExclusiveTo = GetBooleanFlagArgument(argsMap, nameof(PublicExclusiveTo)),
            IdicExclusiveTo = GetBooleanFlagArgument(argsMap, nameof(IdicExclusiveTo)),
            PartialFactory = GetBooleanFlagArgument(argsMap, nameof(PartialFactory)),
            IsReferenceProjection = GetBooleanFlagArgument(argsMap, nameof(IsReferenceProjection)),
            DebugReproDirectory = GetNullableStringArgument(argsMap, nameof(DebugReproDirectory))
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
            throw WellKnownProjectionExceptions.ResponseFileArgumentParsingError(propertyName, e);
        }
    }

    /// <summary>
    /// Parses a <see cref="string"/> array argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    private static string[] GetStringArrayArgument(Dictionary<string, string?> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue) && argumentValue is not null)
        {
            return argumentValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        throw WellKnownProjectionExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses an optional <see cref="string"/> array argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument, or <see langword="null"/> if not present.</returns>
    private static string[]? GetNullableStringArrayArgument(Dictionary<string, string?> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue) && argumentValue is not null)
        {
            return argumentValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return null;
    }

    /// <summary>
    /// Parses a <see cref="string"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    private static string GetStringArgument(Dictionary<string, string?> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue) && argumentValue is not null)
        {
            return argumentValue;
        }

        throw WellKnownProjectionExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a nullable (optional) <see cref="string"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument, or <see langword="null"/> if not present.</returns>
    private static string? GetNullableStringArgument(Dictionary<string, string?> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return argumentValue;
        }

        return null;
    }

    /// <summary>
    /// Parses a <see cref="bool"/> flag argument. Boolean flags are present (true) or absent (false).
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns><see langword="true"/> if the flag is present; otherwise, <see langword="false"/>.</returns>
    private static bool GetBooleanFlagArgument(Dictionary<string, string?> argsMap, string propertyName)
    {
        return argsMap.ContainsKey(GetCommandLineArgumentName(propertyName));
    }
}

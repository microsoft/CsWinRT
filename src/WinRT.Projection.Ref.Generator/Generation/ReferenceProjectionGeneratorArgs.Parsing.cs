// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using WindowsRuntime.ReferenceProjectionGenerator.Attributes;
using WindowsRuntime.ReferenceProjectionGenerator.Errors;

#pragma warning disable IDE0046

namespace WindowsRuntime.ReferenceProjectionGenerator.Generation;

/// <inheritdoc cref="ReferenceProjectionGeneratorArgs"/>
internal partial class ReferenceProjectionGeneratorArgs
{
    /// <summary>
    /// Parses an <see cref="ReferenceProjectionGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="ReferenceProjectionGeneratorArgs"/> instance.</returns>
    public static ReferenceProjectionGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
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
            throw WellKnownReferenceProjectionGeneratorExceptions.ResponseFileReadError(e);
        }

        Dictionary<string, string> argsMap = [];

        // Build a map with all the commands and their values
        foreach (string line in responseArgs)
        {
            string trimmedLine = line.Trim();

            // Skip empty lines (the MSBuild ToolTask may emit blank lines).
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            // Each line has the command line argument name followed by a space, and then the
            // argument value. If there are no spaces on any given line, the file is malformed.
            int indexOfSpace = trimmedLine.IndexOf(' ');

            if (indexOfSpace == -1)
            {
                throw WellKnownReferenceProjectionGeneratorExceptions.MalformedResponseFile();
            }

            // Now we can parse the actual command line argument name and value
            string argumentName = trimmedLine.AsSpan()[..indexOfSpace].ToString();
            string argumentValue = trimmedLine.AsSpan()[(indexOfSpace + 1)..].TrimEnd().ToString();

            // We should never have duplicate commands
            if (!argsMap.TryAdd(argumentName, argumentValue))
            {
                throw WellKnownReferenceProjectionGeneratorExceptions.MalformedResponseFile();
            }
        }

        // Parse all commands to create the managed arguments to use
        return new()
        {
            InputPaths = GetStringArrayArgument(argsMap, nameof(InputPaths)),
            OutputDirectory = GetStringArgument(argsMap, nameof(OutputDirectory)),
            TargetFramework = GetStringArgument(argsMap, nameof(TargetFramework)),
            IncludeNamespaces = GetOptionalStringArrayArgument(argsMap, nameof(IncludeNamespaces)),
            ExcludeNamespaces = GetOptionalStringArrayArgument(argsMap, nameof(ExcludeNamespaces)),
            AdditionExcludeNamespaces = GetOptionalStringArrayArgument(argsMap, nameof(AdditionExcludeNamespaces)),
            Verbose = GetOptionalBoolArgument(argsMap, nameof(Verbose)),
            Component = GetOptionalBoolArgument(argsMap, nameof(Component)),
            Internal = GetOptionalBoolArgument(argsMap, nameof(Internal)),
            PublicExclusiveTo = GetOptionalBoolArgument(argsMap, nameof(PublicExclusiveTo)),
            IdicExclusiveTo = GetOptionalBoolArgument(argsMap, nameof(IdicExclusiveTo)),
            ReferenceProjection = GetOptionalBoolArgument(argsMap, nameof(ReferenceProjection)),
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
            return typeof(ReferenceProjectionGeneratorArgs).GetProperty(propertyName)!.GetCustomAttribute<CommandLineArgumentNameAttribute>()!.Name;
        }
        catch (Exception e)
        {
            throw WellKnownReferenceProjectionGeneratorExceptions.ResponseFileArgumentParsingError(propertyName, e);
        }
    }

    /// <summary>
    /// Parses a required <see cref="string"/> array argument.
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

        throw WellKnownReferenceProjectionGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses an optional <see cref="string"/> array argument, returning an empty array if not present.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument, or an empty array if not found.</returns>
    private static string[] GetOptionalStringArrayArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return argumentValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [];
    }

    /// <summary>
    /// Parses a required <see cref="string"/> argument.
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

        throw WellKnownReferenceProjectionGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses an optional <see cref="bool"/> argument, returning <c>false</c> if not present.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument, or <c>false</c> if not found.</returns>
    private static bool GetOptionalBoolArgument(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName(propertyName), out string? argumentValue))
        {
            return bool.TryParse(argumentValue, out bool result) && result;
        }

        return false;
    }
}

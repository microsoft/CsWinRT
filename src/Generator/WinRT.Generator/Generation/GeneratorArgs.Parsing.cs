// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Attributes;
using WindowsRuntime.Generator.Errors;

#pragma warning disable IDE0046

namespace WindowsRuntime.Generator.Generation;

/// <summary>
/// Parsing functions for GeneratorArgs.
/// </summary>
internal static class GeneratorArgs
{
    /// <summary>
    /// Parses key-value input from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <returns>The resulting <see cref="Dictionary{TKey, TValue}"/> instance.</returns>
    public static Dictionary<string, string> ParseFromResponseFile(string path)
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
            throw WellKnownGeneratorExceptions.ResponseFileReadError(e);
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
                throw WellKnownGeneratorExceptions.MalformedResponseFile();
            }

            // Now we can parse the actual command line argument name and value
            string argumentName = trimmedLine.AsSpan()[..indexOfSpace].ToString();
            string argumentValue = trimmedLine.AsSpan()[(indexOfSpace + 1)..].TrimEnd().ToString();

            // We should never have duplicate commands
            if (!argsMap.TryAdd(argumentName, argumentValue))
            {
                throw WellKnownGeneratorExceptions.MalformedResponseFile();
            }
        }

        return argsMap;
    }

    /// <summary>
    /// Gets the command line argument name for a property.
    /// </summary>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The command line argument name for <paramref name="propertyName"/>.</returns>
    public static string GetCommandLineArgumentName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] GeneratorArgs>(string propertyName)
    {
        try
        {
            return typeof(GeneratorArgs).GetProperty(propertyName)!.GetCustomAttribute<CommandLineArgumentNameAttribute>()!.Name;
        }
        catch (Exception e)
        {
            throw WellKnownGeneratorExceptions.ResponseFileArgumentParsingError(propertyName, e);
        }
    }

    /// <summary>
    /// Parses a <see cref="string"/> array argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    public static string[] GetStringArrayArgument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] GeneratorArgs>(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName<GeneratorArgs>(propertyName), out string? argumentValue))
        {
            return argumentValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        throw WellKnownGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a <see cref="string"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    public static string GetStringArgument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] GeneratorArgs>(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName<GeneratorArgs>(propertyName), out string? argumentValue))
        {
            return argumentValue;
        }

        throw WellKnownGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a nullable (optional) <see cref="string"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    public static string? GetNullableStringArgument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] GeneratorArgs>(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName<GeneratorArgs>(propertyName), out string? argumentValue))
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
    public static int GetInt32Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] GeneratorArgs>(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName<GeneratorArgs>(propertyName), out string? argumentValue))
        {
            if (int.TryParse(argumentValue, out int parsedValue))
            {
                return parsedValue;
            }
        }

        throw WellKnownGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
    }

    /// <summary>
    /// Parses a <see cref="bool"/> argument.
    /// </summary>
    /// <param name="argsMap">The input map with raw arguments.</param>
    /// <param name="propertyName">The target property name.</param>
    /// <returns>The resulting argument.</returns>
    public static bool GetBooleanArgument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] GeneratorArgs>(Dictionary<string, string> argsMap, string propertyName)
    {
        if (argsMap.TryGetValue(GetCommandLineArgumentName<GeneratorArgs>(propertyName), out string? argumentValue))
        {
            if (bool.TryParse(argumentValue, out bool parsedValue))
            {
                return parsedValue;
            }
        }

        throw WellKnownGeneratorExceptions.ResponseFileArgumentParsingError(propertyName);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using WindowsRuntime.Generator.Attributes;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.Generator.Parsing;

/// <summary>
/// Helper to parse response files into argument objects.
/// </summary>
internal static class ResponseFileParser
{
    /// <summary>
    /// Parses an instance of <typeparamref name="TArgs"/> from a response file at the given path.
    /// </summary>
    /// <remarks>
    /// The path may be prefixed with <c>@</c> (matching MSBuild's default escaping
    /// for <c>ToolTask</c> response files), which is stripped before reading the file.
    /// </remarks>
    /// <typeparam name="TArgs">The strongly-typed args record. Must have a public parameterless constructor surface (only public properties are inspected via reflection).</typeparam>
    /// <typeparam name="TErr">The per-tool well-known exception factory used to route parsing errors.</typeparam>
    /// <param name="path">The path to the response file (optionally prefixed with <c>@</c>).</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The populated <typeparamref name="TArgs"/> instance.</returns>
    public static TArgs Parse<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TArgs, TErr>(
        string path,
        CancellationToken token)
        where TArgs : class
        where TErr : IGeneratorErrorFactory
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
            throw TErr.ResponseFileReadError(e);
        }

        Dictionary<string, string> argsMap = BuildArgsMap<TErr>(lines);

        return Populate<TArgs, TErr>(argsMap, token);
    }

    /// <summary>
    /// Parses an instance of <typeparamref name="TArgs"/> from a response file read from a stream.
    /// </summary>
    /// <typeparam name="TArgs">The strongly-typed args record.</typeparam>
    /// <typeparam name="TErr">The per-tool well-known exception factory used to route parsing errors.</typeparam>
    /// <param name="stream">The stream containing the response file content.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The populated <typeparamref name="TArgs"/> instance.</returns>
    public static TArgs Parse<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TArgs, TErr>(
        Stream stream,
        CancellationToken token)
        where TArgs : class
        where TErr : IGeneratorErrorFactory
    {
        string[] lines = File.ReadAllLines(stream);

        Dictionary<string, string> argsMap = BuildArgsMap<TErr>(lines);

        return Populate<TArgs, TErr>(argsMap, token);
    }

    /// <summary>
    /// Builds the name-to-value map from the lines of a response file.
    /// </summary>
    /// <typeparam name="TErr">The per-tool well-known exception factory used to route parsing errors.</typeparam>
    /// <param name="lines">The lines read from the response file.</param>
    /// <returns>The resulting map.</returns>
    private static Dictionary<string, string> BuildArgsMap<TErr>(string[] lines)
        where TErr : IGeneratorErrorFactory
    {
        Dictionary<string, string> argsMap = [];

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Skip empty lines (the MSBuild ToolTask may emit blank lines)
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            // Each line has the command line argument name followed by a space, and then the
            // argument value. If there are no spaces on any given line, the file is malformed.
            int indexOfSpace = trimmedLine.IndexOf(' ');

            if (indexOfSpace == -1)
            {
                throw TErr.MalformedResponseFile();
            }

            // Now we can parse the actual command line argument name and value
            string argumentName = trimmedLine.AsSpan()[..indexOfSpace].ToString();
            string argumentValue = trimmedLine.AsSpan()[(indexOfSpace + 1)..].TrimEnd().ToString();

            // We should never have duplicate commands
            if (!argsMap.TryAdd(argumentName, argumentValue))
            {
                throw TErr.MalformedResponseFile();
            }
        }

        return argsMap;
    }

    /// <summary>
    /// Populates an arguments object with the provided parsed values.
    /// </summary>
    /// <typeparam name="TArgs">The strongly-typed args record.</typeparam>
    /// <typeparam name="TErr">The per-tool well-known exception factory used to route parsing errors.</typeparam>
    /// <param name="argsMap">The pre-built name-to-value map.</param>
    /// <param name="token">The cancellation token, assigned to any <see cref="CancellationToken"/>-typed property on <typeparamref name="TArgs"/>.</param>
    /// <returns>The populated <typeparamref name="TArgs"/> instance.</returns>
    private static TArgs Populate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TArgs, TErr>(
        Dictionary<string, string> argsMap,
        CancellationToken token)
        where TArgs : class
        where TErr : IGeneratorErrorFactory
    {
        // We bypass any constructor (and the runtime's required-member enforcement) by using
        // 'GetUninitializedObject'. All properties are then populated via reflection from the
        // response file values, with explicit defaults applied for non-required properties.
        TArgs instance = (TArgs)RuntimeHelpers.GetUninitializedObject(typeof(TArgs));

        foreach (PropertyInfo property in typeof(TArgs).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Type propertyType = property.PropertyType;

            // The cancellation token property is always assigned from the parser's argument,
            // not from the response file (it has no '[CommandLineArgumentName]' annotation).
            if (propertyType == typeof(CancellationToken))
            {
                property.SetValue(instance, token);

                continue;
            }

            // Properties without the CLI attribute are not part of the response file contract
            CommandLineArgumentNameAttribute? cliAttribute = property.GetCustomAttribute<CommandLineArgumentNameAttribute>();

            if (cliAttribute is null)
            {
                continue;
            }

            bool isRequired = property.GetCustomAttribute<RequiredMemberAttribute>() is not null;
            bool hasValue = argsMap.TryGetValue(cliAttribute.Name, out string? rawValue);

            if (!hasValue)
            {
                ApplyDefault<TErr>(instance, property, propertyType, isRequired);

                continue;
            }

            // For required values, a parse failure throws (preserving the existing behavior of
            // 'GetBooleanArgument' / 'GetInt32Argument' / 'GetStringArrayArgument'). For optional
            // values, a parse failure silently falls back to the default (matching the existing
            // 'GetOptionalBoolArgument' / 'GetOptionalStringArrayArgument' behavior).
            if (TryConvert(rawValue!, propertyType, out object? converted))
            {
                property.SetValue(instance, converted);
            }
            else if (isRequired)
            {
                throw TErr.ResponseFileArgumentParsingError(property.Name, null);
            }
            else
            {
                ApplyDefault<TErr>(instance, property, propertyType, isRequired: false);
            }
        }

        return instance;
    }

    /// <summary>
    /// Applies the default value for a property when the response file does not provide one.
    /// </summary>
    /// <typeparam name="TErr">The per-tool well-known exception factory used to route parsing errors.</typeparam>
    /// <param name="instance">The args instance being populated.</param>
    /// <param name="property">The property being set.</param>
    /// <param name="propertyType">The property's type.</param>
    /// <param name="isRequired">Whether the property has <see cref="RequiredMemberAttribute"/>.</param>
    private static void ApplyDefault<TErr>(
        object instance,
        PropertyInfo property,
        Type propertyType,
        bool isRequired)
        where TErr : IGeneratorErrorFactory
    {
        if (isRequired)
        {
            throw TErr.ResponseFileArgumentParsingError(property.Name, null);
        }

        // '[DefaultValue("…")]' takes precedence: it lets per-tool args express initializer-style
        // defaults that 'GetUninitializedObject' would otherwise skip (initializers aren't needed).
        DefaultValueAttribute? defaultValueAttribute = property.GetCustomAttribute<DefaultValueAttribute>();

        if (defaultValueAttribute is not null)
        {
            property.SetValue(instance, defaultValueAttribute.Value);

            return;
        }

        // 'string[]' properties default to an empty array (matching 'GetOptionalStringArrayArgument').
        if (propertyType == typeof(string[]))
        {
            property.SetValue(instance, Array.Empty<string>());

            return;
        }
    }

    /// <summary>
    /// Converts a raw string from the response file into the target property type.
    /// </summary>
    /// <param name="rawValue">The raw string value from the response file.</param>
    /// <param name="targetType">The destination property type.</param>
    /// <param name="converted">The converted value on success.</param>
    /// <returns><see langword="true"/> on success; <see langword="false"/> if the value cannot be parsed for <paramref name="targetType"/>.</returns>
    private static bool TryConvert(string rawValue, Type targetType, out object? converted)
    {
        if (targetType == typeof(string))
        {
            converted = rawValue;

            return true;
        }

        if (targetType == typeof(string[]))
        {
            converted = rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return true;
        }

        // For primitives ('bool', 'int', etc.) we rely on the standard 'Convert.ChangeType', which
        // is AOT-safe for the built-in primitives and uses invariant culture for deterministic parsing.
        try
        {
            converted = Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);

            return true;
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            converted = null;

            return false;
        }
    }
}

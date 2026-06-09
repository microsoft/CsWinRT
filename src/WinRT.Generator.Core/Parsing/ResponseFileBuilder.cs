// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using WindowsRuntime.Generator.Attributes;

namespace WindowsRuntime.Generator.Parsing;

/// <summary>
/// Helper to format argument objects into response files.
/// </summary>
internal static class ResponseFileBuilder
{
    /// <summary>
    /// Formats <paramref name="args"/> as a response file string.
    /// </summary>
    /// <typeparam name="TArgs">The strongly-typed args record.</typeparam>
    /// <param name="args">The args instance to format.</param>
    /// <returns>The formatted response file text.</returns>
    public static string Format<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(TArgs args)
        where TArgs : class
    {
        StringBuilder builder = new();

        foreach (PropertyInfo property in typeof(TArgs).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            CommandLineArgumentNameAttribute? cliAttribute = property.GetCustomAttribute<CommandLineArgumentNameAttribute>();

            if (cliAttribute is null)
            {
                continue;
            }

            object? value = property.GetValue(args);

            // 'null' round-trips as "missing" (the parser will apply the property's default)
            if (value is null)
            {
                continue;
            }

            // Empty arrays round-trip as "missing" too: the parser defaults 'string[]' properties to
            // an empty array. Emitting "name<space>" for an empty array would also fail the parser's
            // 'IndexOf(\' \') == -1 -> MalformedResponseFile' check (after a trailing-space trim).
            if (value is ICollection { Count: 0 })
            {
                continue;
            }

            // Optional booleans default to 'false' on parse, so emitting "name false" would be
            // redundant. Required booleans always emit (both 'true' and 'false') so the parser
            // sees them and doesn't reject them as missing-required.
            bool isRequired = property.GetCustomAttribute<RequiredMemberAttribute>() is not null;

            if (value is false && !isRequired)
            {
                continue;
            }

            _ = builder.Append(cliAttribute.Name);
            _ = builder.Append(' ');
            _ = builder.AppendLine(FormatValue(value));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats a single value for inclusion in a response file line.
    /// </summary>
    /// <param name="value">The non-<see langword="null"/> value to format.</param>
    /// <returns>The formatted string representation.</returns>
    private static string FormatValue(object value)
    {
        if (value is string s)
        {
            return s;
        }

        // String values are formatted as comma-separated lists, without brackets or spaces
        if (value is string[] array)
        {
            return string.Join(',', array);
        }

        // All other primitive values use invariant culture, matching the parser's
        // invariant 'Convert.ChangeType' so the round-trip is deterministic.
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}

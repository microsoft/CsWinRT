// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using WindowsRuntime.GeneratorCli.Attributes;

namespace WindowsRuntime.GeneratorCli.Parsing;

/// <summary>
/// Formats an args record into a response file by reflecting over its
/// <c>[<see cref="CommandLineArgumentNameAttribute"/>]</c>-annotated public properties.
/// </summary>
/// <remarks>
/// <para>
/// Properties are emitted in declaration order (the order returned by <see cref="Type.GetProperties()"/>),
/// one <c>name<![CDATA[<space>]]>value</c> per line. The output round-trips cleanly through
/// <see cref="ResponseFileParser"/>.
/// </para>
/// <para>
/// Per-property handling:
/// <list type="bullet">
///   <item><see cref="CancellationToken"/>-typed properties are skipped (they have no CLI surface).</item>
///   <item>Properties without <see cref="CommandLineArgumentNameAttribute"/> are skipped.</item>
///   <item><see langword="null"/> values are skipped (they round-trip as "missing").</item>
///   <item>Empty <see cref="string"/><c>[]</c> values are skipped (they round-trip as "missing"
///   to the optional-array default, matching the previous per-tool emit behavior).</item>
///   <item><see cref="bool"/> values that are <see langword="false"/> on properties without
///   <see cref="System.Runtime.CompilerServices.RequiredMemberAttribute"/> are skipped (they
///   round-trip to the optional-bool default, matching the previous per-tool emit behavior).</item>
///   <item>All other values are formatted using <see cref="CultureInfo.InvariantCulture"/>:
///   strings emit as-is, arrays emit as comma-joined, primitives use <see cref="object.ToString()"/>.</item>
/// </list>
/// </para>
/// </remarks>
internal static class ResponseFileBuilder
{
    /// <summary>The required dynamic-access annotation for the <c>TArgs</c> type parameter.</summary>
    private const DynamicallyAccessedMemberTypes ArgsAccessKinds =
        DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Formats <paramref name="args"/> as a response file string suitable for
    /// <see cref="ResponseFileParser.Parse{TArgs, TErr}(string, CancellationToken)"/>.
    /// </summary>
    /// <typeparam name="TArgs">The strongly-typed args record.</typeparam>
    /// <param name="args">The args instance to format.</param>
    /// <returns>The formatted response file text.</returns>
    public static string Format<[DynamicallyAccessedMembers(ArgsAccessKinds)] TArgs>(TArgs args)
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
            // redundant. Required booleans always emit (both 'true' and 'false') so the parser sees
            // them and doesn't reject them as missing-required.
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

        if (value is string[] array)
        {
            return string.Join(',', array);
        }

        // All other primitive values use invariant culture, matching the parser's invariant
        // 'Convert.ChangeType' so the round-trip is deterministic.
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using WindowsRuntime.ProjectionWriter.Errors;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Selects exclusive-to IDIC support using the projection filter's prefix matching, with unconditional exclusions.
/// </summary>
internal sealed class ExclusiveToInterfaceFilter
{
    private readonly TypeFilter _includes;
    private readonly TypeFilter? _excludes;

    /// <summary>
    /// Creates an exclusive-to IDIC filter. Empty include lists select all eligible interfaces.
    /// </summary>
    /// <param name="includes">Namespace or type-name prefixes to include.</param>
    /// <param name="excludes">Namespace or type-name prefixes to exclude.</param>
    /// <param name="includeTypes">Fully qualified interface names to include exactly.</param>
    public ExclusiveToInterfaceFilter(IEnumerable<string> includes, IEnumerable<string> excludes, IEnumerable<string> includeTypes)
    {
        HashSet<string> includePrefixes = Normalize(includes, nameof(ProjectionWriterOptions.IdicExclusiveToIncludes));
        HashSet<string> excludePrefixes = Normalize(excludes, nameof(ProjectionWriterOptions.IdicExclusiveToExcludes));
        HashSet<string> exactIncludes = Normalize(includeTypes, nameof(ProjectionWriterOptions.IdicExclusiveToTypes));

        _includes = new TypeFilter(includePrefixes, [], exactIncludes);
        _excludes = excludePrefixes.Count > 0 ? new TypeFilter(excludePrefixes, []) : null;
    }

    /// <summary>
    /// Gets whether a type is selected and not excluded.
    /// </summary>
    /// <param name="typeName">The fully qualified interface name.</param>
    /// <returns>Whether the type passes the filter.</returns>
    public bool Includes(string typeName)
    {
        return _includes.Includes(typeName) && _excludes?.Includes(typeName) != true;
    }

    /// <summary>
    /// Trims and deduplicates identifier prefixes, rejecting unsupported wildcard or pattern syntax.
    /// </summary>
    private static HashSet<string> Normalize(IEnumerable<string> filters, string optionName)
    {
        HashSet<string> result = new(StringComparer.Ordinal);

        foreach (string filter in filters)
        {
            if (filter is null)
            {
                throw WellKnownProjectionWriterExceptions.InvalidIdicExclusiveToFilter(optionName, filter);
            }

            string prefix = filter.Trim();

            if (prefix.Length == 0)
            {
                continue;
            }

            bool atIdentifierStart = true;

            foreach (char c in prefix)
            {
                if (c == '.' && !atIdentifierStart)
                {
                    atIdentifierStart = true;
                    continue;
                }

                if (c != '_' && !char.IsLetter(c) && (atIdentifierStart || !char.IsDigit(c)))
                {
                    throw WellKnownProjectionWriterExceptions.InvalidIdicExclusiveToFilter(optionName, filter);
                }

                atIdentifierStart = false;
            }

            _ = result.Add(prefix);
        }

        return result;
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace WindowsRuntime.ProjectionGenerator.Models;

/// <summary>
/// Provides filtering logic for namespace-based include/exclude rules.
/// </summary>
internal sealed class NamespaceFilter
{
    private readonly HashSet<string> _includes;
    private readonly HashSet<string> _excludes;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceFilter"/> class.
    /// </summary>
    /// <param name="includes">The optional set of namespace prefixes to include.</param>
    /// <param name="excludes">The optional set of namespace prefixes to exclude.</param>
    public NamespaceFilter(IEnumerable<string>? includes, IEnumerable<string>? excludes)
    {
        _includes = includes is not null ? new HashSet<string>(includes, StringComparer.Ordinal) : [];
        _excludes = excludes is not null ? new HashSet<string>(excludes, StringComparer.Ordinal) : [];
    }

    /// <summary>
    /// Checks whether a given namespace or qualified type name should be included.
    /// </summary>
    /// <param name="name">The namespace or fully qualified type name.</param>
    /// <returns>Whether the name passes the filter.</returns>
    public bool Includes(string name)
    {
        // If no include prefixes specified, include everything (except excludes)
        // If include prefixes specified, only include if one matches
        bool included = _includes.Count == 0 || MatchesAnyPrefix(name, _includes);
        bool excluded = _excludes.Count > 0 && MatchesAnyPrefix(name, _excludes);
        return included && !excluded;
    }

    private static bool MatchesAnyPrefix(string name, HashSet<string> prefixes)
    {
        foreach (string prefix in prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                // Must match either the full name or a namespace prefix (followed by '.')
                if (name.Length == prefix.Length || name[prefix.Length] == '.')
                {
                    return true;
                }
            }
        }

        return false;
    }
}

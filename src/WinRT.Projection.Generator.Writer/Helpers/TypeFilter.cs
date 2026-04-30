// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Mirrors the C++ <c>winmd::reader::filter</c> include/exclude logic.
/// Filters use longest-prefix-match semantics: type/namespace is checked against
/// each prefix in include/exclude lists, and the longest matching prefix wins.
/// </summary>
internal readonly struct TypeFilter
{
    private readonly List<string> _include;
    private readonly List<string> _exclude;

    public static TypeFilter Empty { get; } = new(Array.Empty<string>(), Array.Empty<string>());

    public TypeFilter(IEnumerable<string> include, IEnumerable<string> exclude)
    {
        _include = include.OrderByDescending(s => s.Length).ToList();
        _exclude = exclude.OrderByDescending(s => s.Length).ToList();
    }

    /// <summary>
    /// Whether this filter matches everything by default (no include rules).
    /// </summary>
    public bool MatchesAllByDefault => _include == null || _include.Count == 0;

    /// <summary>
    /// Returns whether the given type name passes the include/exclude filter.
    /// </summary>
    public bool Includes(string fullName)
    {
        if (_include == null && _exclude == null)
        {
            return true;
        }

        // Find longest matching include prefix
        int includeLen = -1;
        if (_include != null)
        {
            foreach (string p in _include)
            {
                if (IsPrefixMatch(fullName, p))
                {
                    includeLen = p.Length;
                    break;
                }
            }
        }

        // Find longest matching exclude prefix
        int excludeLen = -1;
        if (_exclude != null)
        {
            foreach (string p in _exclude)
            {
                if (IsPrefixMatch(fullName, p))
                {
                    excludeLen = p.Length;
                    break;
                }
            }
        }

        // No include rules => default include unless matched by exclude
        if (_include == null || _include.Count == 0)
        {
            return excludeLen < 0;
        }

        // No matching include
        if (includeLen < 0)
        {
            return false;
        }

        // Include match wins unless exclude is longer-prefix
        return excludeLen <= includeLen;
    }

    public bool Includes(TypeDefinition type)
    {
        return Includes(GetFullName(type));
    }

    private static bool IsPrefixMatch(string name, string prefix)
    {
        if (!name.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }
        if (name.Length == prefix.Length)
        {
            return true;
        }
        // 'prefix' could match either at namespace boundary "."
        // or at exact match
        return name[prefix.Length] == '.';
    }

    public static string GetFullName(TypeDefinition type)
    {
        Utf8String? ns = type.Namespace;
        Utf8String? name = type.Name;
        if (ns is null || ns.Length == 0)
        {
            return name?.Value ?? string.Empty;
        }
        return ns + "." + (name?.Value ?? string.Empty);
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Mirrors the C++ <c>winmd::reader::filter</c> include/exclude logic.
/// Filters use longest-prefix-match semantics: type/namespace is checked against
/// each prefix in include/exclude lists, and the longest matching prefix wins.
/// </summary>
internal readonly struct TypeFilter
{
    private readonly List<string> _include;
    private readonly List<string> _exclude;

    public static TypeFilter Empty { get; } = new([], []);

    public TypeFilter(IEnumerable<string> include, IEnumerable<string> exclude)
    {
        _include = [.. include.OrderByDescending(s => s.Length)];
        _exclude = [.. exclude.OrderByDescending(s => s.Length)];
    }

    /// <summary>
    /// Whether this filter matches everything by default (no include rules).
    /// </summary>
    public bool MatchesAllByDefault => _include == null || _include.Count == 0;

    /// <summary>
    /// Returns whether the given type name passes the include/exclude filter.
    /// Mirrors the C++ <c>winmd::reader::filter</c> algorithm: rules are sorted by descending
    /// prefix length (with includes winning ties over excludes); the first matching rule wins.
    /// Match semantics split the full type name into <c>namespace.typeName</c> parts and treat
    /// the rule prefix as either a namespace-prefix or a namespace + typename-prefix.
    /// </summary>
    public bool Includes(string fullName)
    {
        if ((_include == null || _include.Count == 0) && (_exclude == null || _exclude.Count == 0))
        {
            return true;
        }

        // Split into namespace + typename at the LAST '.'.
        int dot = fullName.LastIndexOf('.');
        string ns;
        string name;
        if (dot < 0)
        {
            ns = fullName;
            name = fullName;
        }
        else
        {
            ns = fullName[..dot];
            name = fullName[(dot + 1)..];
        }

        // Walk both lists in descending length order; on tie, includes win over excludes.
        // (Both _include and _exclude are pre-sorted by descending length in the constructor.)
        int incIdx = 0;
        int excIdx = 0;
        while (true)
        {
            string? incRule = (_include != null && incIdx < _include.Count) ? _include[incIdx] : null;
            string? excRule = (_exclude != null && excIdx < _exclude.Count) ? _exclude[excIdx] : null;
            if (incRule == null && excRule == null) { break; }

            bool pickInclude;
            if (incRule == null)
            {
                pickInclude = false;
            }
            else if (excRule == null)
            {
                pickInclude = true;
            }
            else
            {
                // Equal length: include wins (matches C++ sort key 'pair{size, !isInclude}' descending).
                pickInclude = incRule.Length >= excRule.Length;
            }

            string rule = pickInclude ? incRule! : excRule!;
            if (Match(ns, name, rule))
            {
                return pickInclude;
            }
            if (pickInclude) { incIdx++; } else { excIdx++; }
        }

        // No rule matched. If we have any include rules, default-exclude; else default-include.
        return _include == null || _include.Count == 0;
    }
    private static bool Match(string typeNamespace, string typeName, string rule)
    {
        if (rule.Length <= typeNamespace.Length)
        {
            return typeNamespace.StartsWith(rule, StringComparison.Ordinal);
        }
        if (!rule.StartsWith(typeNamespace, StringComparison.Ordinal))
        {
            return false;
        }
        if (rule[typeNamespace.Length] != '.')
        {
            return false;
        }
        // The rest of the rule (after 'namespace.') is matched as a prefix against typeName.
        string rest = rule[(typeNamespace.Length + 1)..];
        return typeName.StartsWith(rest, StringComparison.Ordinal);
    }

    public bool Includes(TypeDefinition type)
    {
        return Includes(GetFullName(type));
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

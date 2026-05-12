// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Include/exclude type filter using longest-prefix-match semantics: type/namespace is checked
/// against each prefix in the include/exclude lists, and the longest matching prefix wins.
/// </summary>
internal sealed class TypeFilter
{
    private readonly List<string> _include;
    private readonly List<string> _exclude;

    /// <summary>
    /// Gets a default <see cref="TypeFilter"/> that has no include and no exclude rules
    /// (and therefore matches every type).
    /// </summary>
    public static TypeFilter Empty { get; } = new([], []);

    /// <summary>
    /// Initializes a new <see cref="TypeFilter"/> with the given include and exclude prefix lists.
    /// </summary>
    /// <param name="include">The include prefixes (a type matches if any prefix matches; empty means match-all).</param>
    /// <param name="exclude">The exclude prefixes (a type is rejected if any prefix matches and no include prefix wins).</param>
    public TypeFilter(IEnumerable<string> include, IEnumerable<string> exclude)
    {
        _include = [.. include.OrderByDescending(s => s.Length)];
        _exclude = [.. exclude.OrderByDescending(s => s.Length)];
    }

    /// <summary>
    /// Whether this filter matches everything by default (no include rules).
    /// </summary>
    public bool MatchesAllByDefault => _include.Count == 0;

    /// <summary>
    /// Returns whether the given type name passes the include/exclude filter.
    /// Rules are sorted by descending prefix length (with includes winning ties over excludes);
    /// the first matching rule wins. Match semantics split the full type name into
    /// <c>namespace.typeName</c> parts and treat the rule prefix as either a namespace-prefix or
    /// a namespace + typename-prefix.
    /// </summary>
    public bool Includes(string fullName)
    {
        if (_include.Count == 0 && _exclude.Count == 0)
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
            string? incRule = incIdx < _include.Count ? _include[incIdx] : null;
            string? excRule = excIdx < _exclude.Count ? _exclude[excIdx] : null;

            if (incRule == null && excRule == null)
            {
                break;
            }

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
                // Equal length: include wins (this is the documented tie-breaker).
                pickInclude = incRule.Length >= excRule.Length;
            }

            string rule = pickInclude ? incRule! : excRule!;

            if (Match(ns, name, rule))
            {
                return pickInclude;
            }

            if (pickInclude)
            {
                incIdx++;
            }
            else
            {
                excIdx++;
            }
        }

        // No rule matched. If we have any include rules, default-exclude; else default-include.
        return _include.Count == 0;
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

    /// <summary>
    /// Returns whether the given <paramref name="type"/> passes the include/exclude filter.
    /// Computes the type's full name (<c>namespace.typeName</c>) and delegates to
    /// <see cref="Includes(string)"/>.
    /// </summary>
    /// <param name="type">The type definition to test.</param>
    /// <returns><see langword="true"/> if the type's full name is included.</returns>
    public bool Includes(TypeDefinition type)
    {
        return Includes(GetFullName(type));
    }

    /// <summary>
    /// Returns the full name of <paramref name="type"/> in the <c>namespace.typeName</c> form
    /// (or just the type name when the namespace is empty).
    /// </summary>
    /// <param name="type">The type definition to format.</param>
    /// <returns>The fully-qualified type name.</returns>
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
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Include/exclude type filter using longest-prefix-match semantics: type/namespace is checked
/// against each prefix in the include/exclude lists, and the longest matching prefix wins.
/// </summary>
/// <remarks>
/// The semantics are:
/// <list type="bullet">
///   <item>If there are no include and no exclude rules at all, everything is included.</item>
///   <item>Otherwise a type is included only if the longest matching rule is an include; if no
///   rule matches (even when only exclude rules are present), the type is <b>excluded</b>.</item>
///   <item>On an equal-length include/exclude tie, the <b>exclude</b> wins.</item>
/// </list>
/// In other words, once any rule exists the filter behaves as a whitelist: excludes only carve
/// exceptions out of includes, they do not by themselves include everything else.
/// </remarks>
internal sealed class TypeFilter
{
    private readonly List<string> _include;
    private readonly List<string> _exclude;

    /// <summary>
    /// Initializes a new <see cref="TypeFilter"/> with the given include and exclude prefix lists.
    /// </summary>
    /// <param name="include">The include prefixes (a type matches if any prefix matches).</param>
    /// <param name="exclude">The exclude prefixes (a type is rejected if any prefix matches and no longer include prefix wins).</param>
    public TypeFilter(IEnumerable<string> include, IEnumerable<string> exclude)
    {
        _include = [.. include.OrderByDescending(s => s.Length)];
        _exclude = [.. exclude.OrderByDescending(s => s.Length)];
    }

    /// <summary>
    /// Returns whether the given type name passes the include/exclude filter.
    /// Rules are sorted by descending prefix length (with excludes winning ties over includes);
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

        // Walk both lists in descending length order; on tie, excludes win over includes.
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
                // Equal length: exclude wins.
                pickInclude = incRule.Length > excRule.Length;
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

        // No rule matched. Since at least one rule exists (the both-empty case returned true
        // above), default to exclude. This means an excludes-only configuration (no includes)
        // projects nothing rather than everything-but-excluded.
        return false;
    }

    private static bool Match(string typeNamespace, string typeName, string rule)
    {
        if (rule.Length <= typeNamespace.Length)
        {
            // A namespace rule only matches on a segment boundary, so that 'Windows' matches 'Windows' and
            // 'Windows.Foundation', but not an unrelated top level namespace that merely begins with the same
            // characters, such as 'WindowsRuntime'. Without this, the Windows SDK projection claims types that
            // belong to another projection entirely.
            return
                typeNamespace.StartsWith(rule, StringComparison.Ordinal) &&
                (rule.Length == typeNamespace.Length || typeNamespace[rule.Length] == '.');
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
}
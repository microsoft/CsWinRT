// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;

namespace WindowsRuntime.ProjectionGenerator.Helpers;

/// <summary>
/// Provides helpers for C# keyword detection and identifier escaping.
/// </summary>
internal static class CSharpKeywords
{
    /// <summary>
    /// The set of C# reserved keywords.
    /// </summary>
    private static readonly FrozenSet<string> Keywords = FrozenSet.ToFrozenSet(
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    ], StringComparer.Ordinal);

    /// <summary>
    /// Checks whether the given name is a C# keyword.
    /// </summary>
    public static bool IsKeyword(string name)
    {
        return Keywords.Contains(name);
    }

    /// <summary>
    /// Escapes a name by prefixing with '@' if it's a C# keyword.
    /// </summary>
    public static string EscapeIdentifier(string name)
    {
        return IsKeyword(name) ? $"@{name}" : name;
    }
}

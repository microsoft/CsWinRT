// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Recognizes C# language keywords.
/// </summary>
internal static class CSharpKeywords
{
    private static readonly HashSet<string> s_keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
        "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
        "volatile", "while"
    ];

    /// <summary>
    /// Returns whether <paramref name="identifier"/> is a reserved C# language keyword.
    /// </summary>
    /// <param name="identifier">The identifier to test.</param>
    /// <returns><see langword="true"/> if <paramref name="identifier"/> is a C# keyword; otherwise <see langword="false"/>.</returns>
    public static bool IsKeyword(string identifier) => s_keywords.Contains(identifier);
}
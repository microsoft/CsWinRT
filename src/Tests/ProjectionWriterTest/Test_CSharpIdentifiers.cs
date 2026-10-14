// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using WindowsRuntime.Generator.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Covers <see cref="CSharpIdentifiers"/>, whose escaping has to agree with the CsWinRT source generator's (which is based on
/// Roslyn): a component's generated activation entry point forwards to <c>ABI.&lt;AssemblyName&gt;.ManagedExports</c> by name.
/// </summary>
[TestClass]
public class Test_CSharpIdentifiers
{
    [TestMethod]
    public void EscapeIdentifierName_MatchesRoslynForEveryKeyword()
    {
        // This covers both reserved keywords, which have to be escaped, and contextual ones, which are valid identifiers
        foreach (SyntaxKind kind in SyntaxFacts.GetKeywordKinds())
        {
            string keyword = SyntaxFacts.GetText(kind);

            Assert.AreEqual(EscapeWithRoslyn(keyword), CSharpIdentifiers.EscapeIdentifierName(keyword), keyword);
        }
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Contoso.Widgets")]
    [DataRow("1Contoso")]
    [DataRow("Contoso-Widgets.v2")]
    [DataRow("@class")]
    [DataRow("Ünïcödé.Wïdgets")]
    public void EscapeIdentifierName_MatchesRoslyn(string value)
    {
        Assert.AreEqual(EscapeWithRoslyn(value), CSharpIdentifiers.EscapeIdentifierName(value));
    }

    /// <summary>
    /// Escapes a name the same way the CsWinRT source generator does (see <c>StringExtensions.EscapeIdentifierName</c>).
    /// </summary>
    private static string EscapeWithRoslyn(string value)
    {
        if (value is "")
        {
            return "_";
        }

        string escapedValue = value;

        if (!SyntaxFacts.IsValidIdentifier(value))
        {
            StringBuilder builder = new();

            if (!SyntaxFacts.IsIdentifierStartCharacter(value[0]))
            {
                _ = builder.Append('_');
            }

            foreach (char c in value)
            {
                _ = builder.Append(SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');
            }

            escapedValue = builder.ToString();
        }

        return SyntaxFacts.GetKeywordKind(escapedValue) is not SyntaxKind.None ? $"_{escapedValue}" : escapedValue;
    }
}

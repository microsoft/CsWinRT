// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.Generator.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Covers <see cref="CSharpIdentifiers"/>, whose escaping has to agree with the CsWinRT source generator's: a component's
/// generated activation entry point forwards to <c>ABI.&lt;AssemblyName&gt;.ManagedExports</c> by name.
/// </summary>
[TestClass]
public class Test_CSharpIdentifiers
{
    [TestMethod]
    [DataRow("", "_")]
    [DataRow("Contoso.Widgets", "Contoso_Widgets")]
    [DataRow("1Contoso", "_1Contoso")]
    [DataRow("@class", "__class")]
    [DataRow("class", "_class")]
    [DataRow("__arglist", "___arglist")]
    [DataRow("var", "var")]
    [DataRow("Ünïcödé.Wïdgets", "Ünïcödé_Wïdgets")]
    public void EscapeIdentifierName(string value, string expected)
    {
        Assert.AreEqual(expected, CSharpIdentifiers.EscapeIdentifierName(value));
    }
}

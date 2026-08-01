// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// End-to-end tests for how the WinMD generator translates the .NET <c>[Experimental]</c> attribute.
/// </summary>
/// <remarks>
/// <para>
/// The Windows Runtime <c>[Experimental]</c> attribute is custom-mapped to the .NET one, so it has no
/// projected form an author could apply. Authored components use the .NET attribute instead, and the
/// generator translates it back into Windows Runtime metadata (see <c>docs/attribute-projections.md</c>).
/// </para>
/// <para>
/// The .NET attribute's diagnostic id, url format and message have no Windows Runtime counterpart and
/// are dropped, so the tests only assert the presence and placement of the translated attribute.
/// </para>
/// </remarks>
[TestClass]
public class Test_ExperimentalAttribute
{
    /// <summary>
    /// The name of the Windows Runtime attribute the .NET one is translated into.
    /// </summary>
    private const string WindowsRuntimeExperimentalAttribute = "Windows.Foundation.Metadata.ExperimentalAttribute";

    /// <summary>
    /// The name of the .NET attribute, which must never reach the <c>.winmd</c> (its type does not exist
    /// in Windows Runtime metadata, so a copied application would not even be resolvable).
    /// </summary>
    private const string DotNetExperimentalAttribute = "System.Diagnostics.CodeAnalysis.ExperimentalAttribute";

    [TestMethod]
    public void ExperimentalOnTypesAndMethods_IsTranslated()
    {
        ILookup<string, string> attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            using System.Diagnostics.CodeAnalysis;

            [Experimental("TEST0001")]
            public interface IExperimentalComponent
            {
                int Method(int value);
            }

            public interface IComponent
            {
                [Experimental("TEST0002", UrlFormat = "https://example.com/{0}", Message = "Not final")]
                int ExperimentalMethod(int value);

                int StableMethod(int value);
            }
            """);

        AssertIsExperimental(attributes, "IExperimentalComponent");
        AssertIsExperimental(attributes, "IComponent.ExperimentalMethod");

        AssertIsNotExperimental(attributes, "IComponent");
        AssertIsNotExperimental(attributes, "IComponent.StableMethod");

        // The .NET attribute type has no Windows Runtime counterpart, so no row may reference it
        Assert.IsFalse(
            attributes.SelectMany(static group => group).Contains(DotNetExperimentalAttribute),
            $"'{DotNetExperimentalAttribute}' should never be copied into the '.winmd'.");
    }

    /// <summary>
    /// Windows Runtime metadata places member-level markers on the accessor method rather than on the
    /// property or event row, which is the placement MIDL produces.
    /// </summary>
    [TestMethod]
    public void ExperimentalOnPropertiesAndEvents_IsTranslatedOntoTheAccessor()
    {
        ILookup<string, string> attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            using System;
            using System.Diagnostics.CodeAnalysis;

            public interface IComponent
            {
                [Experimental("TEST0001")]
                int ExperimentalProperty { get; set; }

                [Experimental("TEST0002")]
                event EventHandler<int> ExperimentalEvent;
            }
            """);

        AssertIsExperimental(attributes, "IComponent.get_ExperimentalProperty");
        AssertIsExperimental(attributes, "IComponent.add_ExperimentalEvent");

        AssertIsNotExperimental(attributes, "IComponent.ExperimentalProperty");
        AssertIsNotExperimental(attributes, "IComponent.ExperimentalEvent");
    }

    /// <summary>
    /// Asserts that exactly one Windows Runtime <c>[Experimental]</c> attribute is applied to a row.
    /// </summary>
    /// <param name="attributes">The attributes read back from the generated <c>.winmd</c>.</param>
    /// <param name="row">The metadata row to check (see <c>WinMDGeneratorRunner.GetGeneratedAttributes</c>).</param>
    private static void AssertIsExperimental(ILookup<string, string> attributes, string row)
    {
        int count = attributes[row].Count(static name => name == WindowsRuntimeExperimentalAttribute);

        Assert.AreEqual(1, count, $"'{row}' should have exactly one '[{WindowsRuntimeExperimentalAttribute}]' applied.");
    }

    /// <summary>
    /// Asserts that no Windows Runtime <c>[Experimental]</c> attribute is applied to a row.
    /// </summary>
    /// <param name="attributes">The attributes read back from the generated <c>.winmd</c>.</param>
    /// <param name="row">The metadata row to check (see <c>WinMDGeneratorRunner.GetGeneratedAttributes</c>).</param>
    private static void AssertIsNotExperimental(ILookup<string, string> attributes, string row)
    {
        bool isExperimental = attributes[row].Contains(WindowsRuntimeExperimentalAttribute);

        Assert.IsFalse(isExperimental, $"'{row}' should have no '[{WindowsRuntimeExperimentalAttribute}]' applied.");
    }
}

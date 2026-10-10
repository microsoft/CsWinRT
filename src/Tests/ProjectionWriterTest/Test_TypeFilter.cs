// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Helpers;

namespace ProjectionWriterTest;

[TestClass]
public class Test_TypeFilter
{
    [TestMethod]
    [DataRow("Contoso.User", true)]
    [DataRow("Contoso.User2", false)]
    [DataRow("Contoso.UserProfile.UserSetupManager", false)]
    [DataRow("Contoso.User.Profile", false)]
    [DataRow("Contoso.Users.User", false)]
    [DataRow("contoso.User", false)]
    [DataRow("Unrelated.User", false)]
    public void ExactTypeIncludes_DoNotMatchPrefixes(string name, bool expected)
    {
        TypeFilter filter = new([], [], ["Contoso.User"]);

        Assert.AreEqual(expected, filter.Includes(name));
    }

    [TestMethod]
    [DataRow("Contoso.User", true)]
    [DataRow("Contoso.User2", true)]
    [DataRow("Contoso.UserProfile.UserSetupManager", false)]
    [DataRow("Unrelated.User", false)]
    public void PrefixIncludes_KeepExistingSemantics(string name, bool expected)
    {
        // Matched on a segment boundary, so 'Contoso.User' does not claim 'Contoso.UserProfile',
        // but still as a prefix against the type name within a namespace, hence 'Contoso.User2'.
        TypeFilter filter = new(["Contoso.User"], []);

        Assert.AreEqual(expected, filter.Includes(name));
    }

    [TestMethod]
    [DataRow("Windows", "Windows.UI.Xaml", "Windows.Foundation.Uri", true)]
    [DataRow("Windows", "Windows.UI.Xaml", "Windows.UI.Xaml.Controls.Button", false)]
    [DataRow("Windows.UI.Xaml", "Windows", "Windows.UI.Xaml.Controls.Button", true)]
    [DataRow("Windows.UI.Xaml", "Windows", "Windows.Foundation.Uri", false)]
    [DataRow("Contoso.User", "Contoso.User", "Contoso.User", false)]
    public void PrefixRules_LongestMatchWinsWithExcludeTies(string include, string exclude, string name, bool expected)
    {
        TypeFilter filter = new([include], [exclude]);

        Assert.AreEqual(expected, filter.Includes(name));
    }

    [TestMethod]
    [DataRow("Windows", true)]
    [DataRow("WindowsExtension", true)]
    [DataRow("WindowsExtension.User", false)]
    public void ExactTypeIncludes_RespectExcludePrecedence(string exclude, bool expected)
    {
        TypeFilter filter = new([], [exclude], ["WindowsExtension.User"]);

        Assert.AreEqual(expected, filter.Includes("WindowsExtension.User"));
    }

    [TestMethod]
    [DataRow("Contoso.User", true)]
    [DataRow("Contoso.User2", false)]
    [DataRow("Microsoft.UI.Xaml.Controls.Button", true)]
    [DataRow("Microsoft.UI.Xaml.Excluded.Type", false)]
    public void ExactTypesAndNamespacePrefixes_CanBeCombined(string name, bool expected)
    {
        TypeFilter filter = new(["Microsoft.UI"], ["Microsoft.UI.Xaml.Excluded"], ["Contoso.User"]);

        Assert.AreEqual(expected, filter.Includes(name));
    }

    [TestMethod]
    [DataRow("Contoso")]
    [DataRow("Contoso.User")]
    [DataRow("Contoso.UserProfile")]
    [DataRow("Microsoft.UI.Xaml")]
    public void ExactTypeIncludes_DoNotIncludeNamespaceAdditions(string ns)
    {
        TypeFilter filter = new([], [], ["Contoso.User"]);

        Assert.IsFalse(filter.IncludesNamespace(ns));
    }

    [TestMethod]
    public void NamespaceIncludes_StillIncludeAdditionsAlongsideExactTypes()
    {
        TypeFilter filter = new(["Microsoft.UI"], [], ["Contoso.User"]);

        Assert.IsTrue(filter.IncludesNamespace("Microsoft.UI.Xaml"));
        Assert.IsFalse(filter.IncludesNamespace("Contoso.User"));
    }

    [TestMethod]
    public void EmptyFilter_IncludesEverything()
    {
        TypeFilter filter = new([], []);

        Assert.IsTrue(filter.Includes("Contoso.User"));
        Assert.IsTrue(filter.IncludesNamespace("Contoso"));
    }

    [TestMethod]
    public void ExcludeOnlyFilter_IncludesNothing()
    {
        TypeFilter filter = new([], ["Windows"]);

        Assert.IsFalse(filter.Includes("Windows.Foundation.Uri"));
        Assert.IsFalse(filter.Includes("Contoso.User"));
        Assert.IsFalse(filter.IncludesNamespace("Contoso"));
    }

    [TestMethod]
    [DataRow("Windows.Foundation.Uri", true)]
    [DataRow("Windows.UI.Color", true)]
    [DataRow("Windows.UI.Contoso.WidgetItemView", false)]
    [DataRow("Windows.UI.Contoso.WidgetQueryResults", false)]
    public void ExactTypeExcludes_CarveOutOfANamespaceInclude(string name, bool expected)
    {
        TypeFilter filter = new(
            include: ["Windows"],
            exclude: [],
            includeTypes: null,
            excludeTypes: ["Windows.UI.Contoso.WidgetItemView", "Windows.UI.Contoso.WidgetQueryResults"]);

        Assert.AreEqual(expected, filter.Includes(name));
    }

    [TestMethod]
    public void ExactTypeExcludes_OutrankAnExactInclude()
    {
        // The two lists come from different projections claiming the same type, and the exclude wins
        TypeFilter filter = new([], [], ["Contoso.User"], ["Contoso.User"]);

        Assert.IsFalse(filter.Includes("Contoso.User"));
    }

    [TestMethod]
    [DataRow("Windows.UI.Contoso.WidgetItemView", false)]
    [DataRow("Windows.UI.Contoso.WidgetItemView2", true)]
    [DataRow("Windows.UI.Contoso.WidgetItemViewFactory", true)]
    [DataRow("Windows.UI.WidgetItemView", true)]
    public void ExactTypeExcludes_DoNotMatchPrefixes(string name, bool expected)
    {
        // A prefix 'exclude' would take the '*Factory' and '*Statics' companions with it
        TypeFilter filter = new(["Windows"], [], null, ["Windows.UI.Contoso.WidgetItemView"]);

        Assert.AreEqual(expected, filter.Includes(name));
    }

    [TestMethod]
    public void ExcludeTypesOnlyFilter_StillIncludesEverythingElse()
    {
        // Unlike a prefix exclude, these say nothing about what else belongs
        TypeFilter filter = new([], [], null, ["Contoso.User"]);

        Assert.IsFalse(filter.Includes("Contoso.User"));
        Assert.IsTrue(filter.Includes("Contoso.Other"));
        Assert.IsTrue(filter.IncludesNamespace("Contoso"));
    }
}

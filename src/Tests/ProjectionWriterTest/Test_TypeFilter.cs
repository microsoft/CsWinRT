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
        // A rule is matched against the namespace on a segment boundary, so 'Contoso.User' does not claim the
        // unrelated 'Contoso.UserProfile' namespace (without this, the Windows SDK projection's 'Windows' rule
        // claims every type under 'WindowsRuntime'). It is still matched as a prefix against the type name
        // within a namespace, which is why 'Contoso.User2' is included.
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
        // What the Windows SDK projection does for a contract that shares the 'Windows' namespace root
        // without belonging to the SDK: the contract's own reference projection names the types it owns,
        // and the SDK projection has to leave exactly those alone while keeping the rest of the root.
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
        // The two lists come from different projections claiming the same type. The exclude wins, matching
        // the prefix rules' behaviour on a tie, so a type is never emitted into two assemblies at once.
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
        // Why these are a separate list rather than entries in 'exclude': that one matches by prefix, so
        // naming a type there would take its '*Factory' and '*Statics' companions with it, which is exactly
        // how Windows Runtime metadata names the rest of the same contract.
        TypeFilter filter = new(["Windows"], [], null, ["Windows.UI.Contoso.WidgetItemView"]);

        Assert.AreEqual(expected, filter.Includes(name));
    }

    [TestMethod]
    public void ExcludeTypesOnlyFilter_StillIncludesEverythingElse()
    {
        // Exact type excludes alone are not a whitelist: unlike a prefix exclude they say nothing about
        // what else belongs, so an otherwise empty filter keeps including everything.
        TypeFilter filter = new([], [], null, ["Contoso.User"]);

        Assert.IsFalse(filter.Includes("Contoso.User"));
        Assert.IsTrue(filter.Includes("Contoso.Other"));
        Assert.IsTrue(filter.IncludesNamespace("Contoso"));
    }
}

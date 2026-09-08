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
    [DataRow("Contoso.UserProfile.UserSetupManager", true)]
    [DataRow("Unrelated.User", false)]
    public void PrefixIncludes_KeepExistingSemantics(string name, bool expected)
    {
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
}

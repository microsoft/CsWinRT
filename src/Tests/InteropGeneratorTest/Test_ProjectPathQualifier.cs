// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.NET.Build.Tasks;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_ProjectPathQualifier
{
    [TestMethod]
    [DataRow(@"\foo\bar.c", @"C:\proj", @"C:\foo\bar.c")]
    [DataRow(@"/foo/bar.c", @"C:\proj", @"C:\foo/bar.c")]
    [DataRow(@"\foo", @"D:\a\b", @"D:\foo")]
    [DataRow(@"\", @"C:\proj", @"C:\")]
    [DataRow(@"\foo\bar.c", @"\\srv\share\proj", @"\\srv\share\foo\bar.c")]
    [DataRow(@"C:foo\bar.c", @"C:\proj", @"C:\proj\foo\bar.c")]
    [DataRow(@"c:foo", @"C:\proj", @"C:\proj\foo")]
    [DataRow(@"C:", @"C:\proj", @"C:\proj")]
    [DataRow(@"D:foo", @"C:\proj", @"D:foo")]
    [DataRow(@"C:\abs\bar.c", @"C:\proj", @"C:\abs\bar.c")]
    [DataRow(@"\\srv\share\a.c", @"C:\proj", @"\\srv\share\a.c")]
    [DataRow(@"\\?\C:\a.c", @"C:\proj", @"\\?\C:\a.c")]
    [DataRow(@"\??\C:\a.c", @"C:\proj", @"\??\C:\a.c")]
    [DataRow(@"C:\proj\rel\bar.c", @"C:\proj", @"C:\proj\rel\bar.c")]
    [DataRow(@"\foo", @"", @"\foo")]
    public void Qualify_ResolvesPartiallyQualifiedPathsAgainstTheProject(string path, string projectDirectory, string expected)
    {
        Assert.AreEqual(expected, ProjectPathQualifier.Qualify(path, projectDirectory));
    }
}

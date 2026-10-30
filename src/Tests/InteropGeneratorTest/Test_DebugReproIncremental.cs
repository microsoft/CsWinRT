// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_DebugReproIncremental
{
    [TestMethod]
    public async Task MissingInteropDebugReproIsRegenerated()
    {
        using InteropGeneratorRunner runner = new();
        string project = runner.CreateMSBuildProject();
        string output = await runner.RunMSBuildAsync(project, null);
        string cache = Path.Combine(Path.GetDirectoryName(output)!, "Discovery.cswinrtgen.cache");
        string plainCache = File.ReadAllText(cache);

        string directory = Directory.CreateDirectory(Path.Combine(runner.Root, "repro")).FullName;
        string relativeDirectory = Path.GetRelativePath(Path.GetDirectoryName(project)!, directory);
        _ = await runner.RunMSBuildAsync(project, null, debugReproDirectory: relativeDirectory);
        string enabledCache = File.ReadAllText(cache);
        string archive = Path.Combine(directory, "interop-debug-repro.zip");
        Assert.AreNotEqual(plainCache, enabledCache);
        Assert.IsTrue(File.Exists(archive));
        DateTime outputWriteTime = File.GetLastWriteTimeUtc(output);

        _ = await runner.RunMSBuildAsync(project, null, debugReproDirectory: relativeDirectory);
        Assert.AreEqual(outputWriteTime, File.GetLastWriteTimeUtc(output),
            "An unchanged debug repro must not rerun the generator.");

        File.Delete(archive);
        _ = await runner.RunMSBuildAsync(project, null, debugReproDirectory: relativeDirectory);
        Assert.IsTrue(File.Exists(archive), "Deleting the debug repro must rerun the interop target.");
        Assert.AreEqual(enabledCache, File.ReadAllText(cache));
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_IncrementalPrivateAttributes
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MissingAttributeAssemblySupportsCacheReuseAndPayloadInvalidation(bool implementPrivateInterface)
    {
        using InteropGeneratorRunner runner = new();
        runner.SetPrivateAttribute(1, implementPrivateInterface);

        Assert.IsFalse(File.Exists(Path.Combine(runner.Root, "PrivateAttributes.dll")));

        string output = await runner.GenerateAsync("cached", treatWarningsAsErrors: !implementPrivateInterface);
        Assert.IsTrue(runner.LoadOutput(output).Module.TopLevelTypes.Any(type =>
            type.Namespace == "ABI" && type.Name?.ToString().Contains("PrivateAttributeInput", StringComparison.Ordinal) is true),
            "The attributed type must have generated a proxy so fingerprinting its metadata is exercised.");
        string responsePath = Path.Combine(Path.GetDirectoryName(output)!, "interop.rsp");
        string cachePath = Path.Combine(Path.GetDirectoryName(output)!, "WinRT.Interop.cache");
        string response = File.ReadAllText(responsePath);
        Assert.IsFalse(response.Contains("PrivateAttributes.dll", StringComparison.Ordinal),
            "The compiler-only attribute assembly must not be a generator input.");
        byte[] expected = File.ReadAllBytes(output);

        File.WriteAllText(responsePath, response.Replace(
            "--enable-incremental-generation False",
            "--enable-incremental-generation True",
            StringComparison.Ordinal));

        (int seedExitCode, string seedLog) = await InteropGeneratorRunner.InvokeGeneratorAsync(responsePath);
        Assert.AreEqual(0, seedExitCode, seedLog);
        StringAssert.Contains(seedLog, "Generating interop code");

        if (implementPrivateInterface)
        {
            StringAssert.Contains(seedLog, "CSWINRTINTEROPGEN0050");
            StringAssert.Contains(seedLog, "MissingAttributes.IPrivate");
        }

        Assert.IsTrue(File.Exists(cachePath));
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(output));

        (int hitExitCode, string hitLog) = await InteropGeneratorRunner.InvokeGeneratorAsync(responsePath);
        Assert.AreEqual(0, hitExitCode, hitLog);
        StringAssert.Contains(hitLog, "Cached interop code ->");
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(output));

        byte[] originalRecord = File.ReadAllBytes(cachePath);
        runner.SetPrivateAttribute(2, implementPrivateInterface);
        Assert.IsFalse(File.Exists(Path.Combine(runner.Root, "PrivateAttributes.dll")));

        string fresh = await runner.GenerateAsync("fresh", treatWarningsAsErrors: !implementPrivateInterface);
        expected = File.ReadAllBytes(fresh);

        (int missExitCode, string missLog) = await InteropGeneratorRunner.InvokeGeneratorAsync(responsePath);
        Assert.AreEqual(0, missExitCode, missLog);
        StringAssert.Contains(missLog, "Generating interop code");
        Assert.IsFalse(missLog.Contains("Cached interop code ->", StringComparison.Ordinal));
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(output));
        Assert.IsFalse(originalRecord.AsSpan(8, 32).SequenceEqual(File.ReadAllBytes(cachePath).AsSpan(8, 32)),
            "Changing the unresolved attribute payload must change the fingerprint.");

        (int updatedHitExitCode, string updatedHitLog) = await InteropGeneratorRunner.InvokeGeneratorAsync(responsePath);
        Assert.AreEqual(0, updatedHitExitCode, updatedHitLog);
        StringAssert.Contains(updatedHitLog, "Cached interop code ->");
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(output));
    }
}

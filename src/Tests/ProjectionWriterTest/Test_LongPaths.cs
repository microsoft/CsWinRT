// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Tests for projection generation when filesystem paths exceed the legacy Windows MAX_PATH limit.
/// </summary>
[TestClass]
public class Test_LongPaths
{
    [TestMethod]
    public void GeneratorExecutable_IsLongPathAware()
    {
        Assert.AreEqual(
            "true",
            ProjectionWriterRunner.GetLongPathAwareManifestValue(),
            "The embedded application manifest does not enable long-path handling.");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LongResponseInputAndOutputPaths_AreSupported(bool useInputDirectory)
    {
        ProjectionWriterRunner.LongPathRunResult result = ProjectionWriterRunner.RunLongPathScenario(useInputDirectory);

        Assert.IsGreaterThan(260, result.ResponseFilePathLength, "The response file path must exceed MAX_PATH.");
        Assert.IsGreaterThan(260, result.InputPathLength, "The input path must exceed MAX_PATH.");
        Assert.IsGreaterThan(260, result.OutputPathLength, "The generated source path must exceed MAX_PATH.");
        Assert.AreEqual(0, result.ExitCode, result.Output);
        Assert.IsTrue(result.OutputExists, "The projection writer did not create the generated source.");
    }
}

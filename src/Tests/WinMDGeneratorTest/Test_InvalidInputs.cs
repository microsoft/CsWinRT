// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// End-to-end tests for the WinMD generator's handling of invalid invocations: malformed or missing
/// response files, bad arguments, missing or corrupt input assemblies, an unwritable output path, and
/// a missing debug-repro directory.
/// </summary>
/// <remarks>
/// Each test runs the actual <c>cswinrtwinmdgen</c> tool and asserts a non-zero exit code and the
/// expected <c>CSWINRTWINMDGEN</c> error in the output.
/// </remarks>
[TestClass]
public class Test_InvalidInputs
{
    [TestMethod]
    public void MissingResponseFile_IsReported()
    {
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGeneratorWithMissingResponseFile();

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0001");
    }

    [TestMethod]
    public void MalformedResponseFile_IsReported()
    {
        // A response file line must be a '<argument-name> <value>' pair; a line without a space is invalid
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static _ =>
        [
            "--assembly-version 1.0.0.0",
            "this-line-has-no-space",
        ]);

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0002");
    }

    [TestMethod]
    public void DuplicateArgument_IsReported()
    {
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static _ =>
        [
            "--assembly-version 1.0.0.0",
            "--assembly-version 2.0.0.0",
        ]);

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0002");
    }

    [TestMethod]
    public void MissingRequiredArgument_IsReported()
    {
        // The required '--output-winmd-path' argument is omitted
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static _ =>
        [
            "--input-assembly-path input.dll",
            "--reference-assembly-paths input.dll",
            "--assembly-version 1.0.0.0",
            "--use-windows-ui-xaml-projections False",
        ]);

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0003");
    }

    [TestMethod]
    public void InvalidBooleanArgument_IsReported()
    {
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static _ =>
        [
            "--input-assembly-path input.dll",
            "--reference-assembly-paths input.dll",
            "--output-winmd-path output.winmd",
            "--assembly-version 1.0.0.0",
            "--use-windows-ui-xaml-projections not-a-boolean",
        ]);

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0003");
    }

    [TestMethod]
    public void MissingInputAssembly_IsReported()
    {
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static temporaryDirectory =>
        {
            string missingInput = Path.Combine(temporaryDirectory, "DoesNotExist.dll");

            return
            [
                $"--input-assembly-path {missingInput}",
                $"--reference-assembly-paths {missingInput}",
                $"--output-winmd-path {Path.Combine(temporaryDirectory, "out.winmd")}",
                "--assembly-version 1.0.0.0",
                "--use-windows-ui-xaml-projections False",
            ];
        });

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0004");
    }

    [TestMethod]
    public void InvalidInputAssembly_IsReported()
    {
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static temporaryDirectory =>
        {
            string invalidInput = Path.Combine(temporaryDirectory, "NotAPortableExecutable.dll");

            File.WriteAllText(invalidInput, "This is not a valid PE image.");

            return
            [
                $"--input-assembly-path {invalidInput}",
                $"--reference-assembly-paths {invalidInput}",
                $"--output-winmd-path {Path.Combine(temporaryDirectory, "out.winmd")}",
                "--assembly-version 1.0.0.0",
                "--use-windows-ui-xaml-projections False",
            ];
        });

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0004");
    }

    [TestMethod]
    public void UnwritableOutputPath_IsReported()
    {
        // The output path points at an existing directory, so the '.winmd' cannot be written
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static temporaryDirectory =>
        {
            string inputAssemblyPath = WinMDGeneratorTestHelper.CompileComponent("""
                public interface IComponent
                {
                    int Method(int value);
                }
                """, temporaryDirectory);

            return
            [
                $"--input-assembly-path {inputAssemblyPath}",
                $"--reference-assembly-paths {inputAssemblyPath}",
                $"--output-winmd-path {temporaryDirectory}",
                "--assembly-version 1.0.0.0",
                "--use-windows-ui-xaml-projections False",
            ];
        });

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0006");
    }

    [TestMethod]
    public void MissingDebugReproDirectory_IsReported()
    {
        (int exitCode, string output) = WinMDGeneratorTestHelper.RunGenerator(static temporaryDirectory =>
        {
            string inputAssemblyPath = WinMDGeneratorTestHelper.CompileComponent("""
                public interface IComponent
                {
                    int Method(int value);
                }
                """, temporaryDirectory);

            string missingDebugReproDirectory = Path.Combine(temporaryDirectory, "does-not-exist");

            return
            [
                $"--input-assembly-path {inputAssemblyPath}",
                $"--reference-assembly-paths {inputAssemblyPath}",
                $"--output-winmd-path {Path.Combine(temporaryDirectory, "out.winmd")}",
                "--assembly-version 1.0.0.0",
                "--use-windows-ui-xaml-projections False",
                $"--debug-repro-directory {missingDebugReproDirectory}",
            ];
        });

        Assert.AreNotEqual(0, exitCode, output);
        StringAssert.Contains(output, "CSWINRTWINMDGEN0008");
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// End-to-end tests for the WinMD generator's debug repro support.
/// </summary>
/// <remarks>
/// These tests run the actual <c>cswinrtwinmdgen</c> tool with <c>--debug-repro-directory</c>, and then
/// replay the resulting archive. They cover the shared packing logic in <c>WinRT.Generator.Core</c>, which
/// every CsWinRT build tool uses, so a regression here would break debug repros for all of them.
/// </remarks>
[TestClass]
public class Test_DebugRepro
{
    [TestMethod]
    public void DebugRepro_IsSavedAndCanBeReplayed()
    {
        WinMDGeneratorRunner.AssertDebugReproRoundTrips(static (temporaryDirectory, debugReproDirectory) =>
        {
            string inputAssemblyPath = WinMDGeneratorRunner.CompileComponent("""
                public interface IComponent
                {
                    int Method(int value);
                }
                """, temporaryDirectory);

            return $"""
                --input-assembly-path {inputAssemblyPath}
                --reference-assembly-paths {inputAssemblyPath}
                --output-winmd-path {Path.Combine(temporaryDirectory, "TestInput.winmd")}
                --assembly-version 1.0.0.0
                --use-windows-ui-xaml-projections False
                --debug-repro-directory {debugReproDirectory}
                """;
        });
    }

    [TestMethod]
    public void DebugRepro_WithRepeatedInputPaths_IsSavedAndCanBeReplayed()
    {
        // MSBuild item lists are not deduplicated, and several targets contribute to the same reference
        // sets (e.g. '@(ReferencePathWithRefAssemblies)'), so the same path can reach the generators more
        // than once. Packing a debug repro must tolerate that instead of failing the whole build.
        WinMDGeneratorRunner.AssertDebugReproRoundTrips(static (temporaryDirectory, debugReproDirectory) =>
        {
            string inputAssemblyPath = WinMDGeneratorRunner.CompileComponent("""
                public interface IComponent
                {
                    int Method(int value);
                }
                """, temporaryDirectory);

            return $"""
                --input-assembly-path {inputAssemblyPath}
                --reference-assembly-paths {inputAssemblyPath},{inputAssemblyPath}
                --output-winmd-path {Path.Combine(temporaryDirectory, "TestInput.winmd")}
                --assembly-version 1.0.0.0
                --use-windows-ui-xaml-projections False
                --debug-repro-directory {debugReproDirectory}
                """;
        });
    }

    [TestMethod]
    public void DebugRepro_WithRepeatedReadOnlyInputPaths_IsSavedAndCanBeReplayed()
    {
        // Input files can be read-only (e.g. in enlistment-style builds), and 'File.Copy' propagates that
        // attribute to the staged copies. Staging the same file twice would then fail on the read-only
        // destination, and leaving the copies read-only would make the staging directory undeletable.
        WinMDGeneratorRunner.AssertDebugReproRoundTrips(static (temporaryDirectory, debugReproDirectory) =>
        {
            string inputAssemblyPath = WinMDGeneratorRunner.CompileComponent("""
                public interface IComponent
                {
                    int Method(int value);
                }
                """, temporaryDirectory);

            File.SetAttributes(inputAssemblyPath, File.GetAttributes(inputAssemblyPath) | FileAttributes.ReadOnly);

            return $"""
                --input-assembly-path {inputAssemblyPath}
                --reference-assembly-paths {inputAssemblyPath},{inputAssemblyPath}
                --output-winmd-path {Path.Combine(temporaryDirectory, "TestInput.winmd")}
                --assembly-version 1.0.0.0
                --use-windows-ui-xaml-projections False
                --debug-repro-directory {debugReproDirectory}
                """;
        });
    }
}

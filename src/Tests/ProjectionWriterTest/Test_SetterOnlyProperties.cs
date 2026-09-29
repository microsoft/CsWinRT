// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Covers projecting a property that declares only a setter in metadata, where the getter lives on a
/// different version of the same interface.
/// </summary>
/// <remarks>
/// The projected interface exposes both accessors, so the explicit implementation forwards its getter
/// to the interface declaring it. Versioned interfaces are flat, so that is not a base interface.
/// </remarks>
[TestClass]
public class Test_SetterOnlyProperties
{
    /// <summary>
    /// The projected interface exposes both accessors, matching what CsWinRT 2.x emitted.
    /// </summary>
    [TestMethod]
    public void SetterOnlyProperty_StillProjectsBothAccessors()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            string source = Generate(inputPath, outputFolder);

            StringAssert.Contains(source, "int Current { get; set; }");
        });
    }

    /// <summary>
    /// The regression this guards: the search for the declaring interface only walked base interfaces, so
    /// no getter was emitted and the explicit implementation was left missing an accessor it had declared.
    /// </summary>
    [TestMethod]
    public void SetterOnlyProperty_ForwardsItsGetterToTheDeclaringInterface()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            string source = Generate(inputPath, outputFolder);

            StringAssert.Contains(source, "get { return ((global::Contoso.IWidgetStatics)(WindowsRuntimeObject)this).Current; }");
        });
    }

    /// <summary>
    /// The getter-only property on the interface that declares it is unaffected.
    /// </summary>
    [TestMethod]
    public void GetterOnlyProperty_IsStillProjectedAsAGetter()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            string source = Generate(inputPath, outputFolder);

            StringAssert.Contains(source, "int Current { get; }");
        });
    }

    /// <summary>
    /// When no interface declares the getter there is nothing to forward to, so the property is set-only.
    /// </summary>
    [TestMethod]
    public void SetterOnlyProperty_WithNoDeclaringInterface_IsProjectedSetOnly()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            string source = Generate(inputPath, outputFolder);

            StringAssert.Contains(source, "int Detached { set; }");

            Assert.IsFalse(
                source.Contains("Detached { get; set; }", StringComparison.Ordinal),
                "promised a getter that no interface declares");
        });
    }

    private static string Generate(string inputPath, string outputFolder)
    {
        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [inputPath],
            OutputFolder = outputFolder,
            Include = ["Contoso"],

            // Emits the explicit interface implementations the forwarding getter lives in.
            IdicExclusiveTo = true
        });

        return string.Join(
            Environment.NewLine,
            Directory.GetFiles(outputFolder, "*.cs").Select(File.ReadAllText));
    }

    private static void WithMetadata(Action<string, string> action)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionSetterOnlyTest_").FullName;

        try
        {
            action(SetterOnlyPropertyMetadata.Create(directory), Path.Combine(directory, "Generated"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

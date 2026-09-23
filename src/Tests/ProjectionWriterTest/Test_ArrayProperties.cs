// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Covers marshalling an array into a property setter on the CCW (<c>Do_Abi</c>) side.
/// </summary>
[TestClass]
public class Test_ArrayProperties
{
    /// <summary>
    /// An array valued property projects as <c>T[]</c>, unlike an array <i>parameter</i>, which projects as a
    /// span. The setter cannot pass the span local along, and must not: it is backed by an inline array or a
    /// pooled buffer that is returned when the method exits.
    /// </summary>
    [TestMethod]
    public void ArrayPropertySetter_CopiesOutOfTheSpanLocal()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            string source = Generate(inputPath, outputFolder);

            foreach (string name in (string[])["Names", "Counts"])
            {
                StringAssert.Contains(source, $".{name} = __value.ToArray();");
            }
        });
    }

    /// <summary>
    /// The regression this guards: the setter used to emit the raw ABI parameter, which is a <c>void*</c>, so
    /// the generated projection did not compile at all ("cannot implicitly convert type 'void*' to 'T[]'").
    /// </summary>
    [TestMethod]
    public void ArrayPropertySetter_DoesNotAssignTheAbiParameter()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            string source = Generate(inputPath, outputFolder);

            foreach (string name in (string[])["Names", "Counts"])
            {
                Assert.IsFalse(
                    source.Contains($".{name} = value;", StringComparison.Ordinal),
                    $"'{name}' setter assigned the raw ABI parameter instead of the marshalled array");
            }
        });
    }

    private static string Generate(string inputPath, string outputFolder)
    {
        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [inputPath],
            OutputFolder = outputFolder,
            Include = ["Contoso"],

            // The CCW handlers the setter lives in are only emitted for an exclusive-to interface that
            // something can implement, which is what this turns on.
            PublicExclusiveTo = true
        });

        return string.Join(
            Environment.NewLine,
            Directory.GetFiles(outputFolder, "*.cs").Select(File.ReadAllText));
    }

    private static void WithMetadata(Action<string, string> action)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionArrayPropertyTest_").FullName;

        try
        {
            action(ArrayPropertyMetadata.Create(directory), Path.Combine(directory, "Generated"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

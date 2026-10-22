// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public sealed class Test_MetadataCatalogAdditions
{
    [TestMethod]
    [DataRow("Microsoft.UI.Xaml", false, false)]
    [DataRow("Windows.UI.Xaml", false, false)]
    [DataRow("Microsoft.UI.Xaml", true, false)]
    [DataRow("Windows.UI.Xaml", true, false)]
    [DataRow("Microsoft.UI.Xaml", false, true)]
    [DataRow("Windows.UI.Xaml", false, true)]
    public void HandAuthoredStructs_UseTheSameMetadataCatalog(string rootNamespace, bool referenceProjection, bool excludeAdditions)
    {
        string directory = Directory.CreateTempSubdirectory("MetadataCatalogAdditions_").FullName;
        try
        {
            string input = MetadataCatalogAdditionsMetadata.Create(directory, rootNamespace);
            string[] names =
            [
                rootNamespace + ".CornerRadius",
                rootNamespace + ".Duration",
                rootNamespace + ".GridLength",
                rootNamespace + ".Media.Animation.KeyTime",
                rootNamespace + ".Media.Animation.RepeatBehavior",
                rootNamespace + ".Media.Media3D.Matrix3D"
            ];
            string output = Path.Combine(directory, "sources");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [input],
                OutputFolder = output,
                Include = [rootNamespace],
                ReferenceProjection = referenceProjection,
                AdditionExclude = excludeAdditions ? [rootNamespace] : []
            });
            string path = Path.Combine(output, "WindowsRuntimeMetadataTypes.cs");
            if (referenceProjection || excludeAdditions)
            {
                Assert.IsFalse(File.Exists(path));
            }
            else
            {
                string catalog = File.ReadAllText(path);
                foreach (string name in names)
                {
                    Assert.Contains("typeof(global::" + name + ")", catalog);
                }
                Assert.Contains("\"Mapped.Xaml\"", catalog);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
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
            ModuleDefinition module = new("Mapped.Xaml.winmd") { RuntimeVersion = "WindowsRuntime 1.4" };
            _ = new AssemblyDefinition("Mapped.Xaml", new Version(255, 255, 255, 255))
            {
                Modules = { module },
                Attributes = AssemblyAttributes.ContentWindowsRuntime
            };
            string[] names =
            [
                rootNamespace + ".CornerRadius",
                rootNamespace + ".Duration",
                rootNamespace + ".GridLength",
                rootNamespace + ".Media.Animation.KeyTime",
                rootNamespace + ".Media.Animation.RepeatBehavior",
                rootNamespace + ".Media.Media3D.Matrix3D"
            ];
            foreach (string name in names)
            {
                int separator = name.LastIndexOf('.');
                TypeDefinition type = new(name[..separator], name[(separator + 1)..],
                    TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
                    new TypeReference(module, module.CorLibTypeFactory.CorLibScope, "System", "ValueType"));
                type.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.CorLibTypeFactory.Double));
                module.TopLevelTypes.Add(type);
            }
            string input = Path.Combine(directory, "Mapped.Xaml.winmd");
            module.Write(input);
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

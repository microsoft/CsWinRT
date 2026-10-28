// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

/// <summary>
/// Creates metadata for the XAML structs supplied by handwritten namespace additions.
/// </summary>
internal static class MetadataCatalogAdditionsMetadata
{
    public static string Create(string directory, string rootNamespace)
    {
        ModuleDefinition module = new("Mapped.Xaml.winmd") { RuntimeVersion = "WindowsRuntime 1.4" };
        _ = new AssemblyDefinition("Mapped.Xaml", new Version(255, 255, 255, 255))
        {
            Modules = { module },
            Attributes = AssemblyAttributes.ContentWindowsRuntime
        };

        foreach (string name in new[]
        {
            "CornerRadius", "Duration", "GridLength",
            "Media.Animation.KeyTime", "Media.Animation.RepeatBehavior", "Media.Media3D.Matrix3D"
        })
        {
            string fullName = rootNamespace + "." + name;
            int separator = fullName.LastIndexOf('.');
            TypeDefinition type = new(fullName[..separator], fullName[(separator + 1)..],
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
                new TypeReference(module, module.CorLibTypeFactory.CorLibScope, "System", "ValueType"))
            {
                Fields = { new FieldDefinition("Value", FieldAttributes.Public, module.CorLibTypeFactory.Double) }
            };
            module.TopLevelTypes.Add(type);
        }

        string path = Path.Combine(directory, "Mapped.Xaml.winmd");
        module.Write(path);
        return path;
    }
}

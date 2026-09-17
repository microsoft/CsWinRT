// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public class Test_TypeFiltering
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ExactTypeIncludes_OmitCollidingTypes(bool referenceProjection)
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [inputPath],
                OutputFolder = outputFolder,
                IncludeTypes = ["Contoso.User"],
                ReferenceProjection = referenceProjection
            });

            AssertSelectedTypes(outputFolder);
        });
    }

    [TestMethod]
    public void ReferenceProjection_PrefixExclusionsOmitUnsupportedTypes()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [inputPath],
                OutputFolder = outputFolder,
                Include = ["Contoso"],
                Exclude = ["Contoso.User2", "Contoso.IUser2", "Contoso.UserProfile"],
                ReferenceProjection = true
            });

            AssertSelectedTypes(outputFolder);
        });
    }

    [TestMethod]
    public void ExactTypeIncludes_StillEmitExclusiveFactoryInfrastructure()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [inputPath],
                OutputFolder = outputFolder,
                IncludeTypes = ["Contoso.User"]
            });

            string source = File.ReadAllText(Path.Combine(outputFolder, "Contoso.cs"));
            string iids = File.ReadAllText(Path.Combine(outputFolder, "GeneratedInterfaceIIDs.cs"));

            StringAssert.Contains(source, "GetValue");
            StringAssert.Contains(source, "GetStaticValue");
            StringAssert.Contains(source, "IUserStaticsMethods");
            StringAssert.Contains(iids, "IUserStatics");
            StringAssert.Contains(iids, "IUser");
        });
    }

    [TestMethod]
    public void ComponentProjection_RecordsOnlyExportedAuthoredTypes()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [inputPath],
                OutputFolder = outputFolder,
                Include = ["Contoso"],
                Exclude = ["Contoso.User2", "Contoso.IUser2", "Contoso.UserProfile"],
                Component = true
            });

            string metadata = File.ReadAllText(Path.Combine(outputFolder, "WindowsRuntimeMetadataTypes.cs"));
            string[] types = CSharpSyntaxTree.ParseText(metadata).GetRoot()
                .DescendantNodes().OfType<TypeOfExpressionSyntax>()
                .Select(type => type.Type.ToString().Replace("global::", "", StringComparison.Ordinal).Replace("@", "", StringComparison.Ordinal))
                .ToArray();

            CollectionAssert.Contains(types, "Contoso.User");
            CollectionAssert.Contains(types, "ABI.Impl.Contoso.IUser");
            CollectionAssert.DoesNotContain(types, "Contoso.IUser");
            CollectionAssert.DoesNotContain(types, "Contoso.IUserStatics");
            Assert.IsFalse(types.Any(type => type.Contains("User2", StringComparison.Ordinal)));
            Assert.IsFalse(types.Any(type => type.Contains("UserProfile", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    public void ComponentReferenceProjection_DoesNotRecordInteropMetadata()
    {
        WithMetadata((inputPath, outputFolder) =>
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [inputPath],
                OutputFolder = outputFolder,
                IncludeTypes = ["Contoso.User"],
                Component = true,
                ReferenceProjection = true
            });

            Assert.IsFalse(File.Exists(Path.Combine(outputFolder, "WindowsRuntimeMetadataTypes.cs")));
        });
    }

    private static void AssertSelectedTypes(string outputFolder)
    {
        string source = File.ReadAllText(Path.Combine(outputFolder, "Contoso.cs"));

        StringAssert.Contains(source, "class User");
        Assert.IsFalse(source.Contains("User2", StringComparison.Ordinal));
        Assert.IsFalse(File.Exists(Path.Combine(outputFolder, "Contoso.UserProfile.cs")));

        string allSources = string.Join(Environment.NewLine, Directory.GetFiles(outputFolder, "*.cs").Select(File.ReadAllText));

        Assert.IsFalse(allSources.Contains("UserSetupManager", StringComparison.Ordinal));
        Assert.IsFalse(allSources.Contains("Windows.Foundation.IPropertyValue", StringComparison.Ordinal));
    }

    private static void WithMetadata(Action<string, string> action)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionTypeFilteringTest_").FullName;

        try
        {
            action(TypeFilteringMetadata.Create(directory), Path.Combine(directory, "Generated"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

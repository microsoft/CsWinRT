// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public class Test_ExclusiveToInterfaces
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void PublicExclusiveInterface_EmitsStandaloneAbi(bool fastAbi, bool includeOwner)
    {
        WithMetadata(fastAbi, (directory, metadataPath) =>
        {
            string output = Path.Combine(directory, "implementation");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = output,
                IncludeTypes = includeOwner
                    ? ["Contoso.Widget", "Contoso.IWidget", "Contoso.IWidget2", "Contoso.IWidget3", "Contoso.ChangedHandler"]
                    : ["Contoso.IWidget2", "Contoso.IWidget3", "Contoso.ChangedHandler"],
                PublicExclusiveToTypes = ["Contoso.IWidget2"],
                IdicExclusiveToTypes = ["Contoso.IWidget2"]
            });

            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
            ClassDeclarationSyntax methods = GetMethods(source, "IWidget2");

            StringAssert.Contains(source, "public interface IWidget2");
            StringAssert.Contains(source, "[DynamicInterfaceCastableImplementation]");
            StringAssert.Contains(source, "source: typeof(global::Contoso.IWidget2)");
            StringAssert.Contains(source, ExclusiveToMetadata.SecondaryInterfaceIid.ToUpperInvariant());
            AssertSlot(methods, "GetValue", 6);
            StringAssert.Contains(methods.ToString(), "IWidget2Changed");
            Assert.IsFalse(source.Contains("public interface IWidget3", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("file interface IWidget3", StringComparison.Ordinal));
            Assert.AreEqual(includeOwner, source.Contains("class Widget", StringComparison.Ordinal));

            if (fastAbi && includeOwner)
            {
                AssertSlot(GetMethods(source, "IWidget"), "GetValue", 9);
                Assert.IsFalse(source.Contains("class IWidget3Methods", StringComparison.Ordinal));
            }

            _ = Compile(output, Path.Combine(directory, "implementation.dll"), referenceProjection: false);
        });
    }

    [TestMethod]
    public void OrdinaryFastAbiInterfaces_KeepMergedDefaultDispatch()
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string output = Path.Combine(directory, "implementation");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = output,
                Include = ["Contoso"]
            });

            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
            ClassDeclarationSyntax methods = GetMethods(source, "IWidget");
            AssertSlot(methods, "GetDefaultValue", 6);
            AssertSlot(methods, "GetValue", 9);
            AssertSlot(methods, "GetOtherValue", 12);
            Assert.IsFalse(methods.ToString().Contains("IWidgetChanged", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("class IWidget2Methods", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("class IWidget3Methods", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("public interface IWidget", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("[DynamicInterfaceCastableImplementation]", StringComparison.Ordinal));
            _ = Compile(output, Path.Combine(directory, "implementation.dll"), referenceProjection: false);
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PublicFastAbiDefaultInterface_KeepsEventHelpers(bool includeOwner)
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string output = Path.Combine(directory, "implementation");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = output,
                IncludeTypes = includeOwner
                    ? ["Contoso.Widget", "Contoso.IWidget", "Contoso.ChangedHandler"]
                    : ["Contoso.IWidget", "Contoso.ChangedHandler"],
                PublicExclusiveToTypes = ["Contoso.IWidget"],
                IdicExclusiveToTypes = ["Contoso.IWidget"]
            });

            ClassDeclarationSyntax methods = GetMethods(File.ReadAllText(Path.Combine(output, "Contoso.cs")), "IWidget");
            AssertSlot(methods, "GetDefaultValue", 6);
            StringAssert.Contains(methods.ToString(), "IWidgetChanged");
            Assert.AreEqual(includeOwner, methods.Members.OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.ValueText == "GetValue"));
            _ = Compile(output, Path.Combine(directory, "implementation.dll"), referenceProjection: false);
        });
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void GlobalExclusiveOptions_RemainIndependent(bool publicExclusiveTo, bool idicExclusiveTo)
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string output = Path.Combine(directory, "implementation");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = output,
                IncludeTypes = ["Contoso.IWidget2", "Contoso.ChangedHandler"],
                PublicExclusiveTo = publicExclusiveTo,
                IdicExclusiveTo = idicExclusiveTo
            });

            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
            Assert.AreEqual(publicExclusiveTo, source.Contains("public interface IWidget2", StringComparison.Ordinal));
            Assert.AreEqual(idicExclusiveTo, source.Contains("file interface IWidget2", StringComparison.Ordinal));
            Assert.AreEqual(idicExclusiveTo, source.Contains("source: typeof(global::Contoso.IWidget2)", StringComparison.Ordinal));
            Assert.AreEqual(publicExclusiveTo || idicExclusiveTo, source.Contains("class IWidget2Methods", StringComparison.Ordinal));
            Assert.AreEqual(publicExclusiveTo, source.Contains("class IWidget2Marshaller", StringComparison.Ordinal));
            _ = Compile(output, Path.Combine(directory, "implementation.dll"), referenceProjection: false);
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ReferenceProjection_PublicInterfaceRetainsImplementation(bool fastAbi)
    {
        WithMetadata(fastAbi, (directory, metadataPath) =>
        {
            string referenceSources = Path.Combine(directory, "reference");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = referenceSources,
                IncludeTypes = ["Contoso.IWidget2", "Contoso.ChangedHandler"],
                PublicExclusiveTo = true,
                IdicExclusiveTo = true,
                ReferenceProjection = true
            });
            string reference = Compile(referenceSources, Path.Combine(directory, "Supplemental.dll"), referenceProjection: true);
            string output = Directory.CreateDirectory(Path.Combine(directory, "merged")).FullName;
            string responseFile = Path.Combine(directory, "projection.rsp");
            string[] references = [reference, .. ProjectionWriterRunner.GetRuntimeReferencePaths()];
            File.WriteAllText(responseFile, $"""
                --reference-assembly-paths {string.Join(",", references)}
                --generated-assembly-directory {output}
                --winmd-paths {metadataPath}
                --windows-metadata {metadataPath}
                --target-framework net10.0
                --max-degrees-of-parallelism 1
                """);

            string tool = ProjectionWriterRunner.GetRequiredFilePath("ProjectionGeneratorAssemblyPath");
            (int exitCode, string log) = ProjectionWriterRunner.Run(tool, $"@{responseFile}");
            Assert.AreEqual(0, exitCode, log);

            ModuleDefinition module = ModuleDefinition.FromFile(Path.Combine(output, "WinRT.Projection.dll"));
            TypeDefinition iface = module.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget2");
            Assert.IsTrue(iface.IsPublic);
            Assert.IsTrue(iface.IsInterface);
            Assert.IsFalse(module.TopLevelTypes.Any(type => type.FullName is "Contoso.Widget" or "Contoso.IWidget3"));
            Assert.AreEqual(new Guid(ExclusiveToMetadata.SecondaryInterfaceIid),
                new Guid(iface.CustomAttributes.Single(attribute => attribute.Constructor?.DeclaringType?.Name == "GuidAttribute")
                    .Signature!.FixedArguments[0].Element!.ToString()!));
            Assert.IsTrue(module.TopLevelTypes.Any(type => type.CustomAttributes.Any(
                attribute => attribute.Constructor?.DeclaringType?.Name == "DynamicInterfaceCastableImplementationAttribute")));
            Assert.IsTrue(module.Assembly!.CustomAttributes.Any(attribute =>
                attribute.Constructor?.DeclaringType?.FullName.Contains("DynamicInterfaceCastableImplementationTypeMapGroup", StringComparison.Ordinal) == true));
            MethodDefinition method = module.TopLevelTypes.Single(type => type.FullName == "ABI.Contoso.IWidget2Methods")
                .Methods.Single(method => method.Name == "GetValue");
            Assert.IsTrue(method.CilMethodBody!.Instructions.Any(instruction => instruction.OpCode.Code == CilCode.Calli));

            // Removing the only projection reference must also remove the previous merged output.
            File.WriteAllText(responseFile, File.ReadAllText(responseFile).Replace($"{reference},", "", StringComparison.Ordinal));
            (exitCode, log) = ProjectionWriterRunner.Run(tool, $"@{responseFile}");
            Assert.AreEqual(0, exitCode, log);
            Assert.IsFalse(File.Exists(Path.Combine(output, "WinRT.Projection.dll")));
        });
    }

    private static ClassDeclarationSyntax GetMethods(string source, string interfaceName)
    {
        return CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14)).GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == $"{interfaceName}Methods");
    }

    private static void AssertSlot(ClassDeclarationSyntax methods, string methodName, int slot)
    {
        MethodDeclarationSyntax method = methods.Members.OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == methodName);
        Assert.IsTrue(method.DescendantNodes().OfType<ElementAccessExpressionSyntax>()
            .Any(access => access.ArgumentList.Arguments.ToString() == slot.ToString()), method.ToString());
        StringAssert.Contains(method.ToString(), "delegate* unmanaged[MemberFunction]");
        StringAssert.Contains(method.ToString(), "ThrowExceptionForHR");
    }

    private static string Compile(string sourceDirectory, string assemblyPath, bool referenceProjection)
    {
        return ProjectionWriterRunner.CompileSources(
            Directory.GetFiles(sourceDirectory, "*.cs").Select(File.ReadAllText),
            assemblyPath,
            referenceProjection);
    }

    private static void WithMetadata(bool fastAbi, Action<string, string> action)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionExclusiveToTest_").FullName;
        try
        {
            action(directory, ExclusiveToMetadata.Create(directory, fastAbi));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

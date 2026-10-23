// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public sealed class Test_IidTypeMap
{
    [TestMethod]
    [DataRow("WinRT.Sdk.Projection")]
    [DataRow("WinRT.Sdk.Xaml.Projection")]
    [DataRow("WinRT.Projection")]
    [DataRow("#PrivateProjection")]
    public void ImplementationProjection_EmitsConditionalGuidOnlyProviders(string assemblyName)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionIidMap_").FullName;
        try
        {
            string output = Path.Combine(directory, "sources");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [IidProjectionMetadata.Create(directory)],
                OutputFolder = output,
                OutputAssemblyName = assemblyName,
                Include = ["Contoso"],
                Exclude = ["Contoso.UserProfile", "Contoso.TypeValue"]
            });
            string assembly = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText), Path.Combine(directory, assemblyName + ".dll"));
            ModuleDefinition module = ModuleDefinition.FromFile(assembly);
            CustomAttribute[] entries = GetEntries(module);
            string scope = assemblyName.StartsWith('#') ? "#" + assemblyName : assemblyName;
            string prefix = "[" + scope + "]Contoso.";

            Assert.AreEqual(new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622501"), GetIid(module, prefix + "User"));
            Assert.AreEqual(GetIid(module, prefix + "IUser"), GetIid(module, prefix + "User"));
            Assert.AreEqual(new Guid("60A8E0C5-7371-4B96-AB04-B2CCFAFCE870"), GetIid(module, prefix + "Handler"));
            Assert.IsTrue(entries.Any(entry => Key(entry) == "[#System]System.Nullable`1[" + prefix + "Value]"));
            Assert.IsTrue(entries.Any(entry => Key(entry) == "[#System]System.Nullable`1[" + prefix + "Choice]"));
            Assert.AreEqual(new Guid("00B38F98-FFB9-5CA8-9E82-918B885A210B"),
                GetIid(module, "[#System]System.Nullable`1[" + prefix + "Value]"));
            Assert.AreEqual(new Guid("46CA19F4-8075-53B9-8FDE-91279B7E302D"),
                GetIid(module, "[#System]System.Nullable`1[" + prefix + "Choice]"));
            Assert.AreEqual(new Guid("8D443AC5-3D70-513B-82A2-AE55A7332045"),
                GetIid(module, "[#System]System.Nullable`1[" + prefix + "Nested]"));
            Assert.AreEqual(new Guid("EA298EC0-4E3D-52B8-804D-BDCE7E34A6E8"),
                GetIid(module, "[#System]System.Nullable`1[" + prefix + "Flags]"));
            foreach (string absent in new[] { "Value", "Choice", "Statics", "MetadataAttribute", "Contract", "NoIdentity" })
            {
                Assert.IsFalse(entries.Any(entry => Key(entry) == prefix + absent));
            }
            foreach (CustomAttribute entry in entries)
            {
                Assert.HasCount(3, entry.Signature!.FixedArguments);
                TypeSignature providerSignature = (TypeSignature)entry.Signature.FixedArguments[1].Element!;
                TypeDefinition provider = module.TopLevelTypes.Single(type => type.FullName == providerSignature.FullName);
                Assert.IsTrue(provider.IsAbstract && provider.IsSealed);
                Assert.HasCount(1, provider.CustomAttributes);
                Assert.AreEqual("System.Runtime.InteropServices.GuidAttribute", provider.CustomAttributes[0].Constructor!.DeclaringType!.FullName);
                Assert.IsEmpty(provider.Fields);
                Assert.IsEmpty(provider.Methods);
                Assert.IsTrue(entry.Signature.FixedArguments[2].Element is TypeDefOrRefSignature);
            }
            foreach (string valueType in new[] { "Value", "Choice" })
            {
                CustomAttribute entry = entries.Single(entry => Key(entry) == "[#System]System.Nullable`1[" + prefix + valueType + "]");
                Assert.AreEqual("Contoso." + valueType, ((TypeSignature)entry.Signature!.FixedArguments[2].Element!).FullName);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void SystemTypeSignature_UsesTheMappedWindowsRuntimeValue()
    {
        string directory = Directory.CreateTempSubdirectory("SystemTypeIid_").FullName;
        try
        {
            string output = Path.Combine(directory, "sources");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [IidProjectionMetadata.Create(directory)],
                OutputFolder = output,
                OutputAssemblyName = "Projection",
                IncludeTypes = ["Contoso.TypeValue"]
            });
            Assert.Contains("EE6CD58E-0E3C-517D-90D6-AF127C619B9C",
                File.ReadAllText(Path.Combine(output, "WindowsRuntimeIidTypeMap.cs")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SdkGenericDefaults_ResolveReferencedRuntimeClassInterfaces(bool useWinUI)
    {
        string source = ProjectionWriterRunner.GetSources(referenceProjection: false, useWinUI);
        string ns = useWinUI ? "Microsoft.UI.Xaml" : "Windows.UI.Xaml";

        Assert.Contains("[Projection]" + ns + ".DependencyObjectCollection", source);
        Assert.Contains("[Projection]" + ns + ".Controls.ItemCollection", source);
    }

    [TestMethod]
    public void ComponentProjection_UsesAuthoredAndGeneratedInterfaceIdentities()
    {
        string directory = Directory.CreateTempSubdirectory("ComponentIidMap_").FullName;
        try
        {
            string output = Path.Combine(directory, "sources");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [IidProjectionMetadata.Create(directory)],
                OutputFolder = output,
                OutputAssemblyName = "WinRT.Component",
                Component = true,
                ComponentAssemblyNames = ["Contoso"],
                IncludeTypes = ["Contoso.User", "Contoso.IUser", "Contoso.IStandalone", "Contoso.Value", "Contoso.Choice", "Contoso.Handler"]
            });
            SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(output, "WindowsRuntimeIidTypeMap.cs"))).GetRoot();
            string[] literals = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
                .Select(literal => literal.Token.ValueText).ToArray();

            Assert.Contains("[Contoso]Contoso.User", literals);
            Assert.Contains("[WinRT.Component]ABI.Impl.Contoso.IUser", literals, string.Join(Environment.NewLine, literals));
            Assert.DoesNotContain("[WinRT.Component]Contoso.User", literals);
            Assert.DoesNotContain("[Contoso]Contoso.IUser", literals);
            Assert.Contains("[Contoso]Contoso.IStandalone", literals);
            Assert.Contains("[Contoso]Contoso.Handler", literals);
            Assert.Contains("[#System]System.Nullable`1[[Contoso]Contoso.Value]", literals);
            Assert.Contains("[#System]System.Nullable`1[[Contoso]Contoso.Choice]", literals);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void ReferenceProjection_DoesNotEmitIidMaps()
    {
        string directory = Directory.CreateTempSubdirectory("ReferenceIidMap_").FullName;
        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [IidProjectionMetadata.Create(directory)],
                OutputFolder = Path.Combine(directory, "sources"),
                ReferenceProjection = true,
                Include = ["Contoso"],
                Exclude = ["Contoso.UserProfile"]
            });
            Assert.IsFalse(File.Exists(Path.Combine(directory, "sources", "WindowsRuntimeIidTypeMap.cs")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void MissingImplementationAssemblyName_ReportsDiagnostic(string? assemblyName)
    {
        Exception error = Assert.Throws<Exception>(() => ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = ["unused.winmd"],
            OutputFolder = "unused",
            OutputAssemblyName = assemblyName
        }));
        Assert.Contains("CSWINRTPROJECTIONGEN5025", error.ToString());
    }

    private static CustomAttribute[] GetEntries(ModuleDefinition module) => module.Assembly!.CustomAttributes.Where(attribute =>
        attribute.Constructor?.DeclaringType?.FullName == "System.Runtime.InteropServices.TypeMapAttribute`1<WindowsRuntime.InteropServices.WindowsRuntimeIidTypeMapGroup>").ToArray();

    private static string Key(CustomAttribute entry) => entry.Signature!.FixedArguments[0].Element!.ToString()!;

    private static Guid GetIid(ModuleDefinition module, string key)
    {
        CustomAttribute entry = GetEntries(module).Single(entry => Key(entry) == key);
        TypeSignature provider = (TypeSignature)entry.Signature!.FixedArguments[1].Element!;
        return Guid.Parse(module.TopLevelTypes.Single(type => type.FullName == provider.FullName)
            .CustomAttributes.Single().Signature!.FixedArguments[0].Element!.ToString()!);
    }
}

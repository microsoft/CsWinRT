// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public sealed class Test_NonGenericEnumeration
{
    private static readonly Lazy<NativeProjectionFixture> NativeFixture = new(() => CreateNativeFixture());

    [ClassCleanup]
    public static void Cleanup()
    {
        if (NativeFixture.IsValueCreated)
        {
            NativeFixture.Value.Dispose();
        }
    }

    [TestMethod]
    public void RequiredInterfaces_EmitOnlyUnambiguousNonBindableProviders()
    {
        using NativeProjectionFixture fixture = new();
        string metadataPath = NonGenericEnumerableMetadata.Create(fixture.DirectoryPath, "enumerable");
        string output = Path.Combine(fixture.DirectoryPath, "projection");
        GenerateProjection(metadataPath, output);
        string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
        SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();
        InterfaceDeclarationSyntax[] interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
            .Where(type => type.Modifiers.Any(SyntaxKind.FileKeyword)).ToArray();
        ClassDeclarationSyntax[] providers = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(type => type.BaseList?.Types.Any(baseType =>
                baseType.Type.ToString() == "global::WindowsRuntime.InteropServices.DynamicInterfaceCastableEnumerableAttribute") == true)
            .ToArray();

        foreach (string name in new[] { "IRequiredItems", "IDiamondItems" })
        {
            InterfaceDeclarationSyntax type = interfaces.Single(type => type.Identifier.ValueText == name);
            StringAssert.Contains(type.AttributeLists.ToString(), name + "EnumerableAttribute");
            ClassDeclarationSyntax provider = providers.Single(provider => provider.Identifier.ValueText == name + "EnumerableAttribute");
            Assert.IsTrue(provider.Modifiers.Any(SyntaxKind.SealedKeyword));
            Assert.IsNull(provider.TypeParameterList);
            MethodDeclarationSyntax method = provider.Members.OfType<MethodDeclarationSyntax>().Single();
            Assert.AreEqual("TryGetEnumerator", method.Identifier.ValueText);
            Assert.AreEqual("bool", method.ReturnType.ToString());
            ParameterSyntax enumerator = method.ParameterList.Parameters[1];
            Assert.IsTrue(enumerator.Modifiers.Any(SyntaxKind.OutKeyword));
            Assert.IsFalse(enumerator.Type is NullableTypeSyntax);
            Assert.IsEmpty(enumerator.AttributeLists, "Only the runtime declaration needs the success annotation.");
            StringAssert.Contains(provider.ToString(), "thisObject is global::System.Collections.Generic.IEnumerable<int> enumerable");
            StringAssert.Contains(provider.ToString(), "enumerator = enumerable.GetEnumerator();");
            StringAssert.Contains(provider.ToString(), "return true;");
            StringAssert.Contains(provider.ToString(), "return false;");
        }
        InterfaceDeclarationSyntax requiredMap = interfaces.Single(type => type.Identifier.ValueText == "IRequiredDictionary");
        StringAssert.Contains(requiredMap.AttributeLists.ToString(), "IRequiredDictionaryEnumerableAttribute");
        ClassDeclarationSyntax mapProvider = providers.Single(provider => provider.Identifier.ValueText == "IRequiredDictionaryEnumerableAttribute");
        StringAssert.Contains(mapProvider.ToString(),
            "IEnumerable<global::System.Collections.Generic.KeyValuePair<int, string>>");
        Assert.IsFalse(interfaces.SelectMany(type => type.AttributeLists.SelectMany(list => list.Attributes))
            .Any(attribute => attribute.Name.ToString().Contains("DynamicInterfaceCastableEnumerable<", StringComparison.Ordinal)),
            "Enumeration markers must not be constructed generic attribute types.");
        foreach (string name in new[] { "IAmbiguousItems", "IBindableItems" })
        {
            InterfaceDeclarationSyntax type = interfaces.Single(type => type.Identifier.ValueText == name);
            Assert.IsFalse(providers.Any(provider => provider.Identifier.ValueText == name + "EnumerableAttribute"));
            Assert.IsFalse(type.AttributeLists.ToString().Contains(name + "EnumerableAttribute", StringComparison.Ordinal));
        }

        string reference = Path.Combine(fixture.DirectoryPath, "reference-projection");
        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [Path.Combine(fixture.DirectoryPath, "enumerable.winmd")],
            OutputFolder = reference,
            Include = ["Contoso"],
            ReferenceProjection = true
        });
        Assert.IsFalse(Directory.GetFiles(reference, "*.cs").Any(path =>
            File.ReadAllText(path).Contains("DynamicInterfaceCastableEnumerable", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CombinedBindableRequirement_EmitsNoTypedProvider()
    {
        using NativeProjectionFixture fixture = new();
        string metadataPath = NonGenericEnumerableMetadata.Create(fixture.DirectoryPath, "enumerable", includeBindableRequiredItems: true);
        string output = Path.Combine(fixture.DirectoryPath, "combined-bindable");
        GenerateProjection(metadataPath, output);
        // This checks annotation policy only; combined bindable/generic member deduplication is a separate writer issue.
        InterfaceDeclarationSyntax implementation = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(output, "Contoso.cs")))
            .GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == "IBindableItems" && type.Modifiers.Any(SyntaxKind.FileKeyword));
        Assert.IsFalse(implementation.AttributeLists.ToString().Contains("IBindableItemsEnumerableAttribute", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("list")]
    [DataRow("read-only-list")]
    [DataRow("strings")]
    [DataRow("pairs")]
    [DataRow("dictionary")]
    [DataRow("read-only-dictionary")]
    [DataRow("observable-vector")]
    [DataRow("observable-map")]
    [DataRow("required-vector")]
    [DataRow("required-map")]
    [DataRow("bindable")]
    [DataRow("first-failure")]
    [DataRow("missing-required-iterable")]
    [DataRow("concurrent")]
    [DataRow("negative-cache")]
    [DataRow("mixed-elements")]
    [DataRow("generic-adapter")]
    [DataRow("object-adapter")]
    [DataRow("bindable-adapter")]
    public void NativeCollections_UseGeneratedTypedEnumeration(string scenario)
    {
        NativeProjectionFixture fixture = NativeFixture.Value;
        (int exitCode, string log) = ProjectionWriterRunner.Run(
            Path.Combine(fixture.DirectoryPath, "NonGenericEnumerableTests.dll"), scenario);
        Assert.AreEqual(0, exitCode, log);
        StringAssert.Contains(log, "Generated non-generic enumeration passed.");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [TestCategory("NativeAOT")]
#if !CSWINRT_GENERIC_MARSHALLERS_AOT
    [Ignore("Run in x64 Release, or build with CSWINRT_TEST_GENERIC_MARSHALLERS_AOT=1, to run the generated native collection consumer with Native AOT.")]
#endif
    public void NativeCollections_NativeAot(bool fallbackOnly)
    {
        // Discover from the same conditional consumer that will be published.
        using NativeProjectionFixture fixture = CreateNativeFixture(fallbackOnly);
        string directory = Directory.CreateDirectory(Path.Combine(fixture.DirectoryPath, "aot")).FullName;
        File.WriteAllText(Path.Combine(directory, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(directory, "Directory.Build.targets"), "<Project />");
        string project = Path.Combine(directory, "NonGenericEnumerableTests.csproj");
        File.WriteAllText(project, $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>14.0</LangVersion>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <PublishAot>true</PublishAot>
                <DefineConstants>{{(fallbackOnly ? "FALLBACK_ONLY" : "")}}</DefineConstants>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="..\NonGenericEnumerableTests.cs" />
                <Compile Include="..\NonGenericEnumerableNativeObjects.cs" />
                <Reference Include="WinRT.Runtime" HintPath="..\WinRT.Runtime.dll" />
                <Reference Include="WinRT.Projection" HintPath="..\WinRT.Projection.dll" />
                <Reference Include="WinRT.Sdk.Projection" HintPath="..\WinRT.Sdk.Projection.dll" />
                <!-- Do not root the whole sidecar: the fallback must retain its own generated support. -->
                <Reference Include="WinRT.Interop" HintPath="..\WinRT.Interop.dll" />
              </ItemGroup>
            </Project>
            """);
        string output = Path.Combine(directory, "publish");
        string rid = $"win-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
        string binlog = Path.GetFullPath(Path.Combine("TestResults", "non-generic-enumeration-aot-{}.binlog"));
        (int publishExitCode, string publishLog) = NativeProjectionFixture.Run("dotnet",
            ["publish", project, "-c", "Release", "-r", rid, "-o", output, $"-bl:{binlog}"]);
        Assert.AreEqual(0, publishExitCode, publishLog);
        (int exitCode, string log) = NativeProjectionFixture.Run(Path.Combine(output, "NonGenericEnumerableTests.exe"), []);
        Assert.AreEqual(0, exitCode, log);
        StringAssert.Contains(log, "Generated non-generic enumeration passed.");
        if (fallbackOnly)
        {
            StringAssert.Contains(log, "Compile-time-isolated generated fallback passed.");
        }
    }

    private static NativeProjectionFixture CreateNativeFixture(bool fallbackOnly = false)
    {
        return NativeProjectionFixture.Create(
            "enumerable", "NonGenericEnumerableTests", (directory, stem) => NonGenericEnumerableMetadata.Create(directory, stem),
            additionalResources: ["NonGenericEnumerableNativeObjects"],
            preprocessorSymbols: fallbackOnly ? ["FALLBACK_ONLY"] : []);
    }

    private static void GenerateProjection(string metadata, string output)
    {
        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [metadata],
            OutputFolder = output,
            Include = ["Contoso"]
        });
    }
}

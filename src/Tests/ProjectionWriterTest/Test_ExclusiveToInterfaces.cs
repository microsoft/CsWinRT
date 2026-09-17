// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public class Test_ExclusiveToInterfaces
{
    private const string ReferenceMetadataAttributeName = "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyMetadataAttribute";
    private const string IdicMetadataKey = "CsWinRT.IdicExclusiveTo.v1";

    [TestMethod]
    public void ReferenceMetadataAttribute_UsesReadOnlyKeyValuePairs()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 3))
        {
            Assert.Inconclusive("Windows Runtime metadata attributes require Windows 8.1 or later.");
            return;
        }

        WindowsRuntimeReferenceAssemblyMetadataAttribute metadata = new("Example", null);
        Assert.AreEqual("Example", metadata.Key);
        Assert.IsNull(metadata.Value);

        Type type = typeof(WindowsRuntimeReferenceAssemblyMetadataAttribute);
        Assert.IsFalse(type.GetProperty("Key")!.CanWrite);
        Assert.IsFalse(type.GetProperty("Value")!.CanWrite);
        AttributeUsageAttribute usage = type.GetCustomAttribute<AttributeUsageAttribute>()!;
        Assert.AreEqual(AttributeTargets.Assembly, usage.ValidOn);
        Assert.IsTrue(usage.AllowMultiple);
        Assert.IsFalse(usage.Inherited);
        Assert.IsEmpty(typeof(WindowsRuntimeReferenceAssemblyAttribute).GetProperties(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
    }

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
                IdicExclusiveTo = true,
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
                IdicExclusiveTo = true,
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
            _ = Compile(output, Path.Combine(directory, "implementation.dll"), referenceProjection: false);
        });
    }

    [TestMethod]
    [DataRow(false, "", "", "")]
    [DataRow(false, "Contoso.IWidget2", "", "")]
    [DataRow(false, "", "Contoso.IWidget3", "")]
    [DataRow(true, "", "", "Contoso.IWidget2;Contoso.IWidget3")]
    [DataRow(true, "", "Contoso.IWidget3", "Contoso.IWidget2")]
    [DataRow(true, "Contoso.IWidget2", "", "Contoso.IWidget2")]
    [DataRow(true, "Contoso.IWidget2;Contoso.IWidget3", "Contoso.IWidget3", "Contoso.IWidget2")]
    [DataRow(true, "Contoso.IWidget2", "Contoso", "")]
    [DataRow(true, "Contoso", "Contoso.IWidget2", "Contoso.IWidget3")]
    [DataRow(true, "Contoso.IWidget", "Contoso.IWidget3", "Contoso.IWidget2")]
    [DataRow(true, " Contoso.IWidget2 ; Contoso.IWidget2 ; ; ", " ; ", "Contoso.IWidget2")]
    [DataRow(true, " ; ", "", "Contoso.IWidget2;Contoso.IWidget3")]
    [DataRow(true, "contoso", "", "")]
    [DataRow(true, "Contoso.", "contoso", "Contoso.IWidget2;Contoso.IWidget3")]
    [DataRow(true, "Fabrikam", "", "")]
    public void IdicFilters_SelectCastingSupportOnly(bool enabled, string includes, string excludes, string selected)
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string output = Path.Combine(directory, "implementation");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = output,
                IncludeTypes = ["Contoso.IWidget2", "Contoso.IWidget3", "Contoso.ChangedHandler"],
                PublicExclusiveTo = true,
                IdicExclusiveTo = enabled,
                IdicExclusiveToIncludes = includes.Split(';'),
                IdicExclusiveToExcludes = excludes.Split(';')
            });

            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
            HashSet<string> expected = [.. selected.Split(';', StringSplitOptions.RemoveEmptyEntries)];
            ModuleDefinition module = ModuleDefinition.FromFile(Compile(
                output, Path.Combine(directory, "implementation.dll"), referenceProjection: false));

            foreach (string name in new[] { "IWidget2", "IWidget3" })
            {
                StringAssert.Contains(source, $"public interface {name}");
                StringAssert.Contains(source, $"class {name}Methods");
                StringAssert.Contains(source, $"class {name}Impl");
                AssertIdicImplementation(module, $"Contoso.{name}", expected.Contains($"Contoso.{name}"));
            }

            AssertSlot(GetMethods(source, "IWidget2"), "GetValue", 6);
            AssertSlot(GetMethods(source, "IWidget3"), "GetOtherValue", 6);
            Assert.IsFalse(module.TopLevelTypes.Any(type => type.FullName == "Contoso.Widget"));
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ExactIdicTypeSelection_RequiresOptInAndDoesNotMatchPrefixes(bool enabled)
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string output = Path.Combine(directory, "implementation");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = output,
                IncludeTypes = ["Contoso.IWidget", "Contoso.IWidget2", "Contoso.ChangedHandler"],
                PublicExclusiveTo = true,
                IdicExclusiveTo = enabled,
                IdicExclusiveToTypes = ["Contoso.IWidget"]
            });

            ModuleDefinition module = ModuleDefinition.FromFile(Compile(
                output, Path.Combine(directory, "implementation.dll"), referenceProjection: false));
            AssertIdicImplementation(module, "Contoso.IWidget", enabled);
            AssertIdicImplementation(module, "Contoso.IWidget2", enabled: false);
        });
    }

    [TestMethod]
    public void ExcludedIdicInterfaces_KeepDefaultAndOverridableSupport()
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            string output = Path.Combine(directory, "implementation");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadataPath],
                OutputFolder = output,
                Include = ["Contoso"],
                IdicExclusiveTo = true,
                IdicExclusiveToExcludes = ["Contoso"]
            });

            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
            ModuleDefinition module = ModuleDefinition.FromFile(Compile(
                output, Path.Combine(directory, "implementation.dll"), referenceProjection: false));
            Assert.IsTrue(module.TopLevelTypes.Any(type => type.FullName == "Contoso.Widget"));
            Assert.IsTrue(module.TopLevelTypes.Any(type => type.FullName == "Contoso.IWidget"));
            Assert.IsTrue(module.TopLevelTypes.Any(type => type.FullName == "Contoso.IWidget2"));
            Assert.IsTrue(module.TopLevelTypes.Any(type => type.FullName == "ABI.Contoso.IWidget2Impl"));
            AssertSlot(GetMethods(source, "IWidget"), "GetDefaultValue", 6);
            AssertSlot(GetMethods(source, "IWidget2"), "GetValue", 6);
            AssertIdicImplementation(module, "Contoso.IWidget", enabled: false);
            AssertIdicImplementation(module, "Contoso.IWidget2", enabled: false);
        }, overridable: true);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ExclusiveIdicFilters_DoNotDisableOrdinaryInterfaces(bool referenceProjection)
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            ModuleDefinition metadata = ModuleDefinition.FromFile(metadataPath);
            TypeDefinition ordinaryInterface = metadata.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget3");
            _ = ordinaryInterface.CustomAttributes.Remove(ordinaryInterface.CustomAttributes.Single(
                attribute => attribute.Constructor?.DeclaringType?.Name == "ExclusiveToAttribute"));
            string ordinaryMetadata = Path.Combine(directory, "Ordinary.winmd");
            metadata.Write(ordinaryMetadata);

            ModuleDefinition module;
            if (referenceProjection)
            {
                string reference = GenerateReference(directory, ordinaryMetadata, "Supplemental",
                    publicExclusiveTo: true, idicExclusiveTo: true,
                    "Contoso.IWidget2,Contoso.IWidget3,Contoso.ChangedHandler", idicExcludes: "Contoso");
                Assert.IsEmpty(GetRecordedIdicTypes(ModuleDefinition.FromFile(reference)));
                (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, ordinaryMetadata, reference);
                Assert.AreEqual(0, exitCode, log);
                module = ModuleDefinition.FromFile(assemblyPath);
            }
            else
            {
                string output = Path.Combine(directory, "implementation");
                ProjectionWriter.Run(new ProjectionWriterOptions
                {
                    InputPaths = [ordinaryMetadata],
                    OutputFolder = output,
                    IncludeTypes = ["Contoso.IWidget2", "Contoso.IWidget3", "Contoso.ChangedHandler"],
                    PublicExclusiveTo = true,
                    IdicExclusiveTo = true,
                    IdicExclusiveToExcludes = ["Contoso"]
                });
                module = ModuleDefinition.FromFile(Compile(
                    output, Path.Combine(directory, "implementation.dll"), referenceProjection: false));
            }

            AssertIdicImplementation(module, "Contoso.IWidget2", enabled: false);
            AssertIdicImplementation(module, "Contoso.IWidget3", enabled: true);
        });
    }

    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, false)]
    [DataRow(false, true, true)]
    [DataRow(true, false, false)]
    [DataRow(true, true, false)]
    [DataRow(true, true, true)]
    public void IdicImplementations_MatchProjectedExclusiveInheritance(bool referenceProjection, bool includeParent, bool publicParent)
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            ModuleDefinition metadata = ModuleDefinition.FromFile(metadataPath);
            TypeDefinition parent = metadata.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget");
            TypeDefinition child = metadata.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget2");
            child.Interfaces.Add(new InterfaceImplementation(parent));
            string requiredMetadata = Path.Combine(directory, "Required.winmd");
            metadata.Write(requiredMetadata);
            string output = Path.Combine(directory, "sources");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [requiredMetadata],
                OutputFolder = output,
                IncludeTypes = includeParent
                    ? ["Contoso.IWidget", "Contoso.IWidget2", "Contoso.ChangedHandler"]
                    : ["Contoso.IWidget2", "Contoso.ChangedHandler"],
                PublicExclusiveToTypes = publicParent ? ["Contoso.IWidget", "Contoso.IWidget2"] : ["Contoso.IWidget2"],
                IdicExclusiveTo = true,
                IdicExclusiveToIncludes = ["Contoso.IWidget2"],
                ReferenceProjection = referenceProjection
            });

            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
            InterfaceDeclarationSyntax projected = CSharpSyntaxTree.ParseText(source,
                new CSharpParseOptions(LanguageVersion.CSharp14)).GetRoot()
                .DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                .Single(type => type.Identifier.ValueText == "IWidget2" && type.Modifiers.Any(SyntaxKind.PublicKeyword));
            Assert.IsNull(projected.BaseList);
            Assert.IsFalse(source.Contains("global::Contoso.IWidget.", StringComparison.Ordinal));

            string assemblyPath = Compile(output, Path.Combine(directory, "Supplemental.dll"), referenceProjection);
            if (referenceProjection)
            {
                (int exitCode, string log, string implementationPath) = RunImplementationGenerator(
                    directory, requiredMetadata, assemblyPath);
                Assert.AreEqual(0, exitCode, log);
                assemblyPath = implementationPath;
            }

            ModuleDefinition module = ModuleDefinition.FromFile(assemblyPath);
            AssertIdicImplementation(module, "Contoso.IWidget2", enabled: true);
            AssertIdicImplementation(module, "Contoso.IWidget", enabled: false);
        });
    }

    [TestMethod]
    public void IdicOptIn_AddsOneShimAndAssociationForSingleMethodInterface()
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            ModuleDefinition Generate(bool enabled)
            {
                string output = Path.Combine(directory, $"implementation-{enabled}");
                ProjectionWriter.Run(new ProjectionWriterOptions
                {
                    InputPaths = [metadataPath],
                    OutputFolder = output,
                    IncludeTypes = ["Contoso.IWidget3"],
                    PublicExclusiveTo = true,
                    IdicExclusiveTo = enabled
                });
                return ModuleDefinition.FromFile(Compile(
                    output, Path.Combine(directory, $"implementation-{enabled}.dll"), referenceProjection: false));
            }

            ModuleDefinition disabled = Generate(enabled: false);
            ModuleDefinition enabled = Generate(enabled: true);
            Assert.AreEqual(disabled.TopLevelTypes.Count + 1, enabled.TopLevelTypes.Count);
            Assert.AreEqual(disabled.TopLevelTypes.Sum(type => type.Methods.Count) + 1,
                enabled.TopLevelTypes.Sum(type => type.Methods.Count));
            Assert.AreEqual(disabled.Assembly!.CustomAttributes.Count + 1, enabled.Assembly!.CustomAttributes.Count);
            AssertIdicImplementation(disabled, "Contoso.IWidget3", enabled: false);
            AssertIdicImplementation(enabled, "Contoso.IWidget3", enabled: true);
        });
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("*")]
    [DataRow("Contoso.*")]
    [DataRow("Contoso.IWidget?")]
    [DataRow("Contoso..IWidget")]
    [DataRow("Contoso.1Widget")]
    [DataRow("Contoso.I Widget")]
    [DataRow("Contoso;Fabrikam")]
    public void InvalidIdicFilter_ReportsDiagnostic(string? filter)
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            foreach (bool isExclude in new[] { false, true })
            {
                Exception exception = Assert.Throws<Exception>(() => ProjectionWriter.Run(new ProjectionWriterOptions
                {
                    InputPaths = [metadataPath],
                    OutputFolder = Path.Combine(directory, "implementation"),
                    Include = ["Contoso"],
                    IdicExclusiveTo = true,
                    IdicExclusiveToIncludes = isExclude ? [] : [filter!],
                    IdicExclusiveToExcludes = isExclude ? [filter!] : []
                }));

                StringAssert.Contains(exception.ToString(), "CSWINRTPROJECTIONGEN5022");
                StringAssert.Contains(exception.ToString(), isExclude ? "IdicExclusiveToExcludes" : "IdicExclusiveToIncludes");
            }
        });
    }

    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, false, true)]
    [DataRow(false, true, false)]
    [DataRow(false, true, true)]
    [DataRow(true, false, false)]
    [DataRow(true, false, true)]
    [DataRow(true, true, false)]
    [DataRow(true, true, true)]
    public void ReferenceProjection_PreservesIndependentIntent(bool fastAbi, bool publicExclusiveTo, bool idicExclusiveTo)
    {
        WithMetadata(fastAbi, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo, idicExclusiveTo, "Contoso.IWidget2,Contoso.ChangedHandler");
            ModuleDefinition referenceModule = ModuleDefinition.FromFile(reference);
            CollectionAssert.AreEqual(idicExclusiveTo ? new[] { "Contoso.IWidget2" } : [],
                GetRecordedIdicTypes(referenceModule));
            Assert.AreEqual(publicExclusiveTo, referenceModule.TopLevelTypes.Any(
                type => type.FullName == "Contoso.IWidget2" && type.IsPublic));

            (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, metadataPath, reference);
            Assert.AreEqual(0, exitCode, log);

            ModuleDefinition module = ModuleDefinition.FromFile(assemblyPath);
            TypeDefinition? iface = module.TopLevelTypes.SingleOrDefault(type => type.FullName == "Contoso.IWidget2");
            Assert.AreEqual(publicExclusiveTo || idicExclusiveTo, iface is not null);

            if (iface is not null)
            {
                Assert.AreEqual(publicExclusiveTo, iface.IsPublic);
                Assert.IsTrue(iface.IsInterface);
                Assert.AreEqual(new Guid(ExclusiveToMetadata.SecondaryInterfaceIid),
                    new Guid(iface.CustomAttributes.Single(attribute => attribute.Constructor?.DeclaringType?.Name == "GuidAttribute")
                        .Signature!.FixedArguments[0].Element!.ToString()!));
            }

            Assert.IsFalse(module.TopLevelTypes.Any(type => type.FullName is "Contoso.Widget" or "Contoso.IWidget3"));
            AssertIdicImplementation(module, "Contoso.IWidget2", idicExclusiveTo);

            if (publicExclusiveTo || idicExclusiveTo)
            {
                MethodDefinition method = module.TopLevelTypes.Single(type => type.FullName == "ABI.Contoso.IWidget2Methods")
                    .Methods.Single(method => method.Name == "GetValue");
                Assert.IsTrue(method.CilMethodBody!.Instructions.Any(instruction => instruction.OpCode.Code == CilCode.Calli));
            }

            // Removing the only projection reference must also remove the previous merged output.
            (exitCode, log, _) = RunImplementationGenerator(directory, metadataPath);
            Assert.AreEqual(0, exitCode, log);
            Assert.IsFalse(File.Exists(assemblyPath));
        });
    }

    [TestMethod]
    [DataRow(false, "Contoso.IWidget2", "", "")]
    [DataRow(true, "", "Contoso.IWidget3", "Contoso.IWidget2")]
    [DataRow(true, "Contoso.IWidget2,Contoso.IWidget3", "Contoso.IWidget3", "Contoso.IWidget2")]
    [DataRow(true, "Contoso.IWidget2", "Contoso", "")]
    [DataRow(true, " Contoso.IWidget3 , Contoso.IWidget2,Contoso.IWidget3 ", "", "Contoso.IWidget2;Contoso.IWidget3")]
    public void ReferenceProjection_IdicFiltersRoundTrip(bool enabled, string includes, string excludes, string selected)
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, enabled, "Contoso.IWidget2,Contoso.IWidget3,Contoso.ChangedHandler", includes, excludes);
            string[] expected = selected.Split(';', StringSplitOptions.RemoveEmptyEntries);
            CollectionAssert.AreEqual(expected, GetRecordedIdicTypes(ModuleDefinition.FromFile(reference)));
            (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, metadataPath, reference);
            Assert.AreEqual(0, exitCode, log);

            ModuleDefinition module = ModuleDefinition.FromFile(assemblyPath);
            foreach (string typeName in new[] { "Contoso.IWidget2", "Contoso.IWidget3" })
            {
                Assert.IsTrue(module.TopLevelTypes.Single(type => type.FullName == typeName).IsPublic);
                AssertIdicImplementation(module, typeName, expected.Contains(typeName, StringComparer.Ordinal));
                Assert.IsTrue(module.TopLevelTypes.Any(type => type.FullName == $"ABI.{typeName}Methods"));
                Assert.IsTrue(module.TopLevelTypes.Any(type => type.FullName == $"ABI.{typeName}Impl"));
            }
        });
    }

    [TestMethod]
    public void ReferenceProjection_RecordedTypeNamesAreExact()
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, idicExclusiveTo: true, "Contoso.IWidget,Contoso.ChangedHandler",
                idicExcludes: "Contoso.IWidget2,Contoso.IWidget3");
            CollectionAssert.AreEqual(new[] { "Contoso.IWidget" }, GetRecordedIdicTypes(ModuleDefinition.FromFile(reference)));
            (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, metadataPath, reference);
            Assert.AreEqual(0, exitCode, log);
            ModuleDefinition module = ModuleDefinition.FromFile(assemblyPath);
            AssertIdicImplementation(module, "Contoso.IWidget", enabled: true);
            AssertIdicImplementation(module, "Contoso.IWidget2", enabled: false);
            AssertIdicImplementation(module, "Contoso.IWidget3", enabled: false);
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ReferenceProjection_DebugReproPreservesIdicFilters(bool enabled)
    {
        WithMetadata(fastAbi: true, (directory, metadataPath) =>
        {
            string reproDirectory = Directory.CreateDirectory(Path.Combine(directory, "repro")).FullName;
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, enabled, "Contoso.IWidget2,Contoso.IWidget3,Contoso.ChangedHandler",
                idicIncludes: "Contoso.IWidget2,Contoso.IWidget3", idicExcludes: "Contoso.IWidget3",
                debugReproDirectory: reproDirectory);
            string[] expected = GetRecordedIdicTypes(ModuleDefinition.FromFile(reference));
            string reproPath = Path.Combine(reproDirectory, "ref-projection-debug-repro.zip");

            using (ZipArchive archive = ZipFile.OpenRead(reproPath))
            using (StreamReader reader = new(archive.GetEntry("cswinrtprojectionrefgen.rsp")!.Open()))
            {
                string response = reader.ReadToEnd();
                StringAssert.Contains(response, "--idic-exclusive-to-includes Contoso.IWidget2,Contoso.IWidget3");
                StringAssert.Contains(response, "--idic-exclusive-to-excludes Contoso.IWidget3");
            }

            (int exitCode, string log) = ProjectionWriterRunner.Run(
                ProjectionWriterRunner.GetRequiredFilePath("ProjectionRefGeneratorAssemblyPath"), reproPath);
            Assert.AreEqual(0, exitCode, log);
            const string outputPrefix = "Generating reference projection sources -> ";
            string replayDirectory = log.Split('\n').Single(line => line.StartsWith(outputPrefix, StringComparison.Ordinal))
                [outputPrefix.Length..].Trim();
            Assert.IsTrue(Path.IsPathFullyQualified(replayDirectory));
            Assert.IsTrue(Path.GetFullPath(replayDirectory).StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(Path.GetFileName(replayDirectory).StartsWith(
                "cswinrtprojectionrefgen-debug-repro-unpack-", StringComparison.Ordinal));

            try
            {
                string replayReference = Compile(replayDirectory, Path.Combine(directory, "Replayed.dll"), referenceProjection: true);
                CollectionAssert.AreEqual(expected, GetRecordedIdicTypes(ModuleDefinition.FromFile(replayReference)));
                string replayResponse = File.ReadAllText(Path.Combine(replayDirectory, "cswinrtprojectionrefgen.rsp"));
                StringAssert.Contains(replayResponse, "--idic-exclusive-to-includes Contoso.IWidget2,Contoso.IWidget3");
                StringAssert.Contains(replayResponse, "--idic-exclusive-to-excludes Contoso.IWidget3");
            }
            finally
            {
                Directory.Delete(replayDirectory, recursive: true);
            }
        });
    }

    [TestMethod]
    public void ReferenceProjection_AbsentIntentDoesNotInferIdic()
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, idicExclusiveTo: true, "Contoso.IWidget3");
            ModuleDefinition referenceModule = ModuleDefinition.FromFile(reference);
            foreach (CustomAttribute attribute in GetReferenceMetadata(referenceModule).ToArray())
            {
                _ = referenceModule.Assembly!.CustomAttributes.Remove(attribute);
            }
            string unmarkedReference = Path.Combine(directory, "Preview.dll");
            referenceModule.Write(unmarkedReference);

            (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, metadataPath, unmarkedReference);
            Assert.AreEqual(0, exitCode, log);
            ModuleDefinition module = ModuleDefinition.FromFile(assemblyPath);
            Assert.IsTrue(module.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget3").IsPublic);
            AssertIdicImplementation(module, "Contoso.IWidget3", enabled: false);
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ReferenceProjection_DuplicateTypesAreRejectedDeterministically(bool secondEnablesIdic)
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            string first = GenerateReference(directory, metadataPath, "First",
                publicExclusiveTo: true, idicExclusiveTo: true, "Contoso.IWidget3");
            string second = GenerateReference(directory, metadataPath, "Second",
                publicExclusiveTo: true, secondEnablesIdic, "Contoso.IWidget3");

            (int exitCode, string log, _) = RunImplementationGenerator(directory, metadataPath, first, second);
            Assert.AreNotEqual(0, exitCode, log);
            StringAssert.Contains(log, "CSWINRTPROJECTIONGEN0015");
            StringAssert.Contains(log, "Contoso.IWidget3");
            StringAssert.Contains(log, first);
            StringAssert.Contains(log, second);

            (int reversedExitCode, string reversedLog, _) = RunImplementationGenerator(directory, metadataPath, second, first);
            Assert.AreNotEqual(0, reversedExitCode, reversedLog);
            Assert.AreEqual(
                log.Split('\n').Single(line => line.Contains("CSWINRTPROJECTIONGEN0015", StringComparison.Ordinal)),
                reversedLog.Split('\n').Single(line => line.Contains("CSWINRTPROJECTIONGEN0015", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    [DataRow(null, "Contoso.IWidget3")]
    [DataRow(IdicMetadataKey, null)]
    [DataRow(IdicMetadataKey, "")]
    [DataRow(IdicMetadataKey, " ")]
    [DataRow(IdicMetadataKey, " Contoso.IWidget3")]
    [DataRow(IdicMetadataKey, "Contoso.IWidget3 ")]
    public void ReferenceProjection_MalformedIntentIsRejected(string? key, string? value)
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, idicExclusiveTo: true, "Contoso.IWidget3");
            ModuleDefinition module = ModuleDefinition.FromFile(reference);
            CustomAttribute attribute = GetReferenceMetadata(module).Single();
            attribute.Signature!.FixedArguments[0].Elements[0] = key;
            attribute.Signature.FixedArguments[1].Elements[0] = value;

            string malformedReference = Path.Combine(directory, "Malformed.dll");
            module.Write(malformedReference);
            (int exitCode, string log, _) = RunImplementationGenerator(directory, metadataPath, malformedReference);
            Assert.AreNotEqual(0, exitCode, log);
            StringAssert.Contains(log, "CSWINRTPROJECTIONGEN0014");
        });
    }

    [TestMethod]
    public void ReferenceProjection_UnknownMetadataKeysAreIgnored()
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, idicExclusiveTo: true, "Contoso.IWidget3");
            ModuleDefinition module = ModuleDefinition.FromFile(reference);
            CustomAttribute attribute = GetReferenceMetadata(module).Single();
            attribute.Signature!.FixedArguments[0].Elements[0] = "CsWinRT.FutureMetadata.v1";
            attribute.Signature.FixedArguments[1].Elements[0] = null;
            string updatedReference = Path.Combine(directory, "Future.dll");
            module.Write(updatedReference);

            (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, metadataPath, updatedReference);
            Assert.AreEqual(0, exitCode, log);
            AssertIdicImplementation(ModuleDefinition.FromFile(assemblyPath), "Contoso.IWidget3", enabled: false);
        });
    }

    [TestMethod]
    public void ReferenceProjection_DuplicateMetadataEntriesAreDeduplicated()
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, idicExclusiveTo: true, "Contoso.IWidget3");
            ModuleDefinition module = ModuleDefinition.FromFile(reference);
            CustomAttribute attribute = GetReferenceMetadata(module).Single();
            module.Assembly!.CustomAttributes.Add(new CustomAttribute(attribute.Constructor!, attribute.Signature));
            string updatedReference = Path.Combine(directory, "Repeated.dll");
            module.Write(updatedReference);

            (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, metadataPath, updatedReference);
            Assert.AreEqual(0, exitCode, log);
            AssertIdicImplementation(ModuleDefinition.FromFile(assemblyPath), "Contoso.IWidget3", enabled: true);
        });
    }

    [TestMethod]
    public void ReferenceProjection_OrdinaryAssemblyMetadataDoesNotEnableIdic()
    {
        WithMetadata(fastAbi: false, (directory, metadataPath) =>
        {
            string reference = GenerateReference(directory, metadataPath, "Supplemental",
                publicExclusiveTo: true, idicExclusiveTo: true, "Contoso.IWidget3");
            ModuleDefinition module = ModuleDefinition.FromFile(reference);
            _ = module.Assembly!.CustomAttributes.Remove(GetReferenceMetadata(module).Single());
            AddAssemblyMetadata(module, IdicMetadataKey, "Contoso.IWidget3");
            string updatedReference = Path.Combine(directory, "OrdinaryMetadata.dll");
            module.Write(updatedReference);

            (int exitCode, string log, string assemblyPath) = RunImplementationGenerator(directory, metadataPath, updatedReference);
            Assert.AreEqual(0, exitCode, log);
            AssertIdicImplementation(ModuleDefinition.FromFile(assemblyPath), "Contoso.IWidget3", enabled: false);
        });
    }

    private static string GenerateReference(
        string directory, string metadataPath, string assemblyName, bool publicExclusiveTo, bool idicExclusiveTo,
        string includeNamespaces, string idicIncludes = "", string idicExcludes = "", string? debugReproDirectory = null)
    {
        string sources = Path.Combine(directory, assemblyName);
        string responseFile = Path.Combine(directory, $"{assemblyName}.rsp");
        List<string> arguments =
        [
            $"--input-paths {metadataPath}",
            $"--output-directory {sources}",
            "--target-framework net10.0",
            $"--include-namespaces {includeNamespaces}",
            $"--public-exclusive-to {publicExclusiveTo.ToString().ToLowerInvariant()}",
            $"--idic-exclusive-to {idicExclusiveTo.ToString().ToLowerInvariant()}",
            "--reference-projection true"
        ];

        if (idicIncludes.Length > 0)
        {
            arguments.Add($"--idic-exclusive-to-includes {idicIncludes}");
        }

        if (idicExcludes.Length > 0)
        {
            arguments.Add($"--idic-exclusive-to-excludes {idicExcludes}");
        }

        if (debugReproDirectory is not null)
        {
            arguments.Add($"--debug-repro-directory {debugReproDirectory}");
        }

        File.WriteAllLines(responseFile, arguments);
        (int exitCode, string log) = ProjectionWriterRunner.Run(
            ProjectionWriterRunner.GetRequiredFilePath("ProjectionRefGeneratorAssemblyPath"), $"@{responseFile}");
        Assert.AreEqual(0, exitCode, log);
        return Compile(sources, Path.Combine(directory, $"{assemblyName}.dll"), referenceProjection: true);
    }

    private static (int ExitCode, string Log, string AssemblyPath) RunImplementationGenerator(
        string directory, string metadataPath, params string[] projectionReferences)
    {
        string output = Directory.CreateDirectory(Path.Combine(directory, "merged")).FullName;
        string responseFile = Path.Combine(directory, "projection.rsp");
        string[] references =
        [
            .. projectionReferences,
            typeof(WindowsRuntimeObject).Assembly.Location,
            .. Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").Where(IsManagedAssembly)
        ];
        File.WriteAllText(responseFile, $"""
            --reference-assembly-paths {string.Join(",", references)}
            --generated-assembly-directory {output}
            --winmd-paths {metadataPath}
            --windows-metadata {metadataPath}
            --target-framework net10.0
            --max-degrees-of-parallelism 1
            """);

        (int exitCode, string log) = ProjectionWriterRunner.Run(
            ProjectionWriterRunner.GetRequiredFilePath("ProjectionGeneratorAssemblyPath"), $"@{responseFile}");
        return (exitCode, log, Path.Combine(output, "WinRT.Projection.dll"));
    }

    private static IEnumerable<CustomAttribute> GetReferenceMetadata(ModuleDefinition module)
    {
        return module.Assembly!.CustomAttributes.Where(attribute =>
            attribute.Constructor?.DeclaringType?.FullName == ReferenceMetadataAttributeName);
    }

    private static string[] GetRecordedIdicTypes(ModuleDefinition module)
    {
        return [.. GetReferenceMetadata(module)
            .Where(attribute => attribute.Signature!.FixedArguments[0].Element?.ToString() == IdicMetadataKey)
            .Select(attribute => attribute.Signature!.FixedArguments[1].Element!.ToString()!)];
    }

    private static void AddAssemblyMetadata(ModuleDefinition module, string key, string value)
    {
        TypeReference attributeType = new(module, module.CorLibTypeFactory.CorLibScope,
            "System.Reflection", "AssemblyMetadataAttribute");
        MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
            module.CorLibTypeFactory.Void, [module.CorLibTypeFactory.String, module.CorLibTypeFactory.String]));
        module.Assembly!.CustomAttributes.Add(new CustomAttribute(constructor, new CustomAttributeSignature(
        [
            new CustomAttributeArgument(module.CorLibTypeFactory.String, key),
            new CustomAttributeArgument(module.CorLibTypeFactory.String, value)
        ])));
    }

    private static void AssertIdicImplementation(ModuleDefinition module, string typeName, bool enabled)
    {
        Assert.AreEqual(enabled, module.TopLevelTypes.Any(type =>
            type.CustomAttributes.Any(attribute =>
                attribute.Constructor?.DeclaringType?.Name == "DynamicInterfaceCastableImplementationAttribute") &&
            type.Interfaces.Any(implementation => implementation.Interface?.FullName == typeName)), typeName);
        Assert.AreEqual(enabled, module.Assembly!.CustomAttributes.Any(attribute =>
            attribute.Constructor?.DeclaringType?.FullName.Contains("DynamicInterfaceCastableImplementationTypeMapGroup", StringComparison.Ordinal) == true &&
            attribute.Signature!.FixedArguments[0].Element is TypeSignature source && source.FullName == typeName), typeName);
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
        CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14,
            preprocessorSymbols: referenceProjection ? ["CSWINRT_REFERENCE_PROJECTION"] : []);
        CSharpCompilation compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(assemblyPath),
            Directory.GetFiles(sourceDirectory, "*.cs").Select(path => CSharpSyntaxTree.ParseText(
                File.ReadAllText(path), parseOptions, path: path)),
            [.. Net100.References.All, MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        using FileStream stream = File.Create(assemblyPath);
        EmitResult result = compilation.Emit(stream,
            options: new EmitOptions(metadataOnly: referenceProjection, includePrivateMembers: !referenceProjection));
        Assert.IsTrue(result.Success, $"Projection compilation failed:\n{string.Join("\n", result.Diagnostics)}");
        return assemblyPath;
    }

    private static bool IsManagedAssembly(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using System.Reflection.PortableExecutable.PEReader reader = new(stream);
        return reader.HasMetadata;
    }

    private static void WithMetadata(bool fastAbi, Action<string, string> action, bool overridable = false)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionExclusiveToTest_").FullName;
        try
        {
            action(directory, ExclusiveToMetadata.Create(directory, fastAbi, overridable));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

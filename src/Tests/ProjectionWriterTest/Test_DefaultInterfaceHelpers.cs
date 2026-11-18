// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Covers the internal default-interface helpers used to marshal unsealed projected classes.
/// </summary>
[TestClass]
public class Test_DefaultInterfaceHelpers
{
    /// <summary>
    /// A derived helper hides an inherited one only when that member is emitted into the same projection.
    /// Non-exclusive defaults use an explicit interface implementation instead and have no helper to hide.
    /// </summary>
    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, false)]
    [DataRow(true, false, false)]
    [DataRow(true, true, false)]
    [DataRow(false, false, true)]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    [DataRow(true, true, true)]
    public void DerivedHelper_OnlyHidesAccessibleBaseMember(bool sameProjection, bool exclusiveDefault, bool intermediateBase)
    {
        string directory = Directory.CreateTempSubdirectory("DefaultInterfaceHelpers_").FullName;

        try
        {
            string metadata = ImplementableHierarchyMetadata.Create(directory);
            ModuleDefinition module = ModuleDefinition.FromFile(metadata);

            void MakeDefaultNonExclusive(string interfaceName)
            {
                TypeDefinition baseInterface = module.TopLevelTypes.Single(type => type.FullName == interfaceName);
                CustomAttribute exclusive = baseInterface.CustomAttributes.Single(attribute =>
                    attribute.Constructor?.DeclaringType?.FullName == "Windows.Foundation.Metadata.ExclusiveToAttribute");
                baseInterface.CustomAttributes.Remove(exclusive);
            }

            if (!exclusiveDefault)
            {
                MakeDefaultNonExclusive("Contoso.IStringableBase");
            }

            string[] baseTypes = ["Contoso.StringableBase", "Contoso.IStringableBase", "Contoso.IStringableBaseFactory"];
            string[] derivedTypes = ["Contoso.StringableDerived", "Contoso.IStringableDerived", "Contoso.IStringableDerivedFactory"];
            string[] references = [];

            // Search past a non-exclusive intermediate default for a helper on an earlier ancestor
            if (intermediateBase)
            {
                TypeDefinition intermediate = module.TopLevelTypes.Single(type => type.FullName == "Contoso.ClosableBase");
                intermediate.BaseType = module.TopLevelTypes.Single(type => type.FullName == "Contoso.StringableBase");
                module.TopLevelTypes.Single(type => type.FullName == "Contoso.StringableDerived").BaseType = intermediate;
                MakeDefaultNonExclusive("Contoso.IClosableBase");
                baseTypes = [.. baseTypes, "Contoso.ClosableBase", "Contoso.IClosableBase", "Contoso.IClosableBaseFactory"];
            }

            metadata = Path.Combine(directory, "Hierarchy.winmd");
            module.Write(metadata);

            string Generate(string name, string[] includes)
            {
                string output = Path.Combine(directory, name);
                ProjectionWriter.Run(new ProjectionWriterOptions
                {
                    InputPaths = [metadata, ImplementableHierarchyMetadata.FindWindowsFoundationWinmd()],
                    OutputFolder = output,
                    OutputAssemblyName = name,
                    IncludeTypes = includes
                });
                return output;
            }

            // External base helpers are internal to that assembly, just like the precompiled SDK XAML helpers
            if (!sameProjection)
            {
                string baseOutput = Generate("BaseProjection", baseTypes);
                references = [ProjectionWriterRunner.CompileSources(
                    Directory.GetFiles(baseOutput, "*.cs").Select(File.ReadAllText),
                    Path.Combine(directory, "BaseProjection.dll"))];
            }

            string derivedOutput = Generate("DerivedProjection", sameProjection ? [.. baseTypes, .. derivedTypes] : derivedTypes);
            ClassDeclarationSyntax derived = Directory.GetFiles(derivedOutput, "*.cs")
                .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                .Single(type => type.Identifier.ValueText == ImplementableHierarchyMetadata.StringableDerived &&
                    type.Parent is BaseNamespaceDeclarationSyntax { Name: var ns } && ns.ToString() == "Contoso");
            MethodDeclarationSyntax helper = derived.Members.OfType<MethodDeclarationSyntax>()
                .Single(method => method.Identifier.ValueText == "GetDefaultInterface");

            Assert.AreEqual(sameProjection && exclusiveDefault, helper.Modifiers.Any(SyntaxKind.NewKeyword), helper.ToString());
            _ = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(derivedOutput, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "DerivedProjection.dll"),
                additionalReferences: references);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

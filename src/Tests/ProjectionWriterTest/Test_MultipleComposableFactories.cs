// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Covers a runtime class activated through more than one <c>[Composable]</c> factory interface, which MIDL emits
/// for constructors added in a later contract version (e.g. WinUI's <c>DataTemplate</c>).
/// </summary>
[TestClass]
public class Test_MultipleComposableFactories
{
    /// <summary>
    /// Each factory contributes its own constructors, but the base-chaining constructors that derived projected
    /// types call belong to the class, so they are emitted once (twice would not compile, with CS0111).
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MultipleFactories_EmitEachConstructorOnceAndCompile(bool referenceProjection)
    {
        string directory = Directory.CreateTempSubdirectory("MultipleComposableFactories_").FullName;

        try
        {
            string output = Path.Combine(directory, "generated");

            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [ComposableFactoryMetadata.Create(directory)],
                OutputFolder = output,
                Include = ["Contoso"],
                ReferenceProjection = referenceProjection
            });

            string[] sources = Directory.GetFiles(output, "*.cs").Select(File.ReadAllText).ToArray();

            ConstructorDeclarationSyntax[] constructors = sources
                .SelectMany(static source => CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                .Single(static type => type.Identifier.ValueText == ComposableFactoryMetadata.ClassName && type.Parent is BaseNamespaceDeclarationSyntax { Name: var ns } && ns.ToString() == "Contoso")
                .Members.OfType<ConstructorDeclarationSyntax>()
                .ToArray();

            // One public constructor per factory: 'Widget()' and 'Widget(int)'
            CollectionAssert.AreEquivalent(
                new[] { 0, 1 },
                constructors.Where(static ctor => ctor.Modifiers.Any(SyntaxKind.PublicKeyword)).Select(static ctor => ctor.ParameterList.Parameters.Count).ToArray());

            // The four base-chaining constructors, once each (the reference projection omits them)
            Assert.AreEqual(referenceProjection ? 0 : 4, constructors.Count(static ctor =>
                ctor.Modifiers.Any(SyntaxKind.ProtectedKeyword) && ctor.ParameterList.Parameters.Count == 4));

            Assert.AreEqual(constructors.Length, constructors.Select(static ctor => ctor.ParameterList.ToString()).Distinct().Count());

            // Also compiles 'DerivedWidget', which chains to the base-chaining constructors
            _ = ProjectionWriterRunner.CompileSources(sources, Path.Combine(directory, "Projection.dll"), referenceProjection);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

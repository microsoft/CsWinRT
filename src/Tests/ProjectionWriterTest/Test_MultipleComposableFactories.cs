// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
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
    /// Guards the factory emitter's resource-free pass-array optimization while preserving initialization
    /// of temporary COM-pointer and pinned-handle slots needed by exception cleanup.
    /// </summary>
    [TestMethod]
    public void FactoryPassArrays_SkipClearingResourceFreeAbiBuffers()
    {
        string directory = Directory.CreateTempSubdirectory("FactoryPassArrayCleanup_").FullName;
        try
        {
            string metadata = ComposableFactoryMetadata.Create(directory);
            ModuleDefinition module = ModuleDefinition.FromFile(metadata);
            AssemblyReference foundation = module.AssemblyReferences.Single(reference =>
                reference.Name == "Windows.Foundation.FoundationContract");
            TypeDefinition stamp = new("Contoso", "Stamp",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
                new TypeReference(module, module.CorLibTypeFactory.CorLibScope, "System", "ValueType"));
            stamp.Fields.Add(new FieldDefinition("When", FieldAttributes.Public,
                new TypeReference(module, foundation, "Windows.Foundation", "DateTime").ToTypeSignature(true)));
            stamp.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.CorLibTypeFactory.Int32));
            module.TopLevelTypes.Add(stamp);
            MethodDefinition factory = module.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidgetFactory2").Methods.Single();
            MethodSignature signature = factory.Signature!;
            factory.Signature = MethodSignature.CreateInstance(signature.ReturnType,
            [
                new SzArrayTypeSignature(stamp.ToTypeSignature(true)),
                new SzArrayTypeSignature(module.CorLibTypeFactory.Object),
                new SzArrayTypeSignature(module.CorLibTypeFactory.String),
                signature.ParameterTypes[^2],
                signature.ParameterTypes[^1]
            ]);
            factory.ParameterDefinitions.Clear();
            string[] names = ["stamps", "objects", "strings", "baseInterface", "innerInterface"];
            for (int i = 0; i < names.Length; i++)
            {
                factory.ParameterDefinitions.Add(new ParameterDefinition((ushort)(i + 1), names[i],
                    i == names.Length - 1 ? ParameterAttributes.Out : ParameterAttributes.In));
            }
            module.Write(metadata);
            string output = Path.Combine(directory, "generated");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadata, ImplementableHierarchyMetadata.FindWindowsFoundationWinmd()],
                OutputFolder = output,
                OutputAssemblyName = "Projection",
                Include = ["Contoso"]
            });
            string[] sources = Directory.GetFiles(output, "*.cs").Select(File.ReadAllText).ToArray();
            _ = ProjectionWriterRunner.CompileSources(sources, Path.Combine(directory, "Projection.dll"));
            MethodDeclarationSyntax callback = sources.SelectMany(source =>
                    CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                .Single(method => method.Identifier.ValueText == "Invoke" &&
                    method.ToString().Contains("ArrayPool<global::ABI.Contoso.Stamp>", System.StringComparison.Ordinal));
            string body = callback.ToString();
            Assert.IsFalse(body.Contains("__stamps_span.Clear()", System.StringComparison.Ordinal), body);
            StringAssert.Contains(body, "CopyToUnmanaged_stamps");
            StringAssert.Contains(body, "__objects_span.Clear()");
            StringAssert.Contains(body, "__strings_pinnedHandleSpan.Clear()");
            InvocationExpressionSyntax firstConversion = callback.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Single(call => call.Expression is IdentifierNameSyntax { Identifier.ValueText: "CopyToUnmanaged_stamps" });
            foreach (string clear in new[] { "__objects_span.Clear", "__strings_pinnedHandleSpan.Clear" })
            {
                InvocationExpressionSyntax initialization = callback.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Single(call => call.Expression.ToString() == clear);
                Assert.IsTrue(initialization.SpanStart < firstConversion.SpanStart,
                    "Cleanup-bearing slots must be initialized before an earlier parameter's conversion can fail.");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

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
                OutputAssemblyName = "Projection",
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

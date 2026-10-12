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
/// Covers the implementable bases of runtime classes that derive from other runtime classes, or whose shape could
/// collide with generated code (see <see cref="ImplementableHierarchyMetadata"/>).
/// </summary>
[TestClass]
public class Test_ImplementableHierarchy
{
    /// <summary>
    /// The base for <c>IClosable</c> already declares the <c>Dispose()</c> that <c>IIterator&lt;int&gt;</c> (as
    /// <c>IEnumerator&lt;int&gt;</c>) also needs, so redeclaring it would hide the inherited abstract member.
    /// </summary>
    [TestMethod]
    public void InheritedMappedMember_IsNotRedeclared()
    {
        ClassDeclarationSyntax derived = GetAbiClass(ImplementableHierarchyMetadata.IteratorDerived);

        Assert.IsFalse(GetMethodNames(derived).Contains("Dispose"));
        Assert.IsTrue(GetMethodNames(derived).Contains("MoveNext"));
        Assert.IsTrue(GetMethodNames(GetAbiClass(ImplementableHierarchyMetadata.ClosableBase)).Contains("Dispose"));
    }

    /// <summary>
    /// A derived base declares its own conversion to each projected ancestor, so converting to one is not ambiguous
    /// with the conversion it inherits from the ancestor's base.
    /// </summary>
    [TestMethod]
    public void DerivedBase_ConvertsToProjectedAncestors()
    {
        string text = GetAbiClass(ImplementableHierarchyMetadata.IteratorDerived).ToString();

        StringAssert.Contains(text, $"public static implicit operator global::Contoso.{ImplementableHierarchyMetadata.ClosableBase}({ImplementableHierarchyMetadata.IteratorDerived} value)");
        StringAssert.Contains(text, $"return (global::Contoso.{ImplementableHierarchyMetadata.IteratorDerived})value;");
    }

    /// <summary>
    /// The same instantiation of a generic interface reached again through a derived class is only declared once.
    /// </summary>
    [TestMethod]
    public void SameGenericInstantiation_IsDeclaredOnce()
    {
        ClassDeclarationSyntax derived = GetAbiClass(ImplementableHierarchyMetadata.SameIterableDerived);

        Assert.IsFalse(GetMethodNames(derived).Contains("GetEnumerator"));
        Assert.IsFalse(derived.BaseList!.Types.Any(static type => type.ToString().Contains("IEnumerable", System.StringComparison.Ordinal)));
    }

    /// <summary>
    /// A different instantiation of a generic interface the base class already implements is not dropped. Its members
    /// differ only in their types from the inherited abstract ones, which C# cannot declare alongside them, so the
    /// class gets no base rather than one that silently leaves the interface out.
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DistinctGenericInstantiation_IsNotDropped(bool referenceProjection)
    {
        string[] names = GetAbiClassNames(referenceProjection);

        Assert.IsFalse(names.Contains(ImplementableHierarchyMetadata.OtherIterableDerived));
        Assert.IsTrue(names.Contains(ImplementableHierarchyMetadata.IntIterableBase));
    }

    /// <summary>
    /// The local holding the constructed instance must not collide with a constructor parameter of the same name.
    /// </summary>
    [TestMethod]
    public void ComposableConstructorParameter_DoesNotCollideWithLocal()
    {
        string text = GetAbiClass($"{ImplementableHierarchyMetadata.InstanceParameter}ActivationFactory").ToString();

        StringAssert.Contains(text, "CreateInstance(int instance, object baseInterface, out object innerInterface)");
        StringAssert.Contains(text, $"{ImplementableHierarchyMetadata.InstanceParameter} __instance = CreateInstance(instance);");
        StringAssert.Contains(text, "return __instance;");
    }

    /// <summary>
    /// A removed class is not projected, so it gets no bases either (they would convert to a class that does not exist).
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RemovedClass_GetsNoBases(bool referenceProjection)
    {
        string[] names = GetAbiClassNames(referenceProjection);

        Assert.IsFalse(names.Any(static name => name.StartsWith(ImplementableHierarchyMetadata.Removed, System.StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ReferenceProjection_Compiles()
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection: true);

            _ = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "Projection.dll"),
                referenceProjection: true);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// What an author writes against the generated bases, including converting to a projected ancestor.
    /// </summary>
    [TestMethod]
    public void Implementations_CompileAgainstTheGeneratedBases()
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection: false);
            string projection = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "Projection.dll"));

            const string consumer = """
                namespace MyApp
                {
                    public sealed class MyIterator : ABI.Contoso.IteratorDerived
                    {
                        public override void ClosableBaseMethod() { }

                        public override void Dispose() { }

                        public override void IteratorDerivedMethod() { }

                        public override int Current => 0;

                        public override bool MoveNext() => false;

                        public override void Reset() { }
                    }

                    public sealed class MyIterable : ABI.Contoso.SameIterableDerived
                    {
                        public override void IntIterableBaseMethod() { }

                        public override System.Collections.Generic.IEnumerator<int> GetEnumerator() => null!;

                        public override void SameIterableDerivedMethod() { }
                    }

                    public static class Usage
                    {
                        public static void Use(MyIterator iterator, MyIterable iterable)
                        {
                            Contoso.IteratorDerived derived = iterator;
                            Contoso.ClosableBase closable = iterator;
                            Contoso.IntIterableBase intIterable = iterable;

                            TakeClosable(iterator);
                            TakeIntIterable(iterable);
                        }

                        private static void TakeClosable(Contoso.ClosableBase value) { }

                        private static void TakeIntIterable(Contoso.IntIterableBase value) { }
                    }
                }
                """;

            _ = ProjectionWriterRunner.CompileSources(
                [consumer],
                Path.Combine(directory, "Consumer.dll"),
                additionalReferences: [projection]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string[] GetMethodNames(ClassDeclarationSyntax type)
    {
        return [.. type.Members.OfType<MethodDeclarationSyntax>().Where(static method => method.ExplicitInterfaceSpecifier is null).Select(static method => method.Identifier.ValueText)];
    }

    private static ClassDeclarationSyntax GetAbiClass(string name)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            ClassDeclarationSyntax[] classes = [.. GetAbiClasses(Generate(directory, referenceProjection: false)).Where(type => type.Identifier.ValueText == name)];

            Assert.HasCount(1, classes, $"Expected exactly one 'ABI.Contoso.{name}'.");

            return classes[0];
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string[] GetAbiClassNames(bool referenceProjection)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            return [.. GetAbiClasses(Generate(directory, referenceProjection)).Select(static type => type.Identifier.ValueText)];
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Gets the abstract classes in the <c>ABI.Contoso</c> namespace, i.e. the implementable bases.
    /// </summary>
    private static ClassDeclarationSyntax[] GetAbiClasses(string output)
    {
        return [.. Directory.GetFiles(output, "*.cs")
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot())
            .SelectMany(static root => root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Where(static type =>
                type.Parent is BaseNamespaceDeclarationSyntax { Name: var ns } &&
                ns.ToString() == "ABI.Contoso" &&
                type.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                !type.Modifiers.Any(SyntaxKind.StaticKeyword))];
    }

    private static string Generate(string directory, bool referenceProjection)
    {
        string output = Path.Combine(directory, referenceProjection ? "reference" : "generated");

        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [ImplementableHierarchyMetadata.Create(directory), ImplementableHierarchyMetadata.FindWindowsFoundationWinmd()],
            OutputFolder = output,
            Include = ["Contoso"],
            ImplementWinMDTypes = true,
            ReferenceProjection = referenceProjection
        });

        return output;
    }
}

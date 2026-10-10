// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Covers how the factory bases generated for implementing runtime classes in C# expose activation.
/// </summary>
/// <remarks>
/// Constructing a class without arguments is always <c>ActivateInstance</c>, declared returning the implementation's
/// base, whether the class is sealed or unsealed. In metadata, an unsealed class is only activatable through its
/// composable factory, which a caller activating it by class name (through <c>IActivationFactory</c>) does not use.
/// Composition is not supported for a class implemented in C#, so a public parameterless constructor (or none at
/// all) backs default activation instead.
/// </remarks>
[TestClass]
public class Test_ImplementableActivation
{
    [TestMethod]
    public void SealedDefaultConstructor_IsTypedActivateInstance()
    {
        string text = GetFactoryBase(ActivationMetadata.SealedDefault).ToString();

        StringAssert.Contains(text, "return ActivateInstance();");
        StringAssert.Contains(text, $"public abstract {ActivationMetadata.SealedDefault} ActivateInstance();");
        Assert.IsTrue(HasDefaultActivationOnly(ActivationMetadata.SealedDefault));
    }

    /// <summary>
    /// Constructors taking arguments return the implementation's base too, while statics keep the projected class.
    /// </summary>
    [TestMethod]
    public void SealedFactoryConstructor_ReturnsImplementationBase()
    {
        string text = GetFactoryBase(ActivationMetadata.SealedWithFactory).ToString();

        StringAssert.Contains(text, $"public abstract {ActivationMetadata.SealedWithFactory} ActivateInstance();");
        StringAssert.Contains(text, $"public abstract {ActivationMetadata.SealedWithFactory} CreateWithValue(int value0);");
        StringAssert.Contains(text, "return CreateWithValue(value0);");
        StringAssert.Contains(text, $"public abstract global::Contoso.{ActivationMetadata.SealedWithFactory} GetDefault();");
        Assert.IsFalse(HasDefaultActivationOnly(ActivationMetadata.SealedWithFactory));
    }

    [TestMethod]
    public void NoConstructor_IsDefaultActivatableOnly()
    {
        ClassDeclarationSyntax factory = GetFactoryBase(ActivationMetadata.NoConstructor);

        Assert.IsTrue(ImplementsActivationFactory(factory));
        Assert.IsTrue(HasDefaultActivationOnly(ActivationMetadata.NoConstructor));
        StringAssert.Contains(factory.ToString(), $"public abstract {ActivationMetadata.NoConstructor} ActivateInstance();");
    }

    /// <summary>
    /// The composable constructor calls <c>ActivateInstance</c>, so both ways to activate the class without
    /// arguments share one implementation.
    /// </summary>
    [TestMethod]
    public void ParameterlessConstructor_ForwardsToActivateInstance()
    {
        ClassDeclarationSyntax factory = GetFactoryBase(ActivationMetadata.ParameterlessConstructor);
        string text = factory.ToString();

        Assert.IsTrue(ImplementsActivationFactory(factory));
        Assert.IsTrue(HasDefaultActivationOnly(ActivationMetadata.ParameterlessConstructor));
        StringAssert.Contains(text, $"{ActivationMetadata.ParameterlessConstructor} instance = ActivateInstance();");
        StringAssert.Contains(text, $"public abstract {ActivationMetadata.ParameterlessConstructor} ActivateInstance();");
        Assert.IsFalse(text.Contains("CreateInstance();", StringComparison.Ordinal));
    }

    /// <summary>
    /// A class that is only constructed with arguments is not meant to be activated without them.
    /// </summary>
    [TestMethod]
    public void ParameterizedConstructorOnly_IsNotDefaultActivatable()
    {
        ClassDeclarationSyntax factory = GetFactoryBase(ActivationMetadata.ParameterizedConstructor);

        Assert.IsFalse(ImplementsActivationFactory(factory));
        Assert.IsFalse(HasDefaultActivationOnly(ActivationMetadata.ParameterizedConstructor));
        Assert.IsFalse(factory.ToString().Contains("ActivateInstance", StringComparison.Ordinal));
    }

    /// <summary>
    /// A protected constructor is only there for derived types, so it does not back activation.
    /// </summary>
    [TestMethod]
    public void ProtectedConstructorOnly_IsNotDefaultActivatable()
    {
        ClassDeclarationSyntax factory = GetFactoryBase(ActivationMetadata.ProtectedConstructor);

        Assert.IsFalse(ImplementsActivationFactory(factory));
        Assert.IsFalse(HasDefaultActivationOnly(ActivationMetadata.ProtectedConstructor));
        StringAssert.Contains(factory.ToString(), $"protected abstract {ActivationMetadata.ProtectedConstructor} CreateInstance();");
    }

    /// <summary>
    /// Each class is recorded once: the build tools find its bases by name.
    /// </summary>
    [TestMethod]
    public void ReferenceProjection_RecordsImplementableClassesOnce()
    {
        (string Key, string Value)[] entries = GetReferenceMetadata();

        CollectionAssert.Contains(entries, ("CsWinRT.ImplementableClass.v1", $"Contoso.{ActivationMetadata.NoConstructor}"));
        CollectionAssert.AllItemsAreUnique(entries);
        Assert.IsTrue(entries.All(static entry => entry.Key is "CsWinRT.ImplementableClass.v1"));
    }

    [TestMethod]
    public void ImplementationProjection_DoesNotRecordImplementableClasses()
    {
        Assert.IsFalse(GetReferenceMetadata(referenceProjection: false).Any(static entry => entry.Key.StartsWith("CsWinRT.ImplementableClass", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Projection_Compiles(bool referenceProjection)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionActivationTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection);

            _ = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "Projection.dll"),
                referenceProjection);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// What an author (or the source generator, for the classes it covers) writes against each shape.
    /// </summary>
    [TestMethod]
    public void Factories_CompileAgainstTheGeneratedBases()
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionActivationTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection: false);
            string projection = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "Projection.dll"));

            const string consumer = """
                namespace MyApp
                {
                    public sealed class Sealed : ABI.Contoso.SealedWidget
                    {
                        public override void DoStuff() { }
                    }

                    public sealed class SealedFactory : ABI.Contoso.SealedWidgetActivationFactory
                    {
                        public override ABI.Contoso.SealedWidget ActivateInstance() => new Sealed();
                    }

                    public sealed class SealedWithFactory : ABI.Contoso.SealedFactoryWidget
                    {
                        public override void DoStuff() { }
                    }

                    public sealed class SealedWithFactoryFactory : ABI.Contoso.SealedFactoryWidgetActivationFactory
                    {
                        public override ABI.Contoso.SealedFactoryWidget ActivateInstance() => new SealedWithFactory();

                        public override ABI.Contoso.SealedFactoryWidget CreateWithValue(int value0) => new SealedWithFactory();

                        public override Contoso.SealedFactoryWidget GetDefault() => null!;
                    }

                    public sealed class NoConstructor : ABI.Contoso.NoConstructorWidget
                    {
                        public override void DoStuff() { }
                    }

                    public sealed class NoConstructorFactory : ABI.Contoso.NoConstructorWidgetActivationFactory
                    {
                        public override ABI.Contoso.NoConstructorWidget ActivateInstance() => new NoConstructor();
                    }

                    public sealed class Parameterless : ABI.Contoso.ParameterlessWidget
                    {
                        public override void DoStuff() { }
                    }

                    public sealed class ParameterlessFactory : ABI.Contoso.ParameterlessWidgetActivationFactory
                    {
                        public override ABI.Contoso.ParameterlessWidget ActivateInstance() => new Parameterless();
                    }

                    public sealed class Parameterized : ABI.Contoso.ParameterizedWidget
                    {
                        public override void DoStuff() { }
                    }

                    public sealed class ParameterizedFactory : ABI.Contoso.ParameterizedWidgetActivationFactory
                    {
                        protected override ABI.Contoso.ParameterizedWidget CreateInstance(int value0) => new Parameterized();
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

    private static ClassDeclarationSyntax GetFactoryBase(string className, bool referenceProjection = false)
    {
        return GetAbiClass($"{className}ActivationFactory", referenceProjection);
    }

    private static ClassDeclarationSyntax GetAbiClass(string name, bool referenceProjection = false)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionActivationTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection);

            ClassDeclarationSyntax[] classes = Directory.GetFiles(output, "*.cs")
                .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot())
                .SelectMany(static root => root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                .Where(type => type.Identifier.ValueText == name &&
                               type.Parent is BaseNamespaceDeclarationSyntax { Name: var ns } && ns.ToString() == "ABI.Contoso")
                .ToArray();

            Assert.HasCount(1, classes, $"Expected exactly one 'ABI.Contoso.{name}'.");

            return classes[0];
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string Generate(string directory, bool referenceProjection)
    {
        string output = Path.Combine(directory, referenceProjection ? "reference" : "generated");

        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [ActivationMetadata.Create(directory)],
            OutputFolder = output,
            Include = ["Contoso"],
            ImplementWinMDTypes = true,
            ReferenceProjection = referenceProjection
        });

        return output;
    }

    private static bool ImplementsActivationFactory(ClassDeclarationSyntax factory)
    {
        return factory.BaseList?.Types.Any(static type => type.ToString().EndsWith(".IActivationFactory", StringComparison.Ordinal)) == true;
    }

    /// <summary>
    /// Checks whether <c>ActivateInstance()</c> is the only member left to implement on the factory base, which is
    /// how the source generator decides it can supply the factory itself.
    /// </summary>
    private static bool HasDefaultActivationOnly(string className)
    {
        MemberDeclarationSyntax[] abstractMembers = GetFactoryBase(className, referenceProjection: true).Members
            .Where(static member => member.Modifiers.Any(SyntaxKind.AbstractKeyword))
            .ToArray();

        return abstractMembers is [MethodDeclarationSyntax { Identifier.ValueText: "ActivateInstance", ParameterList.Parameters.Count: 0 }];
    }

    /// <summary>
    /// Reads the <c>[WindowsRuntimeReferenceAssemblyMetadata]</c> entries a projection records.
    /// </summary>
    private static (string Key, string Value)[] GetReferenceMetadata(bool referenceProjection = true)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionActivationTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection);

            return Directory.GetFiles(output, "*.cs")
                .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"WindowsRuntimeReferenceAssemblyMetadata(?:Attribute)?\(""([^""]*)"", ""([^""]*)""\)"))
                .Select(static match => (match.Groups[1].Value, match.Groups[2].Value))
                .ToArray();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

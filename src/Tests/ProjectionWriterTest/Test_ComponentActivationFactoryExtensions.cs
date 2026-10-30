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

/// <summary>
/// Tests for extending the activation factories generated for authored components with an abstract
/// <c>[WindowsRuntimeActivationFactory]</c> class, which the generated factory then derives from.
/// </summary>
[TestClass]
public class Test_ComponentActivationFactoryExtensions
{
    [TestMethod]
    public void Extension_IsTheGeneratedFactoryBaseClass()
    {
        string baseline = GenerateComponentFactories("");
        string source = GenerateComponentFactories("""
            [WindowsRuntimeActivationFactory(typeof(Contoso.User))]
            public abstract class UserFactoryExtensions
            {
            }
            """);

        // The extension comes first (it is the base class), followed by everything the factory implemented anyway
        Assert.AreEqual(
            $"global::Extensions.UserFactoryExtensions, {GetBaseList(baseline, "UserServerActivationFactory")}",
            GetBaseList(source, "UserServerActivationFactory"));

        // Other factories are unaffected
        Assert.AreEqual(GetBaseList(baseline, "User2ServerActivationFactory"), GetBaseList(source, "User2ServerActivationFactory"));
    }

    [TestMethod]
    public void NestedExtension_IsReferencedByItsQualifiedName()
    {
        string source = GenerateComponentFactories("""
            public static class Outer
            {
                [WindowsRuntimeActivationFactory(typeof(Contoso.User))]
                public abstract class UserFactoryExtensions
                {
                    protected UserFactoryExtensions()
                    {
                    }
                }
            }
            """);

        StringAssert.StartsWith(GetBaseList(source, "UserServerActivationFactory"), "global::Extensions.Outer.UserFactoryExtensions, ");
    }

    [TestMethod]
    public void ExtensionWithKeywordNames_IsReferencedByItsEscapedQualifiedName()
    {
        // Each part of the name is escaped on its own, whether it is a namespace, a containing type or the type itself
        string source = GenerateComponentFactories("""
            namespace @event
            {
                public static class @class
                {
                    [WindowsRuntimeActivationFactory(typeof(Contoso.User))]
                    public abstract class @void
                    {
                        protected @void()
                        {
                        }
                    }
                }
            }
            """);

        StringAssert.StartsWith(GetBaseList(source, "UserServerActivationFactory"), "global::Extensions.@event.@class.@void, ");
    }

    [TestMethod]
    public void NonAbstractFactory_IsNotAnExtension()
    {
        // A complete activation factory (as written when implementing a runtime class declared in existing metadata)
        string source = GenerateComponentFactories("""
            [WindowsRuntimeActivationFactory(typeof(Contoso.User))]
            public sealed class UserFactory
            {
            }
            """);

        StringAssert.StartsWith(GetBaseList(source, "UserServerActivationFactory"), "global::WindowsRuntime.InteropServices.IActivationFactory");
    }

    [TestMethod]
    public void NoImplementationAssemblies_FactoriesAreUnchanged()
    {
        // Without the managed implementation assemblies there is nothing to look extensions up in
        string source = GenerateComponentFactories(declarations: null);

        StringAssert.StartsWith(GetBaseList(source, "UserServerActivationFactory"), "global::WindowsRuntime.InteropServices.IActivationFactory");
    }

    [TestMethod]
    [DataRow("internal abstract class UserFactoryExtensions { }")]
    [DataRow("public abstract class UserFactoryExtensions<T> { }")]
    [DataRow("public abstract class UserFactoryExtensions { private protected UserFactoryExtensions() { } }")]
    [DataRow("internal static class Outer { [WindowsRuntimeActivationFactory(typeof(Contoso.User))] public abstract class UserFactoryExtensions { } }")]
    public void InaccessibleExtension_ReportsDiagnostic(string declaration)
    {
        string attribute = declaration.Contains("[WindowsRuntimeActivationFactory", StringComparison.Ordinal)
            ? ""
            : "[WindowsRuntimeActivationFactory(typeof(Contoso.User))]";

        Exception exception = Assert.Throws<Exception>(() => GenerateComponentFactories($"{attribute} {declaration}"));

        StringAssert.Contains(exception.ToString(), "CSWINRTPROJECTIONGEN5023");
        StringAssert.Contains(exception.ToString(), "Contoso.User");
    }

    [TestMethod]
    public void DuplicateExtensions_ReportDiagnostic()
    {
        Exception exception = Assert.Throws<Exception>(() => GenerateComponentFactories("""
            [WindowsRuntimeActivationFactory(typeof(Contoso.User))]
            public abstract class FirstUserFactoryExtensions
            {
            }

            [WindowsRuntimeActivationFactory(typeof(Contoso.User))]
            public abstract class SecondUserFactoryExtensions
            {
            }
            """));

        StringAssert.Contains(exception.ToString(), "CSWINRTPROJECTIONGEN5024");
        StringAssert.Contains(exception.ToString(), "FirstUserFactoryExtensions");
        StringAssert.Contains(exception.ToString(), "SecondUserFactoryExtensions");
    }

    /// <summary>
    /// Builds a component implementation assembly declaring the given types (unless they are <see langword="null"/>),
    /// then generates the component projection for the test metadata with it, and returns all the generated sources.
    /// </summary>
    private static string GenerateComponentFactories(string? declarations)
    {
        string directory = Directory.CreateTempSubdirectory("ComponentActivationFactoryExtensionsTest_").FullName;

        try
        {
            string[] implementationPaths = declarations is null ? [] : [ProjectionWriterRunner.CompileSources(
                [$$"""
                namespace Contoso
                {
                    public sealed class User;

                    public sealed class User2;
                }

                namespace Extensions
                {
                    using WindowsRuntime.InteropServices;

                    {{declarations}}
                }
                """],
                Path.Combine(directory, "Component.dll"))];

            string outputFolder = Path.Combine(directory, "Generated");

            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [TypeFilteringMetadata.Create(directory)],
                OutputFolder = outputFolder,
                OutputAssemblyName = "WinRT.Component",
                Include = ["Contoso"],
                Exclude = ["Contoso.UserProfile"],
                Component = true,
                ComponentImplementationAssemblyPaths = implementationPaths
            });

            return string.Join(Environment.NewLine, Directory.GetFiles(outputFolder, "*.cs").Order(StringComparer.Ordinal).Select(File.ReadAllText));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Gets the base list of a generated class, as written.
    /// </summary>
    private static string GetBaseList(string source, string className)
    {
        ClassDeclarationSyntax type = CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == className);

        return string.Join(", ", type.BaseList!.Types.Select(static baseType => baseType.ToString()));
    }
}

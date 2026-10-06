// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;
using WindowsRuntime.ProjectionWriter;
using WindowsRuntime.ProjectionWriter.Helpers;

namespace ProjectionWriterTest;

[TestClass]
public class Test_StringNullability
{
    [TestMethod]
    [DataRow(true, NullableContextOptions.Disable)]
    [DataRow(true, NullableContextOptions.Enable)]
    [DataRow(false, NullableContextOptions.Disable)]
    [DataRow(false, NullableContextOptions.Enable)]
    public void Strings_AreNonNullableOnlyInReferenceProjections(bool referenceProjection, NullableContextOptions nullableContextOptions)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionStringNullability_").FullName;
        try
        {
            string metadata = ArrayPropertyMetadata.Create(directory, exclusiveTo: false);
            ModuleDefinition module = ModuleDefinition.FromFile(metadata);
            TypeDefinition iface = module.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget2");
            MethodDefinition echo = new("Echo",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                MethodSignature.CreateInstance(module.CorLibTypeFactory.String,
                    [module.CorLibTypeFactory.String, module.CorLibTypeFactory.Object,
                    new ByReferenceTypeSignature(module.CorLibTypeFactory.String)]));
            echo.ParameterDefinitions.Add(new ParameterDefinition(1, "text", ParameterAttributes.In));
            echo.ParameterDefinitions.Add(new ParameterDefinition(2, "other", ParameterAttributes.In));
            echo.ParameterDefinitions.Add(new ParameterDefinition(3, "result", ParameterAttributes.Out));
            iface.Methods.Add(echo);
            MethodDefinition invoke = module.TopLevelTypes.Single(type => type.FullName == "Contoso.ChangedHandler")
                .Methods.Single(method => method.Name == "Invoke");
            invoke.Signature = MethodSignature.CreateInstance(module.CorLibTypeFactory.String, [module.CorLibTypeFactory.String, module.CorLibTypeFactory.Object]);
            module.Write(metadata);
            string output = Path.Combine(directory, "sources");
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [metadata],
                OutputFolder = output,
                Include = ["Contoso"],
                ReferenceProjection = referenceProjection
            });
            string[] sources = Directory.GetFiles(output, "*.cs").Select(File.ReadAllText).ToArray();
            string assembly = ProjectionWriterRunner.CompileSources(sources, Path.Combine(directory, "Projection.dll"), referenceProjection,
                nullableContextOptions: nullableContextOptions);
            CSharpCompilation consumer = CreateCompilation([""], assembly);
            INamedTypeSymbol widget = consumer.GetTypeByMetadataName("Contoso.Widget")!;
            IPropertySymbol label = widget.GetMembers("Label").OfType<IPropertySymbol>().Single();
            NullableAnnotation expected = referenceProjection ? NullableAnnotation.NotAnnotated : NullableAnnotation.None;
            Assert.AreEqual(expected, label.NullableAnnotation);
            Assert.AreEqual(expected, label.GetMethod!.ReturnNullableAnnotation);
            Assert.AreEqual(expected, label.SetMethod!.Parameters[0].NullableAnnotation);

            IPropertySymbol names = widget.GetMembers("Names").OfType<IPropertySymbol>().Single();
            Assert.AreEqual(NullableAnnotation.None, names.NullableAnnotation, "The array itself must remain oblivious.");
            Assert.AreEqual(expected, ((IArrayTypeSymbol)names.Type).ElementNullableAnnotation);
            IMethodSymbol acceptNames = widget.GetMembers("AcceptNames").OfType<IMethodSymbol>().Single();
            Assert.AreEqual(expected, ((INamedTypeSymbol)acceptNames.Parameters[0].Type).TypeArgumentNullableAnnotations[0]);
            IPropertySymbol objects = widget.GetMembers("Objects").OfType<IPropertySymbol>().Single();
            Assert.AreEqual(NullableAnnotation.None, objects.NullableAnnotation);
            Assert.AreEqual(NullableAnnotation.None, ((IArrayTypeSymbol)objects.Type).ElementNullableAnnotation);
            IFieldSymbol text = consumer.GetTypeByMetadataName("Contoso.Payload")!.GetMembers("Text").OfType<IFieldSymbol>().Single();
            Assert.AreEqual(expected, text.NullableAnnotation);
            IMethodSymbol echoSymbol = widget.GetMembers("Echo").OfType<IMethodSymbol>().Single();
            Assert.AreEqual(expected, echoSymbol.ReturnNullableAnnotation);
            Assert.AreEqual(expected, echoSymbol.Parameters[0].NullableAnnotation);
            Assert.AreEqual(NullableAnnotation.None, echoSymbol.Parameters[1].NullableAnnotation);
            Assert.AreEqual(expected, echoSymbol.Parameters[2].NullableAnnotation);
            IMethodSymbol delegateInvoke = consumer.GetTypeByMetadataName("Contoso.ChangedHandler")!.DelegateInvokeMethod!;
            Assert.AreEqual(expected, delegateInvoke.ReturnNullableAnnotation);
            Assert.AreEqual(expected, delegateInvoke.Parameters[0].NullableAnnotation);
            Assert.AreEqual(NullableAnnotation.None, delegateInvoke.Parameters[1].NullableAnnotation);
            IMethodSymbol constructor = consumer.GetTypeByMetadataName("Contoso.Payload")!.InstanceConstructors
                .Single(method => method.Parameters.Length > 0);
            Assert.AreEqual(expected, constructor.Parameters[1].NullableAnnotation);

            consumer = CreateCompilation(
                ["""
                #nullable enable
                public static class Consumer
                {
                    public static void Use(Contoso.Widget widget)
                    {
                        _ = widget.Label.Length;
                        widget.Label = null;
                        widget.AcceptNames(new string?[] { null });
                        widget.Objects = null;
                    }
                }
                """], assembly);
            Diagnostic[] warnings = consumer.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ToArray();
            Assert.AreEqual(referenceProjection ? 2 : 0, warnings.Length, string.Join("\n", warnings.Select(static warning => warning.ToString())));
            Assert.IsFalse(consumer.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(NullableContextOptions.Disable)]
    [DataRow(NullableContextOptions.Enable)]
    public void SdkReferenceProjection_StringAnnotationsSize(NullableContextOptions nullableContextOptions)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionStringSize_").FullName;
        try
        {
            string[] sources = ProjectionWriterRunner.GenerateSources(true, "sdk", "Windows");
            string[] baseline = RemoveStringAnnotations(sources);
            string baselineDirectory = Directory.CreateDirectory(Path.Combine(directory, "baseline")).FullName;
            string annotatedDirectory = Directory.CreateDirectory(Path.Combine(directory, "annotated")).FullName;
            string before = ProjectionWriterRunner.CompileSources(baseline, Path.Combine(baselineDirectory, "Sdk.dll"), referenceProjection: true,
                nullableContextOptions: nullableContextOptions);
            string after = ProjectionWriterRunner.CompileSources(sources, Path.Combine(annotatedDirectory, "Sdk.dll"), referenceProjection: true,
                nullableContextOptions: nullableContextOptions);
            VerifySizeAndSurface($"Windows SDK (including UWP XAML; nullable {nullableContextOptions})", before, after);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string[] RemoveStringAnnotations(string[] sources)
    {
        return sources.Select(static source => source
                .Replace("\n#nullable enable annotations\nstring\n#nullable disable annotations\n", "string", StringComparison.Ordinal)
                .Replace("\n#nullable enable annotations\nstring\n#nullable restore annotations\n", "string", StringComparison.Ordinal)
                .Replace("#nullable disable annotations\n", "", StringComparison.Ordinal)).ToArray();
    }

    private static void VerifySizeAndSurface(string name, string before, string after)
    {
        long beforeSize = new FileInfo(before).Length;
        long afterSize = new FileInfo(after).Length;
        Console.WriteLine($"{name} reference projection: {beforeSize:N0} -> {afterSize:N0} bytes; delta {afterSize - beforeSize:N0} bytes ({100.0 * (afterSize - beforeSize) / beforeSize:F2}%).");
        Assert.IsGreaterThanOrEqualTo(beforeSize, afterSize);
        CSharpCompilation baselineCompilation = CreateCompilation([""], before);
        CSharpCompilation annotatedCompilation = CreateCompilation([""], after);
        VerifyNamespace(
            ((IAssemblySymbol)baselineCompilation.GetAssemblyOrModuleSymbol(baselineCompilation.References.Last())!).GlobalNamespace,
            ((IAssemblySymbol)annotatedCompilation.GetAssemblyOrModuleSymbol(annotatedCompilation.References.Last())!).GlobalNamespace);
    }

    private static void VerifyNamespace(INamespaceSymbol baseline, INamespaceSymbol annotated)
    {
        foreach (INamespaceSymbol ns in baseline.GetNamespaceMembers())
        {
            VerifyNamespace(ns, annotated.GetNamespaceMembers().Single(other => other.Name == ns.Name));
        }
        foreach (INamedTypeSymbol type in baseline.GetTypeMembers().Where(static type => type.DeclaredAccessibility == Accessibility.Public))
        {
            INamedTypeSymbol other = annotated.GetTypeMembers(type.Name, type.Arity).Single();
            ISymbol[] before = type.GetMembers().Where(static member => member.DeclaredAccessibility == Accessibility.Public).ToArray();
            ISymbol[] after = other.GetMembers().Where(static member => member.DeclaredAccessibility == Accessibility.Public).ToArray();
            Assert.HasCount(before.Length, after, type.ToDisplayString());
            for (int i = 0; i < before.Length; i++)
            {
                Assert.AreEqual(before[i].Name, after[i].Name, type.ToDisplayString());
                switch (before[i], after[i])
                {
                    case (IMethodSymbol method, IMethodSymbol otherMethod):
                        VerifyType(method.ReturnType, otherMethod.ReturnType);
                        for (int j = 0; j < method.Parameters.Length; j++)
                        {
                            VerifyType(method.Parameters[j].Type, otherMethod.Parameters[j].Type);
                        }
                        break;
                    case (IPropertySymbol property, IPropertySymbol otherProperty):
                        VerifyType(property.Type, otherProperty.Type);
                        break;
                    case (IFieldSymbol field, IFieldSymbol otherField):
                        VerifyType(field.Type, otherField.Type);
                        break;
                    case (IEventSymbol @event, IEventSymbol otherEvent):
                        VerifyType(@event.Type, otherEvent.Type);
                        break;
                }
            }
        }
    }

    private static void VerifyType(ITypeSymbol baseline, ITypeSymbol annotated)
    {
        Assert.AreEqual(baseline.SpecialType == SpecialType.System_String ? NullableAnnotation.NotAnnotated : baseline.NullableAnnotation,
            annotated.NullableAnnotation, baseline.ToDisplayString());
        switch (baseline, annotated)
        {
            case (IArrayTypeSymbol array, IArrayTypeSymbol other):
                VerifyType(array.ElementType, other.ElementType);
                break;
            case (INamedTypeSymbol named, INamedTypeSymbol other):
                for (int i = 0; i < named.TypeArguments.Length; i++)
                {
                    VerifyType(named.TypeArguments[i], other.TypeArguments[i]);
                }
                break;
        }
    }

    [TestMethod]
    public void RuntimeProjectionStrings_AreAlreadyNonNullable()
    {
        CSharpCompilation compilation = CSharpCompilation.Create("RuntimeContract",
            references: [.. Net100.References.All, MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location)]);
        IMethodSymbol stringable = compilation.GetTypeByMetadataName("Windows.Foundation.IStringable")!
            .GetMembers("ToString").OfType<IMethodSymbol>().Single();
        Assert.AreEqual(NullableAnnotation.NotAnnotated, stringable.ReturnNullableAnnotation);
        foreach (string name in new[] { "Point", "Size", "Rect" })
        {
            IMethodSymbol[] methods = compilation.GetTypeByMetadataName($"Windows.Foundation.{name}")!
                .GetMembers("ToString").OfType<IMethodSymbol>().ToArray();
            foreach (IMethodSymbol method in methods)
            {
                Assert.AreEqual(NullableAnnotation.NotAnnotated, method.ReturnNullableAnnotation);
            }
            IMethodSymbol formattable = methods.Single(static method => method.Parameters.Length == 2);
            Assert.AreEqual(NullableAnnotation.Annotated, formattable.Parameters[0].NullableAnnotation,
                "The .NET IFormattable contract intentionally accepts a null format.");
            Assert.AreEqual(NullableAnnotation.Annotated, formattable.Parameters[1].NullableAnnotation);
        }
    }

    [TestMethod]
    public void StringTokens_PreserveLiteralsIdentifiersAndNullableContexts()
    {
        const string source = """
            #nullable disable annotations
            // string and #nullable enable are not code
            /* string */
            public class C
            {
                public object @string;
                public const string Text = "string \" string";
                public const string Verbatim = @"string "" string";
                #nullable enable
                public string Existing(object Other) => "";
                #nullable disable warnings
                public string StillEnabled(object Other) => "";
                #nullable restore annotations
                public string Restored;
            }
            """;
        string annotated = StringNullability.Annotate(source);
        StringAssert.Contains(annotated, "// string and #nullable enable are not code");
        StringAssert.Contains(annotated, "/* string */");
        StringAssert.Contains(annotated, "object @string;");
        StringAssert.Contains(annotated, "\"string \\\" string\"");
        StringAssert.Contains(annotated, "@\"string \"\" string\"");
        StringAssert.Contains(annotated, "public string Existing(object Other)");
        StringAssert.Contains(annotated, "public string StillEnabled(object Other)");
        StringAssert.Contains(annotated, "string\n#nullable restore annotations\n");
        Assert.IsFalse(CSharpSyntaxTree.ParseText(annotated).GetDiagnostics().Any());
    }

    private static CSharpCompilation CreateCompilation(string[] sources, string reference)
    {
        return CSharpCompilation.Create("Consumer",
            sources.Select(static source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14))),
            [.. Net100.References.All, MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location), MetadataReference.CreateFromFile(reference)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
}

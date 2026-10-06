// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public class Test_StringNullability
{
    private static readonly Lazy<string[]> SdkSources = new(static () => ProjectionWriterRunner.GenerateSources(true, "sdk", "Windows"));
    private static readonly Lazy<string[]> WinUIValueSources = new(static () => ProjectionWriterRunner.GenerateSources(true,
        $"sdk,{ProjectionWriterRunner.GetRequiredFilePath("WinUIMetadataPath")}",
        string.Join(",", new[]
        {
            "Microsoft.UI.Xaml.CornerRadius", "Microsoft.UI.Xaml.Duration", "Microsoft.UI.Xaml.DurationType",
            "Microsoft.UI.Xaml.GridLength", "Microsoft.UI.Xaml.GridUnitType", "Microsoft.UI.Xaml.Thickness",
            "Microsoft.UI.Xaml.Media.Matrix", "Microsoft.UI.Xaml.Media.Media3D.Matrix3D",
            "Microsoft.UI.Xaml.Controls.Primitives.GeneratorPosition", "Microsoft.UI.Xaml.Media.Animation.KeyTime",
            "Microsoft.UI.Xaml.Media.Animation.RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation.RepeatBehaviorType"
        })));

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
            string[] sources = SdkSources.Value;
            string[] baseline = RemoveStringAnnotations(sources);
            string baselineDirectory = Directory.CreateDirectory(Path.Combine(directory, "baseline")).FullName;
            string annotatedDirectory = Directory.CreateDirectory(Path.Combine(directory, "annotated")).FullName;
            string before = ProjectionWriterRunner.CompileSources(baseline, Path.Combine(baselineDirectory, "Sdk.dll"), referenceProjection: true,
                nullableContextOptions: nullableContextOptions);
            string after = ProjectionWriterRunner.CompileSources(sources, Path.Combine(annotatedDirectory, "Sdk.dll"), referenceProjection: true,
                nullableContextOptions: nullableContextOptions);
            VerifySizeAndSurface($"Windows SDK (including UWP XAML; nullable {nullableContextOptions})", before, after);
            VerifyResourceContracts(CreateCompilation([""], after));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string[] RemoveStringAnnotations(string[] sources)
    {
        return sources.Select(static source => Regex.Replace(source,
                @"\r?\n[ \t]*#nullable enable annotations\r?\n[ \t]*string\r?\n[ \t]*#nullable (?:disable|restore) annotations\r?\n[ \t]*",
                "string ")
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
            if (type.BaseType is { } baseType)
            {
                VerifyType(baseType, other.BaseType!);
            }
            for (int i = 0; i < type.Interfaces.Length; i++)
            {
                VerifyType(type.Interfaces[i], other.Interfaces[i]);
            }
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
        Assert.AreEqual(baseline.SpecialType == SpecialType.System_String && baseline.NullableAnnotation == NullableAnnotation.None
                ? NullableAnnotation.NotAnnotated
                : baseline.NullableAnnotation,
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
    [DataRow(true)]
    [DataRow(false)]
    public void ManagedResourceImplementations_CompileWithNullableWarningsEnabled(bool referenceProjection)
    {
        string directory = Directory.CreateTempSubdirectory("NullableManagedResources_").FullName;
        try
        {
            string[] sdkSources = SdkSources.Value;
            string sdk = ProjectionWriterRunner.CompileSources(sdkSources, Path.Combine(directory, "Sdk.dll"), referenceProjection: true);
            string[] winUISources = WinUIValueSources.Value;
            string[] names = ["Color", "Thickness", "Matrix", "GeneratorPosition"];
            const string header = """
                // <auto-generated/>
                #nullable disable
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using Windows.Foundation;
                using WindowsRuntime;
                using WindowsRuntime.InteropServices;
                """;
            string[] companions = sdkSources.Concat(winUISources).SelectMany(static source => CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
                    .OfType<StructDeclarationSyntax>())
                .Where(type => names.Contains(type.Identifier.ValueText, StringComparer.Ordinal) &&
                    type.Members.OfType<FieldDeclarationSyntax>().Any(static field => !field.Modifiers.Any(SyntaxKind.StaticKeyword)) &&
                    (type.Ancestors().OfType<NamespaceDeclarationSyntax>().First().Name.ToString().StartsWith("Windows.UI", StringComparison.Ordinal) ||
                        type.Ancestors().OfType<NamespaceDeclarationSyntax>().First().Name.ToString().StartsWith("Microsoft.UI.Xaml", StringComparison.Ordinal)))
                .Select(type => header + "\nnamespace " + type.Ancestors().OfType<NamespaceDeclarationSyntax>().First().Name +
                    "\n{\n" + type.WithAttributeLists(default).ToFullString() + "\n}").ToArray();
            Assert.HasCount(7, companions);
            string[] enums = winUISources.SelectMany(static source => CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
                    .OfType<EnumDeclarationSyntax>())
                .Where(static type => type.Identifier.ValueText is "DurationType" or "GridUnitType" or "RepeatBehaviorType")
                .Select(type => header + "\nnamespace " + type.Ancestors().OfType<NamespaceDeclarationSyntax>().First().Name +
                    "\n{\n" + type.WithAttributeLists(default).ToFullString() + "\n}").ToArray();
            Assert.HasCount(3, enums);
            string[] resources = typeof(ProjectionWriter).Assembly.GetManifestResourceNames()
                .Where(static name => name.Contains(".Resources.Additions.", StringComparison.Ordinal) &&
                    !name.Contains("SynchronizationContext", StringComparison.Ordinal) &&
                    !name.Contains("WindowsRuntimeStorageExtensions", StringComparison.Ordinal))
                .Select(static name =>
                {
                    using Stream stream = typeof(ProjectionWriter).Assembly.GetManifestResourceStream(name)!;
                    using StreamReader reader = new(stream);
                    // Registration attributes require generated ABI types; the managed bodies are tested independently.
                    string source = Regex.Replace(reader.ReadToEnd(), @"(?m)^[ \t]*\[ABI\.[^\r\n]+\][ \t]*\r?$", "");
                    return header + "\n" + source;
                }).ToArray();
            CSharpCompilation compilation = CSharpCompilation.Create("ManagedResources",
                resources.Concat(companions).Concat(enums).Select(source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14,
                    preprocessorSymbols: referenceProjection ? ["CSWINRT_REFERENCE_PROJECTION"] : []))),
                [.. Net100.References.All, MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location),
                    MetadataReference.CreateFromFile(sdk)],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
            Diagnostic[] failures = compilation.GetDiagnostics().Where(static diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error || diagnostic.Id.StartsWith("CS86", StringComparison.Ordinal) ||
                diagnostic.Id.StartsWith("CS87", StringComparison.Ordinal)).ToArray();
            Assert.IsEmpty(failures, string.Join("\n", failures.Select(static diagnostic => diagnostic.ToString())));
            foreach (string name in new[]
            {
                "Windows.UI.Color", "Windows.UI.Xaml.Media.Matrix", "Windows.UI.Xaml.Media.Media3D.Matrix3D",
                "Windows.UI.Xaml.Media.Animation.RepeatBehavior", "Microsoft.UI.Xaml.Media.Matrix",
                "Microsoft.UI.Xaml.Media.Media3D.Matrix3D", "Microsoft.UI.Xaml.Media.Animation.RepeatBehavior"
            })
            {
                IMethodSymbol format = compilation.GetTypeByMetadataName(name)!.GetMembers().OfType<IMethodSymbol>()
                    .Single(static method => method.ExplicitInterfaceImplementations.Any(
                        implementation => implementation.ContainingType.Name == "IFormattable"));
                Assert.AreEqual(NullableAnnotation.NotAnnotated, format.ReturnNullableAnnotation, name);
                Assert.AreEqual(NullableAnnotation.Annotated, format.Parameters[0].NullableAnnotation, name);
                Assert.AreEqual(NullableAnnotation.Annotated, format.Parameters[1].NullableAnnotation, name);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void ReferenceBearingResources_UseExplicitNullableContextsWithoutPlaceholders()
    {
        string[] names = typeof(ProjectionWriter).Assembly.GetManifestResourceNames()
            .Where(static name => name.Contains(".Resources.", StringComparison.Ordinal)).ToArray();
        Assert.IsNotEmpty(names);
        foreach (string name in names)
        {
            using Stream stream = typeof(ProjectionWriter).Assembly.GetManifestResourceStream(name)!;
            using StreamReader reader = new(stream);
            string source = reader.ReadToEnd();
            Assert.IsFalse(source.Contains("/*$string$*/", StringComparison.Ordinal), name);
            Assert.IsFalse(source.Contains("throw null;", StringComparison.Ordinal), name);
            if (name.EndsWith(".Base.AssemblyAttributes.cs", StringComparison.Ordinal) ||
                name.EndsWith(".Base.ReferenceInterfaceEntries.cs", StringComparison.Ordinal))
            {
                Assert.IsFalse(source.Contains("#nullable", StringComparison.Ordinal), name);
                continue;
            }
            StringAssert.Contains(source, "#nullable enable", name);
            SyntaxNode root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14)).GetRoot();
            NullableDirectiveTriviaSyntax restore = root.DescendantTrivia(descendIntoTrivia: true)
                .Select(static trivia => trivia.GetStructure()).OfType<NullableDirectiveTriviaSyntax>().Last();
            Assert.AreEqual("restore", restore.SettingToken.ValueText, name);
            Assert.IsTrue(root.DescendantNodes().OfType<MethodDeclarationSyntax>().All(method => method.Span.End < restore.SpanStart), name);
        }
    }

    private static void VerifyResourceContracts(CSharpCompilation compilation)
    {
        foreach (string name in new[] { "Windows.UI.Color", "Windows.UI.Xaml.Media.Matrix", "Windows.UI.Xaml.Media.Media3D.Matrix3D",
            "Windows.UI.Xaml.Media.Animation.RepeatBehavior" })
        {
            INamedTypeSymbol type = compilation.GetTypeByMetadataName(name)!;
            IMethodSymbol format = type.GetMembers("ToString").OfType<IMethodSymbol>().Single(static method => method.Parameters.Length == 1);
            Assert.AreEqual(NullableAnnotation.NotAnnotated, format.ReturnNullableAnnotation, name);
            Assert.AreEqual(NullableAnnotation.Annotated, format.Parameters[0].NullableAnnotation, name);
        }
        foreach (string name in new[] { "Windows.UI.Xaml.CornerRadius", "Windows.UI.Xaml.Duration", "Windows.UI.Xaml.GridLength",
            "Windows.UI.Xaml.Media.Animation.KeyTime", "Windows.UI.Xaml.Media.Animation.RepeatBehavior", "Windows.UI.Xaml.Media.Media3D.Matrix3D" })
        {
            IMethodSymbol equals = compilation.GetTypeByMetadataName(name)!.GetMembers("Equals").OfType<IMethodSymbol>()
                .Single(static method => method.Parameters.Length == 1 && method.Parameters[0].Type.SpecialType == SpecialType.System_Object);
            Assert.AreEqual(NullableAnnotation.Annotated, equals.Parameters[0].NullableAnnotation, name);
        }
        INamedTypeSymbol storage = compilation.GetTypeByMetadataName("Windows.Storage.WindowsRuntimeStorageExtensions")!;
        Assert.IsTrue(storage.GetMembers("CreateSafeFileHandle").OfType<IMethodSymbol>()
            .All(static method => method.ReturnNullableAnnotation == NullableAnnotation.Annotated));
        INamedTypeSymbol dispatch = compilation.GetTypeByMetadataName("Windows.System.DispatcherQueueSynchronizationContext")!;
        Assert.AreEqual(NullableAnnotation.Annotated, dispatch.GetMembers("Post").OfType<IMethodSymbol>().Single().Parameters[1].NullableAnnotation);
        (string Type, string[] Methods)[] interopReturns =
        [
            ("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManagerExtensions", ["GetForWindow"]),
            ("Windows.Graphics.Printing.PrintManagerExtensions", ["GetForWindow", "ShowPrintUIForWindowAsync"]),
            ("Windows.Media.SystemMediaTransportControlsExtensions", ["GetForWindow"]),
            ("Windows.Media.PlayTo.PlayToManagerExtensions", ["GetForWindow"]),
            ("Windows.Security.Credentials.UI.UserConsentVerifierExtensions", ["RequestVerificationForWindowAsync"]),
            ("Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManagerExtensions",
                ["RequestTokenForWindowAsync", "RequestTokenWithWebAccountForWindowAsync"]),
            ("Windows.UI.ApplicationSettings.AccountsSettingsPaneExtensions",
                ["GetForWindow", "ShowManageAccountsForWindowAsync", "ShowAddAccountForWindowAsync"]),
            ("Windows.UI.Input.RadialControllerConfigurationExtensions", ["GetForWindow"]),
            ("Windows.UI.Input.RadialControllerExtensions", ["CreateForWindow"]),
            ("Windows.UI.Input.Core.RadialControllerIndependentInputSourceExtensions", ["CreateForWindow"]),
            ("Windows.UI.Input.Spatial.SpatialInteractionManagerExtensions", ["GetForWindow"]),
            ("Windows.UI.ViewManagement.InputPaneExtensions", ["GetForWindow"]),
            ("Windows.UI.ViewManagement.UIViewSettingsExtensions", ["GetForWindow"]),
            ("Windows.Graphics.Display.DisplayInformationExtensions", ["GetForWindow", "GetForMonitor"]),
            ("Windows.ApplicationModel.DataTransfer.DataTransferManagerExtensions", ["GetForWindow"])
        ];
        foreach ((string type, string[] methods) in interopReturns)
        {
            foreach (string name in methods)
            {
                VerifyExtensionReturn(compilation.GetTypeByMetadataName(type)!, name, NullableAnnotation.NotAnnotated);
            }
        }
        INamedTypeSymbol authentication = compilation.GetTypeByMetadataName(
            "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManagerExtensions")!;
        foreach (string name in new[] { "RequestTokenForWindowAsync", "RequestTokenWithWebAccountForWindowAsync" })
        {
            foreach (IMethodSymbol method in GetExtensionMethods(authentication, name))
            {
                INamedTypeSymbol operation = (INamedTypeSymbol)method.ReturnType;
                Assert.AreEqual(NullableAnnotation.Annotated, operation.TypeArgumentNullableAnnotations.Single(), method.ToDisplayString());
                foreach (IParameterSymbol parameter in method.Parameters.Where(static parameter =>
                    parameter.Type.Name is "WebTokenRequest" or "WebAccount"))
                {
                    Assert.AreEqual(NullableAnnotation.NotAnnotated, parameter.NullableAnnotation, method.ToDisplayString());
                }
            }
        }
    }

    private static void VerifyExtensionReturn(INamedTypeSymbol type, string name, NullableAnnotation annotation)
    {
        IMethodSymbol[] methods = GetExtensionMethods(type, name);
        Assert.IsNotEmpty(methods, type.ToDisplayString());
        foreach (IMethodSymbol method in methods)
        {
            Assert.AreEqual(annotation, method.ReturnNullableAnnotation, method.ToDisplayString());
        }
    }

    private static IMethodSymbol[] GetExtensionMethods(INamedTypeSymbol type, string name)
    {
        return type.GetMembers(name).OfType<IMethodSymbol>()
            .Concat(type.GetTypeMembers().SelectMany(nested => nested.GetMembers(name).OfType<IMethodSymbol>())).ToArray();
    }

    private static CSharpCompilation CreateCompilation(string[] sources, string reference)
    {
        return CSharpCompilation.Create("Consumer",
            sources.Select(static source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14))),
            [.. Net100.References.All, MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location), MetadataReference.CreateFromFile(reference)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
}

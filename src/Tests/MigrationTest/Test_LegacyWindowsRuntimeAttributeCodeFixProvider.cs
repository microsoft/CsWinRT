// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using WindowsRuntime.SourceGenerator.Diagnostics;

namespace WindowsRuntime.Migration.Tests;

/// <summary>
/// Tests legacy attribute migrations against real 3.0 and retained 2.x runtime metadata.
/// </summary>
[TestClass]
public sealed class Test_LegacyWindowsRuntimeAttributeCodeFixProvider
{
    [TestMethod]
    [DataRow("using WinRT; [GeneratedWinRTExposedType] public class Probe;")]
    [DataRow("using WinRT; [WinRTExposedType] public class Probe;")]
    [DataRow("using WinRT; public class Probe { [DynamicWindowsRuntimeCast(typeof(IDisposable))] public void Run() { } }")]
    [DataRow("[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(Probe))] public class Probe;")]
    public async Task RedundantAttribute_RemovesOrPreservesLegacyBranch(string source)
    {
        foreach (bool preserve in new[] { false, true })
        {
            using AdhocWorkspace workspace = new();
            Document document = CreateDocument(workspace, "using System;\n" + source, preserve);
            List<CodeAction> actions = await GetActionsAsync(document);
            Assert.HasCount(1, actions);
            Document changed = await ApplyAsync(document, actions[0]);
            await AssertCompilesAsync(changed.Project);
            Assert.IsEmpty(await GetActionsAsync(changed));

            if (preserve)
            {
                StringAssert.Contains((await changed.GetTextAsync()).ToString(), "#if !CSWINRT3_0");
                await AssertCompilesAsync(AsLegacy(changed.Project));
            }
        }
    }

    [TestMethod]
    [DataRow("WinRT.WinRTRuntimeClassName(\"Contoso.Widget\")", "WindowsRuntime.WindowsRuntimeClassNameAttribute")]
    [DataRow("WinRT.GeneratedBindableCustomProperty", "WindowsRuntime.Xaml.GeneratedCustomPropertyProviderAttribute")]
    [DataRow("WinRT.GeneratedBindableCustomProperty(new[] { \"Value\" }, new[] { typeof(int) })", "WindowsRuntime.Xaml.GeneratedCustomPropertyProviderAttribute")]
    public async Task EquivalentAttribute_ReplacesAndRetainsArguments(string attribute, string replacement)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, $$"""
            using System;
            [{{attribute}}]
            public partial class Probe
            {
                public int Value { get; set; }
                public int this[int index] => index;
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await ApplyAsync(document, actions[0]);
        string source = (await changed.GetTextAsync()).ToString();
        StringAssert.Contains(source, "global::" + replacement);
        StringAssert.Contains(source, attribute);
        await AssertCompilesAsync(changed.Project);
        await AssertCompilesAsync(AsLegacy(changed.Project));
        Assert.IsEmpty(await GetActionsAsync(changed));
    }

    [TestMethod]
    [DataRow("using Old = WinRT.GeneratedWinRTExposedTypeAttribute;\n[Old]")]
    [DataRow("using W = WinRT;\n[W::GeneratedWinRTExposedType]")]
    [DataRow("using W = WinRT;\n[W.GeneratedWinRTExposedType]")]
    public async Task MissingAliases_ConditionalMigrationCompilesOnBothTargets(string prefix)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, prefix + "\npublic class Probe;", preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await ApplyAsync(document, actions[0]);
        await AssertCompilesAsync(changed.Project);
        await AssertCompilesAsync(AsLegacy(changed.Project));
    }

    [TestMethod]
    public async Task FixAll_MixedAttributeListAndComments_CompilesOnBothTargets()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, """
            using System;
            using WinRT;
            // Retain this declaration comment
            [Obsolete("keep"), /* retain this attribute comment */ GeneratedWinRTExposedType, WinRTRuntimeClassName("Contoso.Probe")]
            public class Probe
            {
                [DynamicWindowsRuntimeCast(typeof(IDisposable))]
                public void Run() { }
            }
            """, preserve: true);
        LegacyWindowsRuntimeAttributeCodeFixProvider provider = new();
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(3, actions);
        FixAllContext context = new(document, provider, FixAllScope.Document, actions[0].EquivalenceKey!,
            provider.FixableDiagnosticIds, new MigrationDiagnosticProvider(), CancellationToken.None);
        CodeAction? action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.IsNotNull(action);
        Document changed = await ApplyAsync(document, action);
        string source = (await changed.GetTextAsync()).ToString();
        StringAssert.Contains(source, "Retain this declaration comment");
        StringAssert.Contains(source, "retain this attribute comment");
        await AssertCompilesAsync(changed.Project);
        await AssertCompilesAsync(AsLegacy(changed.Project));
        Assert.IsEmpty(await GetActionsAsync(changed));
    }

    [TestMethod]
    public async Task BoundPolyfill_IsReportedAndRemoved()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, """
            public class Probe
            {
                [WinRT.DynamicWindowsRuntimeCast(typeof(System.IDisposable))]
                public void Run() { }
            }
            """, preserve: false);
        document = document.Project.AddDocument("Probe.DynamicWindowsRuntimeCastAttribute.g.cs", SourceText.From("""
            // <auto-generated />
            namespace Microsoft.CodeAnalysis { internal sealed class EmbeddedAttribute : System.Attribute; }
            namespace WinRT
            {
                [Microsoft.CodeAnalysis.Embedded]
                [System.Diagnostics.Conditional("CSWINRT2_0_POLYFILL_ATTRIBUTES")]
                public sealed class DynamicWindowsRuntimeCastAttribute(System.Type type) : System.Attribute;
            }
            """), filePath: @"C:\Probe.DynamicWindowsRuntimeCastAttribute.g.cs").Project.GetDocument(document.Id)!;
        await AssertCompilesAsync(document.Project);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await ApplyAsync(document, actions[0]);
        Assert.IsEmpty(await GetActionsAsync(changed));
        await AssertCompilesAsync(changed.Project);
    }

    [TestMethod]
    public async Task ProjectedExternalType_UsesNativeExposedReplacement()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace,
            "[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(Contoso.Widget))]", preserve: true);
        Compilation references = (await document.Project.GetCompilationAsync())!;
        CSharpCompilation projection = CSharpCompilation.Create("ContosoProjection",
            [CSharpSyntaxTree.ParseText("""
                [assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssembly]
                namespace Contoso { public sealed class Widget; }
                """)],
            references.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream stream = new();
        var result = projection.Emit(stream);
        Assert.IsTrue(result.Success, string.Join("\n", result.Diagnostics));
        document = document.Project.AddMetadataReference(MetadataReference.CreateFromImage(stream.ToArray())).GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await ApplyAsync(document, actions[0]);
        StringAssert.Contains((await changed.GetTextAsync()).ToString(),
            "global::WindowsRuntime.InteropServices.WindowsRuntimeNativeExposedTypeAttribute");
        await AssertCompilesAsync(changed.Project);
    }

    [TestMethod]
    public async Task ExistingDirectives_PreserveBalancedLegacyBranch()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, """
            #if FEATURE
            [WinRT.GeneratedWinRTExposedType] // keep this trailing comment
            #endif
            public class Probe;
            """, preserve: true);
        document = document.Project.WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp14,
            preprocessorSymbols: ["CSWINRT3_0", "FEATURE"])).GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await ApplyAsync(document, actions[0]);
        StringAssert.Contains((await changed.GetTextAsync()).ToString(), "keep this trailing comment");
        await AssertCompilesAsync(changed.Project);
        await AssertCompilesAsync(AsLegacy(changed.Project));
    }

    [TestMethod]
    public async Task ProjectFixAll_MigratesEachDocumentAndItsImports()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, "using WinRT; [GeneratedWinRTExposedType] public class First;", preserve: true);
        document = document.Project.AddDocument("Second.cs",
            SourceText.From("using WinRT; [WinRTRuntimeClassName(\"Contoso.Second\")] public class Second;"),
            filePath: @"C:\Second.cs").Project.GetDocument(document.Id)!;
        LegacyWindowsRuntimeAttributeCodeFixProvider provider = new();
        List<CodeAction> actions = await GetActionsAsync(document);
        FixAllContext context = new(document, provider, FixAllScope.Project, actions[0].EquivalenceKey!,
            provider.FixableDiagnosticIds, new MigrationDiagnosticProvider(), CancellationToken.None);
        CodeAction? action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.IsNotNull(action);
        Document changed = await ApplyAsync(document, action);
        await AssertCompilesAsync(changed.Project);
        await AssertCompilesAsync(AsLegacy(changed.Project));
        Assert.IsEmpty(await GetDiagnosticsAsync(changed.Project));
    }

    [TestMethod]
    public async Task UnknownTargets_OfferExplicitPreservationAndSingleTargetActions()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, "[WinRT.GeneratedWinRTExposedType] public class Probe;", preserve: false);
        document = document.Project.RemoveAnalyzerConfigDocuments([.. document.Project.AnalyzerConfigDocuments.Select(static item => item.Id)])
            .GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(2, actions);
        StringAssert.Contains(actions[0].Title, "preserve CsWinRT 2.x");
        StringAssert.Contains(actions[1].Title, "3.0 only");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\n  net10.0-windows10.0.26100.1,\n  net10.0-windows10.0.26100.0")]
    public async Task EmptyOrBrokenTargetMetadata_OffersExplicitChoices(string frameworks)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, "[WinRT.GeneratedWinRTExposedType] public class Probe;", preserve: true);
        document = document.Project.RemoveAnalyzerConfigDocuments([.. document.Project.AnalyzerConfigDocuments.Select(static item => item.Id)])
            .AddAnalyzerConfigDocument(".globalconfig", SourceText.From(
                "is_global = true\n\nbuild_property.CsWinRTMigrationTargetFrameworks = " + frameworks),
                filePath: @"C:\.globalconfig").Project.GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(2, actions);
        StringAssert.Contains(actions[0].Title, "preserve CsWinRT 2.x");
    }

    [TestMethod]
    public async Task AllTargetsUseVersion3_DoesNotIntroduceLegacyBranches()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, "[WinRT.GeneratedWinRTExposedType] public class Probe;", preserve: true);
        document = document.Project.RemoveAnalyzerConfigDocuments([.. document.Project.AnalyzerConfigDocuments.Select(static item => item.Id)])
            .AddAnalyzerConfigDocument(".globalconfig", SourceText.From("""
                is_global = true

                build_property.CsWinRTMigrationTargetFrameworks = net10.0-windows10.0.26100.1,net11.0-windows10.0.26100.1
                """), filePath: @"C:\.globalconfig").Project.GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await ApplyAsync(document, actions[0]);
        Assert.IsFalse((await changed.GetTextAsync()).ToString().Contains("#if"));
        await AssertCompilesAsync(changed.Project);
    }

    [TestMethod]
    public async Task ExplicitGeneratedConfiguration_NoDiagnostic()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, "[WinRT.GeneratedWinRTExposedType] public class Probe;", preserve: false);
        document = document.Project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From("""
            root = true

            [*.cs]
            generated_code = true
            """), filePath: @"C:\.editorconfig").Project.GetDocument(document.Id)!;
        Assert.IsEmpty(await GetDiagnosticsAsync(document.Project));
    }

    [TestMethod]
    [DataRow("WinRT.WinRTExposedType(typeof(System.Object))")]
    [DataRow("WinRT.WindowsRuntimeHelperType(typeof(System.Object))")]
    [DataRow("WinRT.WindowsRuntimeType(\"Contoso\")")]
    [DataRow("WinRT.ProjectedRuntimeClass(typeof(System.IDisposable))")]
    [DataRow("WinRT.ObjectReferenceWrapper(\"reference\")")]
    public async Task CustomOrProjectionContract_ReportsReviewWithoutFix(string attribute)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, $"[{attribute}] public class Probe;", preserve: false);
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(document.Project);
        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("CSWINRT2052", diagnostics[0].Id);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("WinRT.ProjectedRuntimeClass(typeof(System.IDisposable))")]
    [DataRow("WinRT.ObjectReferenceWrapper(\"reference\")")]
    public async Task HandwrittenRuntimeObjectSubclass_RequiresReview(string attribute)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace,
            $"[{attribute}] public class Probe : WindowsRuntime.WindowsRuntimeObject;", preserve: false);
        Diagnostic diagnostic = (await GetDiagnosticsAsync(document.Project)).Single();
        Assert.AreEqual("CSWINRT2052", diagnostic.Id);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("ReadOnlyArray", "ReadOnlySpan<int>", "CSWINRT2051")]
    [DataRow("WriteOnlyArray", "Span<int>", "CSWINRT2051")]
    [DataRow("ReadOnlyArray", "int[]", "CSWINRT2053")]
    [DataRow("WriteOnlyArray", "int[]", "CSWINRT2053")]
    public async Task ArrayDirection_RequiresMatchingModernSignature(string attribute, string parameterType, string diagnosticId)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, $$"""
            public class Probe
            {
                public void Run([System.Runtime.InteropServices.WindowsRuntime.{{attribute}}] System.{{parameterType.Replace("int[]", "Int32[]")}} values) { }
            }
            """, preserve: false);
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(document.Project);
        Assert.HasCount(1, diagnostics);
        Assert.AreEqual(diagnosticId, diagnostics[0].Id);

        if (diagnosticId == "CSWINRT2051")
        {
            List<CodeAction> actions = await GetActionsAsync(document);
            await AssertCompilesAsync((await ApplyAsync(document, actions.Single())).Project);
        }
    }

    [TestMethod]
    [DataRow("Test.g.i.cs", "")]
    [DataRow("Test.cs", "// <autogenerated />\n")]
    [DataRow("TemporaryGeneratedFile_Test.cs", "")]
    public async Task GeneratedSource_NoMigration(string fileName, string header)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, header + "[WinRT.GeneratedWinRTExposedType] public class Probe;",
            preserve: false, fileName: fileName);
        Assert.IsEmpty(await GetDiagnosticsAsync(document.Project));
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task LookalikeAndRetainedLegacyTarget_NoDiagnostic()
    {
        using AdhocWorkspace workspace = new();
        Document lookalike = CreateDocument(workspace, """
            namespace WinRT { public sealed class GeneratedWinRTExposedTypeAttribute : System.Attribute; }
            [WinRT.GeneratedWinRTExposedType] public class Probe;
            """, preserve: false);
        Assert.IsEmpty(await GetDiagnosticsAsync(lookalike.Project));

        Document legacy = CreateDocument(workspace, "[WinRT.GeneratedWinRTExposedType] public class Probe;", preserve: true);
        Assert.IsEmpty(await GetDiagnosticsAsync(AsLegacy(legacy.Project)));
    }

    internal static Document CreateDocument(AdhocWorkspace workspace, string source, bool preserve, string fileName = "Test.cs")
    {
        Project project = workspace.AddProject("MigrationFixture", LanguageNames.CSharp)
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp14, preprocessorSymbols: ["CSWINRT3_0"]))
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(Net100.References.All.Append(
                MetadataReference.CreateFromFile(typeof(WindowsRuntimeClassNameAttribute).Assembly.Location)));
        project = project.AddAnalyzerConfigDocument(".globalconfig", SourceText.From($"""
            is_global = true

            build_property.CsWinRTMigrationTargetFrameworks = {(preserve ? "net10.0-windows10.0.26100.1,net8.0-windows10.0.26100.0" : "net10.0-windows10.0.26100.1")}
            """), filePath: @"C:\.globalconfig").Project;
        return project.AddDocument(fileName, SourceText.From(source), filePath: @"C:\" + fileName);
    }

    internal static Project AsLegacy(Project project)
    {
        string path = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(static attribute => attribute.Key == "LegacyWinRTRuntimeAssemblyPath").Value!;
        CSharpParseOptions options = (CSharpParseOptions)project.ParseOptions!;
        return project.WithParseOptions(options.WithPreprocessorSymbols(
                options.PreprocessorSymbolNames.Where(static symbol => symbol != "CSWINRT3_0")))
            .WithMetadataReferences(Net100.References.All.Append(MetadataReference.CreateFromFile(path)));
    }

    internal static async Task AssertCompilesAsync(Project project)
    {
        Compilation compilation = (await project.GetCompilationAsync())!;
        using MemoryStream stream = new();
        var result = compilation.Emit(stream);
        Assert.IsTrue(result.Success, string.Join("\n", result.Diagnostics));
    }

    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Project project)
    {
        Compilation compilation = (await project.GetCompilationAsync())!;
        CompilationWithAnalyzersOptions options = new(project.AnalyzerOptions, null, concurrentAnalysis: true,
            logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: false);
        return await compilation.WithAnalyzers([new LegacyWindowsRuntimeAttributeAnalyzer()], options).GetAnalyzerDiagnosticsAsync();
    }

    private static async Task<List<CodeAction>> GetActionsAsync(Document document)
    {
        List<CodeAction> actions = [];
        LegacyWindowsRuntimeAttributeCodeFixProvider provider = new();
        SyntaxTree? tree = await document.GetSyntaxTreeAsync();

        foreach (Diagnostic diagnostic in (await GetDiagnosticsAsync(document.Project)).Where(
            diagnostic => diagnostic.Location.SourceTree == tree && provider.FixableDiagnosticIds.Contains(diagnostic.Id)))
        {
            await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic,
                (action, _) => actions.Add(action), CancellationToken.None));
        }

        return actions;
    }

    internal static async Task<Document> ApplyAsync(Document document, CodeAction action)
    {
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        return operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
    }

    private sealed class MigrationDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            SyntaxTree? tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return (await GetDiagnosticsAsync(document.Project)).Where(diagnostic => diagnostic.Location.SourceTree == tree);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>([]);
        }

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return await GetDiagnosticsAsync(project);
        }
    }
}

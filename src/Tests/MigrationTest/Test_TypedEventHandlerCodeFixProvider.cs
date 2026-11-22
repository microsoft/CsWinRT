// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Text;
using WindowsRuntime.Migration;

namespace WindowsRuntime.Migration.Tests;

/// <summary>
/// Exercises migration fixes against broken compilations and the resulting .NET delegate.
/// </summary>
[TestClass]
public sealed class Test_TypedEventHandlerCodeFixProvider
{
    [TestMethod]
    [DataRow("Windows.Foundation.TypedEventHandler<object, int>", true)]
    [DataRow("global::Windows.Foundation.TypedEventHandler<object, int>", true)]
    [DataRow("TypedEventHandler<object, int>", true)]
    [DataRow("Foundation.TypedEventHandler<object, int>", true)]
    [DataRow("Foundation::TypedEventHandler<object, int>", true)]
    [DataRow("Foundation::TypedEventHandler<object, int>", false)]
    public async Task MissingLegacyDelegate_FixedCodeCompilesAndInvokes(string typeName, bool importFoundation)
    {
        string source = $$"""
            {{(importFoundation ? "using Windows.Foundation;" : "")}}
            using Foundation = Windows.Foundation;
            namespace Windows.Foundation { public struct Point; }
            public static class Probe
            {
                public static int Run()
                {
                    int result = 0;
                    {{typeName}} handler = (sender, value) => result = value;
                    handler(new object(), 42);
                    return result;
                }
            }
            """;

        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source);
        List<CodeAction> actions = await GetActionsAsync(document);

        Assert.HasCount(1, actions);
        Document fixedDocument = await ApplyAsync(document, actions[0]);
        string fixedSource = (await fixedDocument.GetTextAsync()).ToString();
        StringAssert.Contains(fixedSource, "global::System.EventHandler<object, int>");
        Compilation compilation = (await fixedDocument.Project.GetCompilationAsync())!;
        using MemoryStream stream = new();
        var result = compilation.Emit(stream);
        Assert.IsTrue(result.Success, string.Join("\n", result.Diagnostics));
        Assembly assembly = Assembly.Load(stream.ToArray());
        Assert.AreEqual(42, assembly.GetType("Probe")!.GetMethod("Run")!.Invoke(null, null));
        Assert.IsEmpty(await GetActionsAsync(fixedDocument));
    }

    [TestMethod]
    public async Task AliasInConditionalBranch_PreservesLegacyTarget()
    {
        const string source = """
            #if CSWINRT3_0
            using Handler = Windows.Foundation.TypedEventHandler<object, int>;
            #else
            using Handler = Windows.Foundation.TypedEventHandler<object, int>;
            #endif
            public class Probe { public Handler Value; }
            """;

        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source);
        document = document.Project.AddDocument("Foundation.cs",
            """
            namespace Windows.Foundation
            {
                public struct Point;
            #if !CSWINRT3_0
                public delegate void TypedEventHandler<TSender, TArgs>(TSender sender, TArgs args);
            #endif
            }
            """).Project.GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document fixedDocument = await ApplyAsync(document, actions[0]);
        Assert.IsFalse((await fixedDocument.Project.GetCompilationAsync())!.GetDiagnostics()
            .Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        Project legacyProject = fixedDocument.Project.WithParseOptions(
            new CSharpParseOptions(LanguageVersion.CSharp14));
        Assert.IsFalse((await legacyProject.GetCompilationAsync())!.GetDiagnostics()
            .Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        StringAssert.Contains((await fixedDocument.GetTextAsync()).ToString(),
            "using Handler = Windows.Foundation.TypedEventHandler<object, int>;");
    }

    [TestMethod]
    [DataRow("using Custom; namespace Custom { public struct Point; } public class C { public TypedEventHandler<object, int> Value; }", true, "Test.cs")]
    [DataRow("namespace Windows.Foundation { public struct Point; } public class C { public Windows.Foundation.TypedEventHandler<object, int> Value; }", false, "Test.cs")]
    [DataRow("namespace Windows.Foundation { public struct Point; } public class C { public Windows.Foundation.TypedEventHandler<object, int> Value; }", true, "Test.g.cs")]
    [DataRow("namespace Windows.Foundation { public struct Point; } public class C { public Windows.Foundation.TypedEventHandler<object, int> Value; }", true, "Test.g.i.cs")]
    [DataRow("// <autogenerated/>\nnamespace Windows.Foundation { public struct Point; } public class C { public Windows.Foundation.TypedEventHandler<object, int> Value; }", true, "Test.cs")]
    [DataRow("// <auto-generated/>\nnamespace Windows.Foundation { public struct Point; } public class C { public Windows.Foundation.TypedEventHandler<object, int> Value; }", true, "Test.cs")]
    [DataRow("namespace Custom { public struct Point; } public class C { public Custom.TypedEventHandler<object, int> Value; }", true, "Test.cs")]
    [DataRow("using Windows.Foundation; using Custom; namespace Windows.Foundation { public struct Point; } namespace Custom { public class TypedEventHandler<T>; } public class C { public TypedEventHandler<object, int> Value; }", true, "Test.cs")]
    [DataRow("using Windows.Foundation; using Other = Custom; namespace Windows.Foundation { public struct Point; } namespace Custom { public struct Point; } public class C { public Other::TypedEventHandler<object, int> Value; }", true, "Test.cs")]
    [DataRow("using Windows.Foundation; namespace Windows.Foundation { public struct Point; } public class C { public global::TypedEventHandler<object, int> Value; }", true, "Test.cs")]
    public async Task UnsupportedOrAmbiguousSite_NoFix(string source, bool cswinrt3, string fileName)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, cswinrt3, fileName);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task BoundLegacyDelegate_NoFix()
    {
        const string source = """
            namespace Windows.Foundation { public delegate void TypedEventHandler<TSender, TArgs>(TSender sender, TArgs args); }
            public class Probe { public Windows.Foundation.TypedEventHandler<object, int> Value; }
            """;
        using AdhocWorkspace workspace = new();
        Assert.IsEmpty(await GetActionsAsync(CreateDocument(workspace, source)));
    }

    [TestMethod]
    public async Task FixAll_MultipleMissingDelegates_Compiles()
    {
        const string source = """
            using Windows.Foundation;
            using Foundation = Windows.Foundation;
            namespace Windows.Foundation { public struct Point; }
            public class Probe
            {
                public TypedEventHandler<object, int> First;
                public Foundation::TypedEventHandler<object, string> Second;
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(2, actions);
        TypedEventHandlerCodeFixProvider provider = new();
        FixAllContext context = new(document, provider, FixAllScope.Document, actions[0].EquivalenceKey!,
            provider.FixableDiagnosticIds, new CompilerDiagnosticProvider(), CancellationToken.None);
        CodeAction? action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.IsNotNull(action);
        Document fixedDocument = await ApplyAsync(document, action);
        Assert.IsFalse((await fixedDocument.Project.GetCompilationAsync())!.GetDiagnostics()
            .Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.IsEmpty(await GetActionsAsync(fixedDocument));
    }

    private static Document CreateDocument(
        AdhocWorkspace workspace,
        string source,
        bool cswinrt3 = true,
        string fileName = "Test.cs")
    {
        Project project = workspace.AddProject("MigrationFixture", LanguageNames.CSharp)
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp14,
                preprocessorSymbols: cswinrt3 ? ["CSWINRT3_0"] : []))
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(Net100.References.All);

        return project.AddDocument(fileName, SourceText.From(source));
    }

    private static async Task<List<CodeAction>> GetActionsAsync(Document document)
    {
        List<CodeAction> actions = [];
        Compilation compilation = (await document.Project.GetCompilationAsync())!;
        SyntaxTree tree = (await document.GetSyntaxTreeAsync())!;
        TypedEventHandlerCodeFixProvider provider = new();

        foreach (Diagnostic diagnostic in compilation.GetDiagnostics().Where(
            diagnostic => diagnostic.Location.SourceTree == tree &&
                provider.FixableDiagnosticIds.Contains(diagnostic.Id)))
        {
            await provider.RegisterCodeFixesAsync(new CodeFixContext(
                document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        }

        return actions;
    }

    private static async Task<Document> ApplyAsync(Document document, CodeAction action)
    {
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation changes = operations.OfType<ApplyChangesOperation>().Single();
        return changes.ChangedSolution.GetDocument(document.Id)!;
    }

    internal sealed class CompilerDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(
            Document document, CancellationToken cancellationToken)
        {
            Compilation compilation = (await document.Project.GetCompilationAsync(cancellationToken))!;
            SyntaxTree? tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return compilation.GetDiagnostics(cancellationToken)
                .Where(diagnostic => diagnostic.Location.SourceTree == tree);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
            Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>([]);
        }

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(
            Project project, CancellationToken cancellationToken)
        {
            return (await project.GetCompilationAsync(cancellationToken))!.GetDiagnostics(cancellationToken);
        }
    }
}

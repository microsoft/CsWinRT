// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using WindowsRuntime.Migration;
using static WindowsRuntime.Migration.Tests.Test_LegacyWindowsRuntimeAttributeCodeFixProvider;

namespace WindowsRuntime.Migration.Tests;

/// <summary>
/// Checks explicit geometry narrowing against real legacy and migrated runtime behavior.
/// </summary>
[TestClass]
public sealed class Test_GeometryCodeFixProvider
{
    [TestMethod]
    [DataRow("Point", "1d / 3, double.MaxValue", "(float)p.X, (float)p.Y")]
    [DataRow("Size", "1d / 3, -1e-100", "(float)p.Width, (float)p.Height")]
    [DataRow("Rect", "double.NaN, double.NegativeInfinity, 1d / 3, -1e-100", "(float)p.X, (float)p.Y, (float)p.Width, (float)p.Height")]
    [DataRow("Point", "y: double.MaxValue, x: 1d / 3", "(float)p.X, (float)p.Y")]
    public async Task ConstructorArguments_PreserveLegacyFloatBits(string type, string arguments, string components)
    {
        string source = $$"""
            using Geometry = Windows.Foundation;
            public static class Probe
            {
                public static string Run()
                {
                    Geometry.{{type}} p = new Geometry.{{type}}({{arguments}});
                    float[] components = { {{components}} };
                    return string.Join(",", System.Array.ConvertAll(components, System.BitConverter.SingleToInt32Bits));
                }
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, preserve: true);
        object? baseline = Run(AsLegacy(document.Project));
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.IsNotEmpty(actions);
        Document changed = await ApplyAsync(document, actions[0]);
        await AssertCompilesAsync(changed.Project);
        Assert.AreEqual(baseline, Run(changed.Project));
        Assert.AreEqual(baseline, Run(AsLegacy(changed.Project)));
        Assert.IsEmpty(await GetActionsAsync(changed));
    }

    [TestMethod]
    public async Task MixedLongArgument_PreservesDoubleOverloadRounding()
    {
        const string source = """
            public static class Probe
            {
                public static long Run()
                {
                    long value = 4611686293305294849L;
                    var p = new Windows.Foundation.Point(1d, value);
                    return System.BitConverter.SingleToInt32Bits((float)p.Y);
                }
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, preserve: true);
        Document changed = await ApplyAsync(document, (await GetActionsAsync(document))[0]);
        StringAssert.Contains((await changed.GetTextAsync()).ToString(), "(float)((double)(value))");
        object? baseline = Run(AsLegacy(document.Project));
        Assert.AreEqual(baseline, Run(changed.Project));
        Assert.AreEqual(baseline, Run(AsLegacy(changed.Project)));
        long value = 4611686293305294849L;
        Assert.AreNotEqual((long)System.BitConverter.SingleToInt32Bits((float)value), baseline);
    }

    [TestMethod]
    public async Task TargetTypedConstructor_PreservesEvaluationOrderAndComments()
    {
        const string source = """
            using Windows.Foundation;
            public static class Probe
            {
                private static int calls;
                private static double Next() => ++calls / 3d;
                public static int Run()
                {
                    Point p = new(/* first */ Next(), /* second */ Next());
                    return calls == 2 && (float)p.X == (float)(1d / 3) && (float)p.Y == (float)(2d / 3) ? 42 : 0;
                }
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, preserve: true);
        Document changed = await ApplyAsync(document, (await GetActionsAsync(document))[0]);
        string text = (await changed.GetTextAsync()).ToString();
        StringAssert.Contains(text, "/* first */");
        StringAssert.Contains(text, "/* second */");
        Assert.AreEqual(42, Run(AsLegacy(document.Project)));
        Assert.AreEqual(42, Run(changed.Project));
        Assert.AreEqual(42, Run(AsLegacy(changed.Project)));
    }

    [TestMethod]
    [DataRow("Size", "-1d, 2d")]
    [DataRow("Rect", "0d, 0d, -1d, 2d")]
    public async Task ConstructorValidation_PreservesExceptionParameter(string type, string arguments)
    {
        string source = $$"""
            public static class Probe
            {
                public static string Run()
                {
                    try { var p = new Windows.Foundation.{{type}}({{arguments}}); return "No exception"; }
                    catch (System.ArgumentOutOfRangeException error) { return error.ParamName; }
                }
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, preserve: true);
        Document changed = await ApplyAsync(document, (await GetActionsAsync(document))[0]);
        Assert.AreEqual("width", Run(AsLegacy(document.Project)));
        Assert.AreEqual("width", Run(changed.Project));
        Assert.AreEqual("width", Run(AsLegacy(changed.Project)));
    }

    [TestMethod]
    [DataRow("Point", "X")]
    [DataRow("Point", "Y")]
    [DataRow("Rect", "X")]
    [DataRow("Rect", "Y")]
    public async Task CoordinateAssignment_PreservesLegacyNarrowing(string type, string member)
    {
        string source = $$"""
            public static class Probe
            {
                public static int Run()
                {
                    var p = new Windows.Foundation.{{type}}();
                    p.{{member}} = 1d / 3;
                    return System.BitConverter.SingleToInt32Bits((float)p.{{member}});
                }
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, preserve: true);
        Document changed = await ApplyAsync(document, (await GetActionsAsync(document))[0]);
        StringAssert.Contains((await changed.GetTextAsync()).ToString(), "(float)(1d / 3)");
        Assert.AreEqual(Run(AsLegacy(document.Project)), Run(changed.Project));
        Assert.AreEqual(Run(changed.Project), Run(AsLegacy(changed.Project)));
    }

    [TestMethod]
    public async Task CoordinateInitializer_FixedCodeCompiles()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, """
            public class Probe
            {
                public Windows.Foundation.Point Value = new() { X = 1d / 3, Y = 2f };
            }
            """, preserve: false);
        Document changed = await ApplyAsync(document, (await GetActionsAsync(document))[0]);
        await AssertCompilesAsync(changed.Project);
    }

    [TestMethod]
    [DataRow("public class Probe { public Windows.Foundation.Point Value = new(1m, 2d); }")]
    [DataRow("public class Probe { public Windows.Foundation.Rect Value = new(1d, 2d); }")]
    [DataRow("public class Probe { public Windows.Foundation.Point Value = new(x: 1d, x: 2d); }")]
    [DataRow("public class Probe { public void Run() { var s = new Windows.Foundation.Size(); s.Width = 1d; } }")]
    [DataRow("public class Probe { public void Run() { var r = new Windows.Foundation.Rect(); r.Height = -1e-100; } }")]
    [DataRow("public class Probe { public object Run() { var p = new Windows.Foundation.Point(); return p.X = 1d; } }")]
    [DataRow("public class Probe { public void Run() { var p = new Windows.Foundation.Point(); p.Deconstruct(out double x, out double y); } }")]
    [DataRow("namespace Custom { public struct Point { public Point(float x, float y) {} } } public class Probe { public Custom.Point Value = new(1d, 2d); }")]
    [DataRow("public class Probe { public void Run() { int value = 1L; } }")]
    [DataRow("public class Probe { private static void Take(float value) {} public void Run() { Take(1d); } }")]
    [DataRow("namespace Windows.Foundation { public struct Point { public Point(float x, float y) {} } } public class Probe { public Windows.Foundation.Point Value = new(1d, 2d); }")]
    public async Task UnsupportedContractOrUnrelatedGeometry_NoFix(string source)
    {
        using AdhocWorkspace workspace = new();
        Assert.IsEmpty(await GetActionsAsync(CreateDocument(workspace, source, preserve: false)));
    }

    [TestMethod]
    [DataRow("Test.g.cs", "")]
    [DataRow("Test.g.i.cs", "")]
    [DataRow("Test.cs", "// <autogenerated/>\n")]
    public async Task GeneratedGeometry_NoFix(string fileName, string header)
    {
        using AdhocWorkspace workspace = new();
        Assert.IsEmpty(await GetActionsAsync(CreateDocument(workspace,
            header + "public class Probe { public Windows.Foundation.Point Value = new(1d, 2d); }",
            preserve: false, fileName)));
    }

    [TestMethod]
    public async Task LegacyCompilationAndValidFloatArithmetic_NoFix()
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, """
            public class Probe
            {
                public Windows.Foundation.Point Value = new(1d, 2d);
                public double Run() => Value.X + Value.Y;
            }
            """, preserve: true);
        Assert.IsEmpty(await GetActionsAsync(AsLegacy(document.Project).GetDocument(document.Id)!));
        Document valid = document.WithText(Microsoft.CodeAnalysis.Text.SourceText.From("""
            public class Probe
            {
                public Windows.Foundation.Point Value = new(1f, 2f);
                public double Run() => Value.X + Value.Y;
            }
            """));
        Assert.IsEmpty(await GetActionsAsync(valid));
    }

    [TestMethod]
    public async Task FixAll_NestedConstructorsAndCoordinates_PreservesBothTargets()
    {
        const string source = """
            public static class Probe
            {
                public static int Run()
                {
                    var p = new Windows.Foundation.Point(new Windows.Foundation.Point(1d, 2d).X + 1d, 3d);
                    p.Y = 4d;
                    var s = new Windows.Foundation.Size(5d, 6d);
                    return (int)(p.X + p.Y + s.Width + s.Height);
                }
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, preserve: true);
        GeometryCodeFixProvider provider = new();
        FixAllContext context = new(document, provider, FixAllScope.Document, (await GetActionsAsync(document))[0].EquivalenceKey!,
            provider.FixableDiagnosticIds, new Test_TypedEventHandlerCodeFixProvider.CompilerDiagnosticProvider(), CancellationToken.None);
        CodeAction? action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.IsNotNull(action);
        Document changed = await ApplyAsync(document, action);
        await AssertCompilesAsync(changed.Project);
        await AssertCompilesAsync(AsLegacy(changed.Project));
        Assert.AreEqual(Run(AsLegacy(document.Project)), Run(changed.Project));
        Assert.IsEmpty(await GetActionsAsync(changed));
    }

    [TestMethod]
    public async Task ProjectFixAll_RespectsDiagnosticDocumentScope()
    {
        const string source = "public class Probe { public Windows.Foundation.Point Value = new(1d, 2d); }";
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source, preserve: false);
        Document excluded = document.Project.AddDocument("Excluded.cs",
            "public class Excluded { public Windows.Foundation.Point Value = new(3d, 4d); }");
        document = excluded.Project.GetDocument(document.Id)!;
        GeometryCodeFixProvider provider = new();
        FixAllContext context = new(document, provider, FixAllScope.Project, (await GetActionsAsync(document))[0].EquivalenceKey!,
            provider.FixableDiagnosticIds, new SelectedDocumentDiagnosticProvider(document.Id), CancellationToken.None);
        CodeAction? action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.IsNotNull(action);
        Document changed = await ApplyAsync(document, action);
        Assert.IsEmpty(await GetActionsAsync(changed));
        Document untouched = changed.Project.GetDocument(excluded.Id)!;
        Assert.AreEqual((await excluded.GetTextAsync()).ToString(), (await untouched.GetTextAsync()).ToString());
        Assert.IsNotEmpty(await GetActionsAsync(untouched));
    }

    private static async Task<List<CodeAction>> GetActionsAsync(Document document)
    {
        List<CodeAction> actions = [];
        Compilation compilation = (await document.Project.GetCompilationAsync())!;
        SyntaxTree? tree = await document.GetSyntaxTreeAsync();
        GeometryCodeFixProvider provider = new();

        foreach (Diagnostic diagnostic in compilation.GetDiagnostics().Where(item =>
            item.Location.SourceTree == tree && provider.FixableDiagnosticIds.Contains(item.Id)))
        {
            await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic,
                (action, _) => actions.Add(action), CancellationToken.None));
        }

        return actions;
    }

    private static object? Run(Project project)
    {
        Compilation compilation = project.GetCompilationAsync().GetAwaiter().GetResult()!;
        using MemoryStream stream = new();
        var result = compilation.Emit(stream);
        Assert.IsTrue(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        GeometryLoadContext context = new(project.MetadataReferences.Last().Display!);

        try
        {
            return context.LoadFromStream(stream).GetType("Probe")!.GetMethod("Run")!.Invoke(null, null);
        }
        finally
        {
            context.Unload();
        }
    }

    private sealed class GeometryLoadContext(string runtimePath) : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return assemblyName.Name == "WinRT.Runtime" ? LoadFromAssemblyPath(runtimePath) : null;
        }
    }

    private sealed class SelectedDocumentDiagnosticProvider(DocumentId selected) : FixAllContext.DiagnosticProvider
    {
        private readonly Test_TypedEventHandlerCodeFixProvider.CompilerDiagnosticProvider compiler = new();

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            return document.Id == selected ? compiler.GetDocumentDiagnosticsAsync(document, cancellationToken) :
                Task.FromResult<IEnumerable<Diagnostic>>([]);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>([]);
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return GetDocumentDiagnosticsAsync(project.GetDocument(selected)!, cancellationToken);
        }
    }
}

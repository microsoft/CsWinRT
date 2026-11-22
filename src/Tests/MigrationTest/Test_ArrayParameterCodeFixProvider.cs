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
using Microsoft.CodeAnalysis.Text;

namespace WindowsRuntime.Migration.Tests;

using Fixture = Test_LegacyWindowsRuntimeAttributeCodeFixProvider;

/// <summary>
/// Validates span signature conversions through compilation, invocation, and retained legacy source.
/// </summary>
[TestClass]
public sealed class Test_ArrayParameterCodeFixProvider
{
    [TestMethod]
    [DataRow("ReadOnlyArray", "int result = 0; foreach (int value in values) result += value; return result;", "ReadOnlySpan", 7)]
    [DataRow("ReadOnlyArray", "return values.Length;", "ReadOnlySpan", 2)]
    [DataRow("WriteOnlyArray", "for (int i = 0; i < values.Length; i++) values[i] = 21; return values.Length;", "Span", 42)]
    public async Task StraightforwardBody_CompilesAndRunsOnBothBranches(
        string attribute, string body, string typeName, int expected)
    {
        foreach (bool preserve in new[] { false, true })
        {
            using AdhocWorkspace workspace = new();
            Document document = Fixture.CreateDocument(workspace, $$"""
                public static class Probe
                {
                    private static int Process([System.Runtime.InteropServices.WindowsRuntime.{{attribute}}] int[] values)
                    {
                        {{body}}
                    }
                    public static int Run()
                    {
                        int[] values = { 3, 4 };
                        int result = Process(values);
                        return {{(attribute == "WriteOnlyArray" ? "values[0] + values[1]" : "result")}};
                    }
                }
                """, preserve);
            List<CodeAction> actions = await GetActionsAsync(document);
            Assert.HasCount(1, actions);
            Document changed = await Fixture.ApplyAsync(document, actions[0]);
            StringAssert.Contains((await changed.GetTextAsync()).ToString(), $"global::System.{typeName}<int>");
            await AssertRunsAsync(changed.Project, expected);
            Assert.IsEmpty(await GetActionsAsync(changed));

            if (preserve)
            {
                await AssertRunsAsync(Fixture.AsLegacy(changed.Project), expected);
            }
        }
    }

    [TestMethod]
    public async Task AliasedArrayAndAttribute_PreservesCommentsAndOtherAttributes()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            using Input = System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute;
            using Items = System.Int32[];
            public class Probe
            {
                public int Process(
                    [Input, System.Runtime.InteropServices.In] /* keep parameter comment */ Items values,
                    int count) => values.Length + count;
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        string source = (await changed.GetTextAsync()).ToString();
        StringAssert.Contains(source, "keep parameter comment");
        StringAssert.Contains(source, "System.Runtime.InteropServices.In");
        await Fixture.AssertCompilesAsync(changed.Project);
        await Fixture.AssertCompilesAsync(Fixture.AsLegacy(changed.Project));
    }

    [TestMethod]
    [DataRow("public static int Process([ATTR] int[] values) { values[0] = 4; return values.Length; }")]
    [DataRow("public static int Process([ATTR] int[] values) => values == null ? 0 : values.Length;")]
    [DataRow("public static int[] Process([ATTR] int[] values) => values;")]
    [DataRow("public static int Process([ATTR] int[] values) { System.Func<int> getter = () => values.Length; return getter(); }")]
    [DataRow("public static async System.Threading.Tasks.Task<int> Process([ATTR] int[] values) { await System.Threading.Tasks.Task.Yield(); return values.Length; }")]
    [DataRow("public static System.Collections.Generic.IEnumerable<int> Process([ATTR] int[] values) { yield return values.Length; }")]
    [DataRow("public static int Process([ATTR] int[] values = null) => values.Length;")]
    [DataRow("public static int Process([ATTR] ref int[] values) => values.Length;")]
    [DataRow("public static int Process([ATTR] params int[] values) => values.Length;")]
    [DataRow("public virtual int Process([ATTR] int[] values) => values.Length;")]
    [DataRow("public static int Process([ATTR] int[]? values) => values.Length;")]
    public async Task UnsupportedBodyOrSignature_GivesSpecificSuggestionWithoutFix(string method)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, "public class Probe { " +
            method.Replace("ATTR", "System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray") + " }", preserve: false);
        Diagnostic diagnostic = (await Fixture.GetDiagnosticsAsync(document.Project)).Single();
        Assert.AreEqual("CSWINRT2053", diagnostic.Id);
        StringAssert.Contains(diagnostic.GetMessage(), "ReadOnlySpan<T>");
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task WriteOnlyBodyReadingElements_NoFix()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public class Probe
            {
                public int Process([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] values) => values[0];
            }
            """, preserve: false);
        Diagnostic diagnostic = (await Fixture.GetDiagnosticsAsync(document.Project)).Single();
        StringAssert.Contains(diagnostic.GetMessage(), "Span<T>");
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task ImplicitInterfaceImplementation_NoLocalSignatureRewrite()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public interface IProbe { int Process(int[] values); }
            public class Probe : IProbe
            {
                public int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
            }
            """, preserve: false);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("public static int Process(object values) => -1; public static int Run() => Process(null);")]
    [DataRow("public static int Process(System.ReadOnlySpan<int> values) => -1;")]
    [DataRow("public static int Process<T>(T values) => -1; public static int Run() => Process(new[] { 3, 4 });")]
    public async Task OverloadChangesOrCollisions_NoFix(string extra)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Probe
            {
                public static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
            """ + extra + "}", preserve: false);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task FixAll_MultipleParameters_PreservesBothLegacySignatures()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            using System.Runtime.InteropServices.WindowsRuntime;
            public static class Probe
            {
                public static void Process([ReadOnlyArray] int[] input, [WriteOnlyArray] int[] output)
                {
                    for (int i = 0; i < input.Length; i++) output[i] = input[i];
                }
            }
            """, preserve: true);
        ArrayParameterCodeFixProvider provider = new();
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(2, actions);
        FixAllContext context = new(document, provider, FixAllScope.Document, actions[0].EquivalenceKey!,
            provider.FixableDiagnosticIds, new MappingDiagnosticProvider(), CancellationToken.None);
        CodeAction? action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.IsNotNull(action);
        Document changed = await Fixture.ApplyAsync(document, action);
        string source = (await changed.GetTextAsync()).ToString();
        StringAssert.Contains(source, "global::System.ReadOnlySpan<int>");
        StringAssert.Contains(source, "global::System.Span<int>");
        await Fixture.AssertCompilesAsync(changed.Project);
        await Fixture.AssertCompilesAsync(Fixture.AsLegacy(changed.Project));
        Assert.IsEmpty(await GetActionsAsync(changed));
    }

    [TestMethod]
    public async Task LoadedDependentCaller_WithArrayDelegate_NoFix()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Probe
            {
                public static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
            }
            """, preserve: false);
        ProjectId callerId = ProjectId.CreateNewId();
        Project caller = document.Project.Solution.AddProject(callerId, "Caller", "Caller", LanguageNames.CSharp).GetProject(callerId)!
            .WithMetadataReferences(document.Project.MetadataReferences)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddProjectReference(new ProjectReference(document.Project.Id));
        caller = caller.AddDocument("Caller.cs", SourceText.From("""
            public static class Caller { public static System.Func<int[], int> Getter => Probe.Process; }
            """)).Project;
        document = caller.Solution.GetDocument(document.Id)!;
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task KnownNullCaller_NoFix()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Probe
            {
                public static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
                public static int Run() => Process(null);
            }
            """, preserve: false);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("ReadOnlyArray", false)]
    [DataRow("ReadOnlyArray", true)]
    [DataRow("WriteOnlyArray", false)]
    [DataRow("WriteOnlyArray", true)]
    public async Task NullReturningCaller_GivesSuggestionWithoutFix(string attribute, bool nullable)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            #nullable {{(nullable ? "enable" : "disable")}}
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.{{attribute}}] int[] values)
                {
                    {{(attribute == "ReadOnlyArray" ? "return values.Length;" : "for (int i = 0; i < values.Length; i++) values[i] = 1; return 42;")}}
                }
                private static int[] Get() => null;
                public static int Run()
                {
                    try { return Process(Get()); }
                    catch (System.NullReferenceException) { return 100; }
                }
            }
            """, preserve: false);
        Assert.AreEqual("CSWINRT2053", (await Fixture.GetDiagnosticsAsync(document.Project)).Single().Id);
        Assert.IsEmpty(await GetActionsAsync(document));
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), 100);
    }

    [TestMethod]
    [DataRow("int[] values = new[] { 1 }; values = null; return Process(values);")]
    [DataRow("int[] values = new[] { 1 }; Clear(ref values); return Process(values);")]
    [DataRow("int[] values = new[] { 1 }; System.Action clear = () => values = null; clear(); return Process(values);")]
    [DataRow("int[] values = new[] { 1 }; (values, _) = (null, 0); return Process(values);")]
    [DataRow("int[] values = Get(); return Process(values);")]
    [DataRow("int[] values = new[] { 1 }; ref int[] alias = ref values; alias = null; return Process(values);")]
    [DataRow("return Process(Get()!);")]
    public async Task UnprovenOrReassignedCaller_NoFix(string body)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
                private static int[] Get() => null;
                private static void Clear(ref int[] values) => values = null;
                public static int Run() { {{body}} }
            }
            """, preserve: false);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("new int[2]")]
    [DataRow("new[] { 3, 4 }")]
    [DataRow("[3, 4]")]
    public async Task ConstructedArrayCaller_FixedCodeRuns(string argument)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
                public static int Run() => Process({{argument}});
            }
            """, preserve: false);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        await AssertRunsAsync((await Fixture.ApplyAsync(document, actions[0])).Project, 2);
    }

    [TestMethod]
    [DataRow(LanguageVersion.CSharp12, "object[] items = new string[2]; return Process(items);")]
    [DataRow(LanguageVersion.CSharp14, "object[] items = new string[2]; return Process(items);")]
    [DataRow(LanguageVersion.CSharp12, "return Process(new string[2]);")]
    [DataRow(LanguageVersion.CSharp14, "object[] items = new[] { \"a\", \"b\" }; return Process(items);")]
    public async Task CovariantWritableArray_RequiresReview(LanguageVersion language, string caller)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            public static class Probe
            {
                private static int Process<T>([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] T[] values)
                {
                    for (int i = 0; i < values.Length; i++) values[i] = default;
                    return values.Length;
                }
                public static int Run() { {{caller.Replace("Process(", "Process<object>(")}} }
            }
            """, preserve: false);
        document = WithLanguage(document, language);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), 2);
        Assert.AreEqual("CSWINRT2053", (await Fixture.GetDiagnosticsAsync(document.Project)).Single().Id);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("object[] items = new object[2]; return Process(items);")]
    [DataRow("object[] items = [\"a\", \"b\"]; return Process(items);")]
    [DataRow("object[] items = { \"a\", \"b\" }; return Process(items);")]
    [DataRow("return Process(new object[2]);")]
    [DataRow("return Process([\"a\", \"b\"]);")]
    public async Task ExactWritableArrayConstruction_PreservesBothTargets(string caller)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] object[] values)
                {
                    for (int i = 0; i < values.Length; i++) values[i] = "x";
                    return values.Length;
                }
                public static int Run() { {{caller}} }
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, 2);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), 2);
    }

    [TestMethod]
    public async Task CovariantReadonlyArray_PreservesBothTargets()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] object[] values) => values.Length;
                public static int Run() { object[] items = new string[2]; return Process(items); }
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, 2);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), 2);
    }

    [TestMethod]
    [DataRow(LanguageVersion.CSharp12, "ReadOnlyArray")]
    [DataRow(LanguageVersion.CSharp14, "ReadOnlyArray")]
    [DataRow(LanguageVersion.CSharp12, "WriteOnlyArray")]
    [DataRow(LanguageVersion.CSharp14, "WriteOnlyArray")]
    public async Task InferredMethodGroup_RequiresReview(LanguageVersion language, string attribute)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.{{attribute}}] int[] values)
                {
                    {{(attribute == "ReadOnlyArray" ? "return values.Length;" : "for (int i = 0; i < values.Length; i++) values[i] = 1; return values.Length;")}}
                }
                private static int[] Get() => null;
                public static int Run()
                {
                    var function = Process;
                    try { return function(Get()); }
                    catch (System.NullReferenceException) { return -1; }
                }
            }
            """, preserve: false);
        document = WithLanguage(document, language);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), -1);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task WritableCovarianceOrMethodGroup_InLoadedCaller_RequiresReview(bool methodGroup, bool dependent)
    {
        const string method = """
            public static class Processor
            {
                public static int Process([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] object[] values)
                {
                    for (int i = 0; i < values.Length; i++) values[i] = "x";
                    return values.Length;
                }
            }
            """;
        string callerSource = $$"""
            public static class Probe
            {
                private static object[] Get() => null;
                public static int Run()
                {
                    {{(methodGroup ? "var function = Processor.Process;" : "object[] items = new string[2];")}}
                    try { return {{(methodGroup ? "function(Get())" : "Processor.Process(items)")}}; }
                    catch (System.NullReferenceException) { return -1; }
                }
            }
            """;
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, method + (dependent ? "" : callerSource), preserve: false);

        if (dependent)
        {
            ProjectId id = ProjectId.CreateNewId();
            Project caller = document.Project.Solution.AddProject(id, "Caller", "Caller", LanguageNames.CSharp).GetProject(id)!
                .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp14))
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(document.Project.MetadataReferences)
                .AddProjectReference(new ProjectReference(document.Project.Id));
            document = caller.AddDocument("Caller.cs", SourceText.From(callerSource)).Project.Solution.GetDocument(document.Id)!;
        }
        else
        {
            await AssertRunsAsync(Fixture.AsLegacy(document.Project), methodGroup ? -1 : 2);
        }

        Assert.IsEmpty(await GetActionsAsync(document));
    }

    private static Document WithLanguage(Document document, LanguageVersion language)
    {
        return document.Project.WithParseOptions(
            ((CSharpParseOptions)document.Project.ParseOptions!).WithLanguageVersion(language)).GetDocument(document.Id)!;
    }

    [TestMethod]
    [DataRow("static")]
    [DataRow("receiver")]
    [DataRow("constructor")]
    [DataRow("write")]
    [DataRow("collection")]
    public async Task DynamicCaller_RequiresReview(string shape)
    {
        string source = shape switch
        {
            "collection" => """
                public class Collection : System.Collections.IEnumerable
                {
                    public int Length;
                    public void Add([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) { Length = values.Length; }
                    public System.Collections.IEnumerator GetEnumerator() => System.Array.Empty<int>().GetEnumerator();
                }
                public static class Probe
                {
                    private static int[] Get() => null;
                    public static int Run()
                    {
                        dynamic values = Get();
                        try { return new Collection { values }.Length; }
                        catch (System.NullReferenceException) { return -1; }
                    }
                }
                """,
            "constructor" => """
                public class Probe
                {
                    public int Length;
                    public Probe([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) { Length = values.Length; }
                    private static int[] Get() => null;
                    public static int Run()
                    {
                        dynamic values = Get();
                        try { return new Probe(values).Length; }
                        catch (System.NullReferenceException) { return -1; }
                    }
                }
                """,
            "write" => """
                public static class Probe
                {
                    public static int Process([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] object[] values)
                    {
                        for (int i = 0; i < values.Length; i++) values[i] = "x";
                        return values.Length;
                    }
                    public static int Run() { dynamic values = new string[2]; return Process(values); }
                }
                """,
            _ => $$"""
                public class Probe
                {
                    public {{(shape == "static" ? "static" : "")}} int Process(
                        [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
                    private static int[] Get() => null;
                    public static int Run()
                    {
                        dynamic values = Get();
                        {{(shape == "receiver" ? "dynamic receiver = new Probe();" : "")}}
                        try { return {{(shape == "receiver" ? "receiver.Process(values)" : "Process(values)")}}; }
                        catch (System.NullReferenceException) { return -1; }
                    }
                }
                """
        };
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, source, preserve: false);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), shape == "write" ? 2 : -1);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task UnrelatedDynamicCall_DoesNotBlockSafeConversion()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public class Other { public int Count(int[] values) => values.Length; }
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
                public static int Run()
                {
                    dynamic receiver = new Other();
                    return Process(new[] { 3, 4 }) + receiver.Count(new[] { 5 });
                }
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, 3);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), 3);
    }

    [TestMethod]
    public async Task LoadedDependentDynamicCaller_RequiresReview()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Processor
            {
                public static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
            }
            """, preserve: false);
        ProjectId id = ProjectId.CreateNewId();
        Project caller = document.Project.Solution.AddProject(id, "Caller", "Caller", LanguageNames.CSharp).GetProject(id)!
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp14))
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(document.Project.MetadataReferences)
            .AddProjectReference(new ProjectReference(document.Project.Id));
        caller = caller.AddDocument("Caller.cs", SourceText.From("""
            public static class Caller
            {
                public static int Run() { dynamic values = null; return Processor.Process(values); }
            }
            """)).Project;
        Assert.IsEmpty(await GetActionsAsync(caller.Solution.GetDocument(document.Id)!));
    }

    [TestMethod]
    [DataRow("base", "ReadOnlyArray")]
    [DataRow("this", "ReadOnlyArray")]
    [DataRow("primary", "ReadOnlyArray")]
    [DataRow("collection", "ReadOnlyArray")]
    [DataRow("extension-collection", "ReadOnlyArray")]
    [DataRow("collection-expression", "ReadOnlyArray")]
    [DataRow("base", "WriteOnlyArray")]
    [DataRow("this", "WriteOnlyArray")]
    [DataRow("primary", "WriteOnlyArray")]
    [DataRow("collection", "WriteOnlyArray")]
    [DataRow("extension-collection", "WriteOnlyArray")]
    [DataRow("collection-expression", "WriteOnlyArray")]
    public async Task ImplicitOrChainedNullCaller_RequiresReview(string shape, string attribute)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, CreateIndirectCaller(shape, attribute, "Get()"), preserve: false);
        Assert.AreEqual("CSWINRT2053", (await Fixture.GetDiagnosticsAsync(document.Project)).Single().Id);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), -1);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("base")]
    [DataRow("this")]
    [DataRow("primary")]
    [DataRow("collection")]
    [DataRow("extension-collection")]
    public async Task ImplicitOrChainedConstructedCaller_PreservesBothTargets(string shape)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace,
            CreateIndirectCaller(shape, "ReadOnlyArray", "new[] { 3, 4 }"), preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, 2);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), 2);
    }

    [TestMethod]
    [DataRow("base")]
    [DataRow("this")]
    [DataRow("primary")]
    [DataRow("collection")]
    public async Task ImplicitOrChainedOverloadRebinding_NoFix(string shape)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace,
            CreateIndirectCaller(shape, "ReadOnlyArray", "new[] { 3, 4 }", competingOverload: true), preserve: false);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), 2);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task CollectionExpressionAdd_RequiresReviewEvenWithConstructedElement()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace,
            CreateIndirectCaller("collection-expression", "ReadOnlyArray", "new[] { 3, 4 }"), preserve: false);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), 2);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("values[0].Inc(); return values[0].V;")]
    [DataRow("return values[0].Incremented;")]
    [DataRow("values[0].Inner.Inc(); return values[0].Inner.V;")]
    public async Task MutableStructElementReceiver_RequiresReview(string body)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            public struct Item
            {
                public int V;
                public Counter Inner;
                public void Inc() => V++;
                public int Incremented => ++V;
            }
            public struct Counter { public int V; public void Inc() => V++; }
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] Item[] values)
                {
                    {{body}}
                }
                public static int Run() => Process(new Item[1]);
            }
            """, preserve: false);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), 1);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    [DataRow("public class Item { public int V; public void Inc() => V++; }", "values[0].Inc(); return values[0].V;", "new[] { new Item() }", 1)]
    [DataRow("public readonly struct Item { public int Read() => 42; }", "return values[0].Read();", "new Item[1]", 42)]
    [DataRow("public struct Item { public int V; public readonly int Read() => V; }", "return values[0].Read();", "new[] { new Item { V = 42 } }", 42)]
    [DataRow("public struct Item { public int V; public readonly int Value => V; }", "return values[0].Value;", "new[] { new Item { V = 42 } }", 42)]
    [DataRow("public struct Item { public int V; }", "return values[0].V;", "new[] { new Item { V = 42 } }", 42)]
    public async Task ElementValueReadOrReadonlyReceiver_PreservesBehavior(string item, string body, string argument, int expected)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, $$"""
            {{item}}
            public static class Probe
            {
                private static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] Item[] values)
                {
                    {{body}}
                }
                public static int Run() => Process({{argument}});
            }
            """, preserve: true);
        await AssertRunsAsync(Fixture.AsLegacy(document.Project), expected);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, expected);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), expected);
    }

    private static string CreateIndirectCaller(string shape, string attribute, string argument, bool competingOverload = false)
    {
        string parameter = $"[System.Runtime.InteropServices.WindowsRuntime.{attribute}] int[] values";
        string body = attribute == "ReadOnlyArray"
            ? "N = values.Length;"
            : "for (int i = 0; i < values.Length; i++) values[i] = 1; N = values.Length;";
        string types = shape switch
        {
            "base" => $$"""
                public class Base
                {
                    public int N;
                    public Base({{parameter}}) { {{body}} }
                    {{(competingOverload ? "public Base(int[] values, bool extra = false) { N = 99; }" : "")}}
                }
                public class Target : Base { public Target() : base({{argument}}) {} private static int[] Get() => null; }
                """,
            "this" => $$"""
                public class Target
                {
                    public int N;
                    public Target() : this(0, {{argument}}) {}
                    private Target(int tag, {{parameter}}) { {{body}} }
                    {{(competingOverload ? "private Target(int tag, int[] values, bool extra = false) { N = 99; }" : "")}}
                    private static int[] Get() => null;
                }
                """,
            "primary" => $$"""
                public class Base
                {
                    public int N;
                    public Base({{parameter}}) { {{body}} }
                    {{(competingOverload ? "public Base(int[] values, bool extra = false) { N = 99; }" : "")}}
                }
                public class Target() : Base({{argument}}) { private static int[] Get() => null; }
                """,
            "collection" or "collection-expression" => $$"""
                public class Target : System.Collections.IEnumerable
                {
                    public int N;
                    public void Add({{parameter}}) { {{body}} }
                    {{(competingOverload ? "public void Add(int[] values, bool extra = false) { N = 99; }" : "")}}
                    public System.Collections.IEnumerator GetEnumerator() => System.Array.Empty<int>().GetEnumerator();
                }
                """,
            "extension-collection" => $$"""
                public class Target : System.Collections.IEnumerable
                {
                    public int N;
                    public System.Collections.IEnumerator GetEnumerator() => System.Array.Empty<int>().GetEnumerator();
                }
                public static class Extensions
                {
                    public static void Add(this Target target, {{parameter}})
                    {
                        int N = 0;
                        {{body}}
                        target.N = N;
                    }
                }
                """,
            _ => throw new System.ArgumentException(shape)
        };
        string creation = shape switch
        {
            "collection" or "extension-collection" => $"new Target {{ {argument} }}",
            "collection-expression" => $"Make([{argument}])",
            _ => "new Target()"
        };
        return types + $$"""
            public static class Probe
            {
                private static int[] Get() => null;
                private static Target Make(Target target) => target;
                public static int Run()
                {
                    try { return ({{creation}}).N; }
                    catch (System.NullReferenceException) { return -1; }
                }
            }
            """;
    }

    [TestMethod]
    public async Task SynchronousConstructor_ConvertsAndPreservesLegacySignature()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public class Probe
            {
                public Probe([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values)
                {
                    Value = values.Length;
                }
                public int Value { get; }
                public static int Run() => new Probe(new[] { 3, 4 }).Value;
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, 2);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), 2);
    }

    [TestMethod]
    public async Task LoadedDependentCaller_WithOrdinaryArrayArgument_StillCompiles()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Probe
            {
                public static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;
            }
            """, preserve: false);
        ProjectId id = ProjectId.CreateNewId();
        Project caller = document.Project.Solution.AddProject(id, "Caller", "Caller", LanguageNames.CSharp).GetProject(id)!
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp14))
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(document.Project.MetadataReferences)
            .AddProjectReference(new ProjectReference(document.Project.Id));
        caller = caller.AddDocument("Caller.cs", SourceText.From("""
            public static class Caller { public static int Run() => Probe.Process(new[] { 3, 4 }); }
            """)).Project;
        document = caller.Solution.GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await Fixture.AssertCompilesAsync(changed.Project.Solution.GetProject(id)!);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LoadedDependentBaseInitializer_ValidatesArrayArgument(bool constructed)
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public class Base
            {
                public int N;
                public Base([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) { N = values.Length; }
            }
            """, preserve: false);
        ProjectId id = ProjectId.CreateNewId();
        Project caller = document.Project.Solution.AddProject(id, "Caller", "Caller", LanguageNames.CSharp).GetProject(id)!
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp14))
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(document.Project.MetadataReferences)
            .AddProjectReference(new ProjectReference(document.Project.Id));
        caller = caller.AddDocument("Caller.cs", SourceText.From($$"""
            public class Caller : Base
            {
                public Caller() : base({{(constructed ? "new[] { 3, 4 }" : "Get()")}}) {}
                private static int[] Get() => null;
            }
            """)).Project;
        document = caller.Solution.GetDocument(document.Id)!;
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(constructed ? 1 : 0, actions);

        if (constructed)
        {
            Document changed = await Fixture.ApplyAsync(document, actions[0]);
            await Fixture.AssertCompilesAsync(changed.Project.Solution.GetProject(id)!);
        }
    }

    [TestMethod]
    public async Task ConflictingArrayDirections_NoFix()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public class Probe
            {
                public void Process(
                    [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray,
                     System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] values) { }
            }
            """, preserve: false);
        Assert.IsEmpty(await GetActionsAsync(document));
    }

    [TestMethod]
    public async Task ExistingSpanHelper_AllowsEquivalentArgumentPassing()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Probe
            {
                public static int Process([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => Helper(values);
                private static int Helper(System.ReadOnlySpan<int> values) => values.Length;
                public static int Run() => Process(new[] { 3, 4 });
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, 2);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), 2);
    }

    [TestMethod]
    public async Task GenericElementInference_PreservesTheConstructedMethod()
    {
        using AdhocWorkspace workspace = new();
        Document document = Fixture.CreateDocument(workspace, """
            public static class Probe
            {
                private static int Count<T>([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] T[] values) => values.Length;
                public static int Run() => Count(new[] { 3, 4 });
            }
            """, preserve: true);
        List<CodeAction> actions = await GetActionsAsync(document);
        Assert.HasCount(1, actions);
        Document changed = await Fixture.ApplyAsync(document, actions[0]);
        await AssertRunsAsync(changed.Project, 2);
        await AssertRunsAsync(Fixture.AsLegacy(changed.Project), 2);
    }

    private static async Task<List<CodeAction>> GetActionsAsync(Document document)
    {
        List<CodeAction> actions = [];
        SyntaxTree? tree = await document.GetSyntaxTreeAsync();
        ArrayParameterCodeFixProvider provider = new();

        foreach (Diagnostic diagnostic in (await Fixture.GetDiagnosticsAsync(document.Project)).Where(
            item => item.Location.SourceTree == tree && provider.FixableDiagnosticIds.Contains(item.Id)))
        {
            await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic,
                (action, _) => actions.Add(action), CancellationToken.None));
        }

        return actions;
    }

    private static async Task AssertRunsAsync(Project project, int expected)
    {
        Compilation compilation = (await project.GetCompilationAsync())!;
        using MemoryStream stream = new();
        var result = compilation.Emit(stream);
        Assert.IsTrue(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        ArrayLoadContext context = new(project.MetadataReferences.Last().Display!);

        try
        {
            Assembly assembly = context.LoadFromStream(stream);
            Assert.AreEqual(expected, assembly.GetType("Probe")!.GetMethod("Run")!.Invoke(null, null));
        }
        finally
        {
            context.Unload();
        }
    }

    private sealed class ArrayLoadContext(string runtimePath) : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return assemblyName.Name == "WinRT.Runtime" ? LoadFromAssemblyPath(runtimePath) : null;
        }
    }

    private sealed class MappingDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            SyntaxTree? tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return (await Fixture.GetDiagnosticsAsync(document.Project)).Where(item => item.Location.SourceTree == tree);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>([]);
        }

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return await Fixture.GetDiagnosticsAsync(project);
        }
    }
}

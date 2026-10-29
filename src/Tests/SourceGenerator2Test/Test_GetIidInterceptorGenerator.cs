// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public partial class Test_GetIidInterceptorGenerator
{
    private static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.CSharp14)
        .WithFeatures([new("InterceptorsNamespaces", "WindowsRuntime.Generated")]);

    [TestMethod]
    [DataRow("object", "af86e2e0-b12d-4c6a-9c5a-d7aa65101e90")]
    [DataRow("System.IDisposable", "30d5a829-7fa4-4026-83bb-d75bae4ea99e")]
    [DataRow("System.Uri", "9e365e57-48b2-4160-956f-c7385120bbfc")]
    [DataRow("int?", "548cefbd-bc8a-5fa0-8df2-957440fc8bf4")]
    [DataRow("int[]", "a6d080a5-b087-5bc2-9a9f-5cd687b4d1f7")]
    [DataRow("System.Collections.Generic.IList<int>", "b939af5b-b45d-5489-9149-61442c1905fe")]
    [DataRow("System.Collections.Generic.IEnumerable<int>", "81a643fb-f51c-5565-83c4-f96425777b66")]
    [DataRow("Windows.Foundation.IStringable", "96369f54-8eb6-48f0-abce-c1b211e627c3")]
    [DataRow("Windows.Foundation.IAsyncAction", "5a648006-843a-4da9-865b-9d26e5dfad7b")]
    [DataRow("Windows.Foundation.IAsyncInfo", "00000036-0000-0000-c000-000000000046")]
    [DataRow("Windows.Storage.Streams.IBuffer", "905a0fe0-bc53-11df-8c49-001e4fc686da")]
    [DataRow("Windows.Foundation.AsyncActionCompletedHandler", "a4ed5c81-76c9-40bd-8be6-b1d90fb20ae7")]
    [DataRow("System.Collections.Generic.IList<Windows.Foundation.AsyncActionCompletedHandler>", "5dafe591-86dc-59aa-bfda-07f5d59fc708")]
    [DataRow("System.Collections.Generic.IList<System.Uri>", "0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e")]
    [DataRow("System.Collections.Generic.IList<System.Collections.Generic.IEnumerable<Windows.Foundation.IStringable>>", "0e3f106f-a266-50a1-8043-c90fcf3844f6")]
    [DataRow("System.EventHandler<System.Collections.Generic.IEnumerable<Windows.Foundation.IStringable>>", "fa0b7d80-7efa-52df-9b69-0574ce57ada4")]
    [DataRow("Windows.Foundation.IAsyncOperation<System.Collections.Generic.IEnumerable<Windows.Foundation.IStringable>>", "2bd35ee6-72d9-5c5d-9827-05ebb81487ab")]
    [DataRow("Windows.Foundation.Collections.IObservableMap<string, object>", "236aac9d-fb12-5c4d-a41c-9e445fb4d7ec")]
    [DataRow("Windows.Foundation.Collections.IMapChangedEventArgs<string>", "60141efb-f2f9-5377-96fd-f8c60d9558b5")]
    [DataRow("Windows.Foundation.Collections.MapChangedEventHandler<string, object>", "24f981e5-ddca-538d-aada-a59906084cf1")]
    [DataRow("Windows.Foundation.Collections.IObservableVector<object>", "7b81c56a-0985-518d-baa9-0da9ae009f65")]
    [DataRow("Windows.Foundation.Collections.VectorChangedEventHandler<object>", "b423a801-d35e-56b9-813b-00889536cb98")]
    [DataRow("Windows.Foundation.IAsyncOperation<bool>", "cdb5efb3-5788-509d-9be1-71ccb8a3362a")]
    [DataRow("Windows.Foundation.IAsyncOperationWithProgress<uint, uint>", "eccb574a-c684-5572-a679-6b0842cfb57f")]
    [DataRow("Windows.Foundation.IAsyncActionWithProgress<double>", "4f1430a6-a825-56ca-b047-1a9bad52ba67")]
    [DataRow("System.Collections.Generic.IDictionary<string, string>", "f6d1f700-49c2-52ae-8154-826f9908773c")]
    [DataRow("System.Collections.Generic.IReadOnlyDictionary<string, string>", "ac7f26f2-feb7-5b2a-8ac4-345bc62caede")]
    [DataRow("System.Collections.Generic.KeyValuePair<string, string>", "60310303-49c5-52e6-abc6-a9b36eccc716")]
    [DataRow("System.Collections.Generic.IReadOnlyList<string>", "2f13c006-a03a-5f69-b090-75a43e33423e")]
    [DataRow("System.Collections.Generic.IEnumerator<string>", "8c304ebb-6615-50a4-8829-879ecd443236")]
    [DataRow("System.EventHandler<object, object>", "c7e65ce2-fad5-5e3b-9c58-186ca8c1dd57")]
    [DataRow("System.Guid?", "7d50f649-632c-51f9-849a-ee49428933ea")]
    [DataRow("Windows.Foundation.AsyncStatus?", "a4b74936-2947-5fe8-88d5-51cd35050e71")]
    [DataRow("Windows.Foundation.Collections.CollectionChange?", "25bcaf91-880d-537d-82fc-9bbf0caccb8b")]
    [DataRow("Windows.Storage.Streams.InputStreamOptions?", "aea3ca95-06d3-5b19-a94d-907d1e6bcf18")]
    [DataRow("Windows.Foundation.AsyncActionProgressHandler<A>", "c261d8d0-71ba-5f38-a239-872342253a18")]
    [DataRow("Windows.Foundation.AsyncActionWithProgressCompletedHandler<A>", "9a0d211c-0374-5d23-9e15-eaa3570fae63")]
    [DataRow("Windows.Foundation.AsyncOperationCompletedHandler<A>", "9d534225-231f-55e7-a6d0-6c938e2d9160")]
    [DataRow("Windows.Foundation.AsyncOperationProgressHandler<A, B>", "264f1e0c-abe4-590b-9d37-e1cc118ecc75")]
    [DataRow("Windows.Foundation.AsyncOperationWithProgressCompletedHandler<A, B>", "c2d078d8-ac47-55ab-83e8-123b2be5bc5a")]
    public void ClosedType_ReturnsExpectedIid(string type, string expected)
    {
        string source = $$"""
            using A = System.Collections.Generic.IEnumerable<Windows.Foundation.IStringable>;
            using B = System.Collections.Generic.KeyValuePair<string, Windows.Foundation.IAsyncOperationWithProgress<System.Collections.Generic.IEnumerable<Windows.Foundation.IStringable>, float>>;

            public static class Test
            {
                public static System.Guid Run() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<{{type}}>();
            }
            """;

        (Compilation compilation, GeneratorDriverRunResult result) = Run(source);
        Assert.AreEqual(1, result.GeneratedTrees.Length);
        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.IsTrue(emit.Success, string.Join("\n", emit.Diagnostics));
        Assembly assembly = Assembly.Load(stream.ToArray());
        Assert.AreEqual(Guid.Parse(expected), assembly.GetType("Test")!.GetMethod("Run")!.Invoke(null, null));
    }

    [TestMethod]
    [DataRow("int")]
    [DataRow("string")]
    [DataRow("System.Guid")]
    [DataRow("System.EventHandler")]
    [DataRow("System.EventHandler[]")]
    [DataRow("System.Collections.Generic.IList<System.EventHandler>")]
    [DataRow("System.Collections.Generic.KeyValuePair<string, System.EventHandler>")]
    [DataRow("System.Collections.Generic.List<int>")]
    [DataRow("System.Collections.Generic.IList<int[]>")]
    [DataRow("System.Collections.Generic.IList<System.Version>")]
    [DataRow("System.Collections.Generic.IList<System.AttributeTargets>")]
    [DataRow("System.AttributeTargets?")]
    [DataRow("Windows.Foundation.FoundationContract?")]
    [DataRow("System.Collections.Generic.KeyValuePair<int, int>?")]
    [DataRow("int[][]")]
    [DataRow("int[,]")]
    [DataRow("nint")]
    [DataRow("(int, int)")]
    [DataRow("T")]
    [DataRow("System.Collections.Generic.IList<T>")]
    [DataRow("T[]")]
    public void UnsupportedOrOpenType_LeavesCallAlone(string type)
    {
        (Compilation _, GeneratorDriverRunResult result) = Run($$"""
            public static class Test
            {
                public static System.Guid Run<T>() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<{{type}}>();
            }
            """);
        Assert.IsEmpty(result.GeneratedTrees);
    }

    [TestMethod]
    [DataRow("dynamic")]
    [DataRow("dynamic[]")]
    [DataRow("System.Collections.Generic.IList<dynamic>")]
    [DataRow("System.Collections.Generic.IDictionary<string, System.Collections.Generic.IList<dynamic>>")]
    public void DynamicType_LeavesCallAlone(string type)
    {
        (Compilation compilation, GeneratorDriverRunResult result) = Run($$"""
            public static class Test
            {
                public static System.Guid Run() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<{{type}}>();
            }
            """);
        Assert.IsEmpty(result.GeneratedTrees);
        Assert.IsEmpty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
    }

    [TestMethod]
    public void AliasesAndUsingStatic_ShareInterceptorRvaData()
    {
        (Compilation compilation, GeneratorDriverRunResult result) = Run("""
            using Marshal = WindowsRuntime.InteropServices.WindowsRuntimeMarshal;
            using static WindowsRuntime.InteropServices.WindowsRuntimeMarshal;
            public static class Test
            {
                public static System.Guid A() => Marshal.GetIid<object>();
                public static System.Guid B() => GetIid<object>();
                public static System.Guid C() => global::WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """);
        SyntaxNode root = result.GeneratedTrees.Single().GetRoot();
        Assert.AreEqual(0, root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Count());
        Assert.AreEqual(1, root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count());
        Assert.AreEqual(3, root.DescendantNodes().OfType<AttributeSyntax>().Count(attribute => attribute.Name.ToString().EndsWith("InterceptsLocationAttribute", StringComparison.Ordinal)));
        Assert.IsEmpty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
    }

    [TestMethod]
    [DataRow(null, true)]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void Interceptor_HonorsOptOutProperty(bool? generateInterceptors, bool expectIntercepted)
    {
        (_, GeneratorDriverRunResult result) = Run("""
            public static class Test
            {
                public static System.Guid Run() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """, generateInterceptors: generateInterceptors);

        Assert.AreEqual(expectIntercepted, result.GeneratedTrees.Length != 0);
    }

    [TestMethod]
    [DataRow("WindowsRuntime.Generated", true)]
    [DataRow("WindowsRuntime", true)]
    [DataRow("Other.Namespace;WindowsRuntime.Generated", true)]
    [DataRow("WindowsRuntime.Generated;Other.Namespace", true)]
    [DataRow("global", true)]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow(";", false)]
    [DataRow("Windows", false)]
    [DataRow("WindowsRuntimeX", false)]
    [DataRow("WindowsRuntime.", false)]
    [DataRow("WindowsRuntime.Generated.Nested", false)]
    [DataRow(" WindowsRuntime.Generated", false)]
    [DataRow("windowsruntime.generated", false)]
    [DataRow("global.WindowsRuntime.Generated", false)]
    [DataRow("Other.Namespace", false)]
    public void Interceptor_RequiresInterceptorsNamespace(string? namespaces, bool expectIntercepted)
    {
        const string Source = """
            public static class Test
            {
                public static System.Guid Run() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """;

        CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);

        if (namespaces is not null)
        {
            parseOptions = parseOptions.WithFeatures([new("InterceptorsNamespaces", namespaces)]);
        }

        (Compilation compilation, GeneratorDriverRunResult result) = Run(Source, parseOptions: parseOptions);

        Assert.AreEqual(expectIntercepted, result.GeneratedTrees.Length != 0);
        Assert.IsEmpty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());

        // Compare with the compiler itself: the interceptors (generated with the namespace enabled) must be accepted with these
        // options exactly when we generate them (CS9137 is the error for an interceptor in a namespace that isn't enabled)
        (_, GeneratorDriverRunResult enabled) = Run(Source);
        CSharpCompilation oracle = CSharpCompilation.Create(
            "Oracle",
            [
                CSharpSyntaxTree.ParseText(Source, parseOptions, path: @"C:\Tests\Input.cs"),
                CSharpSyntaxTree.ParseText(enabled.GeneratedTrees.Single().ToString(), parseOptions)
            ],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.AreEqual(expectIntercepted, !oracle.GetDiagnostics().Any(static diagnostic => diagnostic.Id == "CS9137"));
    }

    [TestMethod]
    public void MetadataCache_DoesNotLeakAcrossCompilations()
    {
        // Both projections define the same type names, so a cache shared across compilations would return the wrong IID
        foreach (Guid iid in new[] { Guid.NewGuid(), Guid.NewGuid() })
        {
            (Compilation compilation, GeneratorDriverRunResult result) = Run("""
                using WindowsRuntime.InteropServices;
                public static class Test
                {
                    public static System.Guid A() => WindowsRuntimeMarshal.GetIid<Projection.RuntimeClass>();
                    public static System.Guid B() => WindowsRuntimeMarshal.GetIid<Projection.IRuntimeClass>();
                    public static System.Guid C() => WindowsRuntimeMarshal.GetIid<Projection.RuntimeClass>();
                    public static System.Guid D() => WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<Projection.RuntimeClass>>();
                    public static System.Guid E() => WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<Projection.IRuntimeClass>>();
                }
                """, [CreateRuntimeClassProjection(iid)]);

            string generated = result.GeneratedTrees.Single().ToString();

            // The class and its default interface share one interceptor, while the two lists differ (a class has its own signature)
            Assert.AreEqual(3, result.GeneratedTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Count());
            Assert.AreEqual(5, generated.Split("InterceptsLocationAttribute(").Length - 2);
            Assert.Contains($"GetIid_{iid.ToString("N").ToUpperInvariant()}", generated);
            Assert.IsEmpty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
        }
    }

    [TestMethod]
    public void MetadataCache_AfterEditAddingRuntimeClassCall_MatchesFreshRun()
    {
        MetadataReference reference = CreateRuntimeClassProjection(Guid.NewGuid());

        const string Before = """
            using WindowsRuntime.InteropServices;
            public static class Test
            {
                public static System.Guid A() => WindowsRuntimeMarshal.GetIid<Projection.IRuntimeClass>();
            }
            """;

        const string After = """
            using WindowsRuntime.InteropServices;
            public static class Test
            {
                public static System.Guid A() => WindowsRuntimeMarshal.GetIid<Projection.IRuntimeClass>();
                public static System.Guid B() => WindowsRuntimeMarshal.GetIid<Projection.RuntimeClass>();
            }
            """;

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Incremental",
            [CSharpSyntaxTree.ParseText(Before, ParseOptions, path: @"C:\Tests\Input.cs")],
            [.. GetReferences(), reference],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new GetIidInterceptorGenerator().AsSourceGenerator()], parseOptions: ParseOptions, optionsProvider: new OptionsProvider(false, null));

        // The first run only deals with an interface, so nothing needs the default interface index yet. After the edit, the
        // new compilation needs it for the first time, and the result must be the same as if it had been analyzed from scratch.
        driver = driver.RunGenerators(compilation);
        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(After, ParseOptions, path: @"C:\Tests\Input.cs"));
        driver = driver.RunGenerators(compilation);

        (_, GeneratorDriverRunResult fresh) = Run(After, [reference]);

        Assert.AreEqual(fresh.GeneratedTrees.Single().ToString(), driver.GetRunResult().GeneratedTrees.Single().ToString());
    }

    [TestMethod]
    public void Interceptor_EmbedsExactRvaBytesWithoutStaticConstructor()
    {
        (Compilation compilation, _) = Run("""
            public static class Test
            {
                public static System.Guid Run() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """);
        using MemoryStream stream = new();
        Assert.IsTrue(compilation.Emit(stream).Success);
        stream.Position = 0;
        using PEReader pe = new(stream);
        MetadataReader reader = pe.GetMetadataReader();
        FieldDefinition[] rvaFields = reader.FieldDefinitions.Select(reader.GetFieldDefinition)
            .Where(static field => (field.Attributes & FieldAttributes.HasFieldRVA) != 0).ToArray();
        Assert.HasCount(1, rvaFields);
        CollectionAssert.AreEqual(
            new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90").ToByteArray(),
            pe.GetSectionData(rvaFields[0].GetRelativeVirtualAddress()).GetContent(0, 16).ToArray());
        Assert.IsFalse(reader.MethodDefinitions.Any(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == ".cctor"));
    }

    [TestMethod]
    public void UnrelatedEdit_DoesNotChangeInterceptorOutput()
    {
        CSharpCompilation compilation = CSharpCompilation.Create("Incremental",
            [CSharpSyntaxTree.ParseText("""
                public static class Test
                {
                    public static System.Guid Run() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
                }
                """, ParseOptions, @"C:\Tests\Input.cs")],
            GetReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new GetIidInterceptorGenerator().AsSourceGenerator()], parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        string first = driver.GetRunResult().GeneratedTrees.Single().ToString();
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }", ParseOptions, @"C:\Tests\Other.cs")));
        GeneratorDriverRunResult result = driver.GetRunResult();
        Assert.AreEqual(first, result.GeneratedTrees.Single().ToString());
        var outputs = result.Results.Single().TrackedSteps["GetIidInterceptorInfo"].SelectMany(static step => step.Outputs).ToArray();
        Assert.HasCount(1, outputs);
        Assert.IsTrue(outputs.All(static output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
    }

    [TestMethod]
    public void MethodGroupsAndExpressionTrees_AreNotIntercepted()
    {
        (Compilation _, GeneratorDriverRunResult result) = Run("""
            using System;
            using System.Linq.Expressions;
            using WindowsRuntime.InteropServices;
            public static class Test
            {
                public static Func<Guid> MethodGroup = WindowsRuntimeMarshal.GetIid<object>;
                public static Expression<Func<Guid>> Expression = () => WindowsRuntimeMarshal.GetIid<object>();
            }
            """);
        Assert.IsEmpty(result.GeneratedTrees);
    }

    [TestMethod]
    public void QueryExpressionTree_IsNotIntercepted()
    {
        (Compilation _, GeneratorDriverRunResult result) = Run("""
            using System.Linq;
            using WindowsRuntime.InteropServices;
            public static class Test
            {
                public static IQueryable<System.Guid> Run(IQueryable<int> query) =>
                    from value in query select WindowsRuntimeMarshal.GetIid<int?>();
            }
            """);
        Assert.IsEmpty(result.GeneratedTrees);
    }

    [TestMethod]
    public void UwpExecutable_SelectsUwpIid()
    {
        (Compilation compilation, GeneratorDriverRunResult result) = Run("""
            public static class Test
            {
                public static void Main() { }
                public static System.Guid Run() =>
                    WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.ComponentModel.INotifyPropertyChanged>();
            }
            """, outputKind: OutputKind.ConsoleApplication, useUwp: true);
        Assert.Contains("GetIid_CF75D69CF2F4486BB302BB4C09BAEBFA", result.GeneratedTrees.Single().ToString());
        Assert.IsEmpty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
    }

    [TestMethod]
    public void LibraryWithModeDependentNestedType_LeavesCallAlone()
    {
        (Compilation _, GeneratorDriverRunResult result) = Run("""
            public static class Test
            {
                public static System.Guid Run() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<
                    System.Collections.Generic.IList<System.ComponentModel.INotifyPropertyChanged>>();
            }
            """);
        Assert.IsEmpty(result.GeneratedTrees);
    }

    [TestMethod]
    public void ReferenceShapedProjection_UsesDelegateGuidAndHiddenDefaultInterfaceName()
    {
        const string projection = """
            using System;
            using System.Runtime.InteropServices;
            using WindowsRuntime.InteropServices;
            [assembly: WindowsRuntimeReferenceAssembly]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.RuntimeClass", "Projection.IRuntimeClass")]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.GenericDefaultClass", "System.Collections.Generic.IList<int>")]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.MalformedDefaultClass", "System.Collections.Generic.IList<")]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.MissingDefaultClass", "Projection.IMissing")]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.ClassDefaultClass", "Projection.RuntimeClass")]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.ShadowedDefaultClass", "Shadowed.IShadow")]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.MappedDefaultClass", "System.IDisposable")]
            [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.NestedDefaultClass", "System.Collections.Generic.IList<Projection.RuntimeClass>")]
            namespace Projection
            {
                [Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E")]
                internal interface IRuntimeClass { }
                public sealed class RuntimeClass { }
                public sealed class GenericDefaultClass { }
                public sealed class MalformedDefaultClass { }
                public sealed class MissingDefaultClass { }
                public sealed class ClassDefaultClass { }
                public sealed class ShadowedDefaultClass { }
                public sealed class MappedDefaultClass { }
                public sealed class NestedDefaultClass { }
                [Guid("96369F54-8EB6-48F0-ABCE-C1B211E627C3")]
                public delegate void Handler();
                public struct Value { public int X; public string Y; }
                public sealed class OldProjectionClass { }
                public sealed class MetadataAttribute : Attribute { }
                [Windows.Foundation.Metadata.ApiContract]
                public enum Contract { }
                public enum EnumValue { Zero }
            }
            """;
        MetadataReference reference = CompileReference(projection, "Projection");
        (Compilation compilation, GeneratorDriverRunResult result) = Run("""
            using WindowsRuntime.InteropServices;
            namespace Shadowed
            {
                [System.Runtime.InteropServices.Guid("D7527847-57DA-4E15-914C-06806699A098")]
                public interface IShadow { }
            }
            public static class Test
            {
                public static System.Guid A() => WindowsRuntimeMarshal.GetIid<Projection.RuntimeClass>();
                public static System.Guid B() => WindowsRuntimeMarshal.GetIid<Projection.Handler>();
                public static System.Guid C() => WindowsRuntimeMarshal.GetIid<Projection.Value?>();
                public static System.Guid D() => WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<Projection.RuntimeClass>>();
                public static System.Guid E() => WindowsRuntimeMarshal.GetIid<Projection.OldProjectionClass>();
                public static System.Guid F() => WindowsRuntimeMarshal.GetIid<Projection.MetadataAttribute>();
                public static System.Guid G() => WindowsRuntimeMarshal.GetIid<Projection.GenericDefaultClass>();
                public static System.Guid H() => WindowsRuntimeMarshal.GetIid<Projection.Contract?>();
                public static System.Guid I() => WindowsRuntimeMarshal.GetIid<Projection.EnumValue?>();
                public static System.Guid J() => WindowsRuntimeMarshal.GetIid<Projection.MalformedDefaultClass>();
                public static System.Guid K() => WindowsRuntimeMarshal.GetIid<Projection.MissingDefaultClass>();
                public static System.Guid L() => WindowsRuntimeMarshal.GetIid<Projection.ClassDefaultClass>();
                public static System.Guid M() => WindowsRuntimeMarshal.GetIid<Projection.ShadowedDefaultClass>();
                public static System.Guid N() => WindowsRuntimeMarshal.GetIid<Projection.MappedDefaultClass>();
                public static System.Guid O() => WindowsRuntimeMarshal.GetIid<Projection.NestedDefaultClass>();
            }
            """, [reference]);
        string generated = result.GeneratedTrees.Single().ToString();
        Guid nestedIid = CreateExpectedParameterizedIid("pinterface({913337e9-11a1-4345-a3a2-4e7f956e222d};rc(Projection.RuntimeClass;{30d5a829-7fa4-4026-83bb-d75bae4ea99e}))");

        // Class, consumer-declared, malformed, and missing default interfaces are not intercepted. Calls with
        // the same IID share a method, so the mapped and nested cases add interceptors but no new methods.
        Assert.AreEqual(6, result.GeneratedTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Count());
        Assert.AreEqual(8, generated.Split("InterceptsLocationAttribute(").Length - 2);
        Assert.Contains("GetIid_B939AF5BB45D5489914961442C1905FE", generated);
        Assert.Contains($"GetIid_{nestedIid.ToString("N").ToUpperInvariant()}", generated);
        Assert.IsEmpty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
    }

    [TestMethod]
    public void UnrelatedMethodsAndManagedComTypes_AreNotIntercepted()
    {
        (Compilation _, GeneratorDriverRunResult result) = Run("""
            using System;
            using System.Runtime.InteropServices;
            [Guid("96369F54-8EB6-48F0-ABCE-C1B211E627C3")]
            public interface IManaged { }
            public static class Test
            {
                public static Guid GetIid<T>() => default;
                public static Guid A() => GetIid<object>();
                public static Guid B() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<IManaged>();
            }
            """);
        Assert.IsEmpty(result.GeneratedTrees);
    }

    [TestMethod]
    public void ReferencedImpostorTypes_AreNotIntercepted()
    {
        MetadataReference reference = CompileReference("""
            namespace System.Collections.Generic { public interface IList<T> { } }
            namespace Windows.Foundation { public interface IAsyncOperation<T> { } }
            """, "Impostors").WithAliases(["impostor"]);
        (Compilation _, GeneratorDriverRunResult result) = Run("""
            extern alias impostor;
            public static class Test
            {
                public static System.Guid A() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<impostor::System.Collections.Generic.IList<int>>();
                public static System.Guid B() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<impostor::Windows.Foundation.IAsyncOperation<int>>();
            }
            """, [reference]);
        Assert.IsEmpty(result.GeneratedTrees);
    }

    [TestMethod]
    public void ClosedArrayAndNestedGeneric_AreIntercepted()
    {
        (Compilation compilation, GeneratorDriverRunResult result) = Run("""
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class Test
            {
                public static System.Guid A() => WindowsRuntimeMarshal.GetIid<int[]>();
                public static System.Guid B() => WindowsRuntimeMarshal.GetIid<IDictionary<string, IList<int>>>();
                public static System.Guid C() => WindowsRuntimeMarshal.GetIid<KeyValuePair<string, int>>();
                public static System.Guid D() => WindowsRuntimeMarshal.GetIid<Windows.Foundation.IAsyncOperation<int>>();
                public static System.Guid E() => WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<System.EventHandler<object>>>();
                public static System.Guid F() => WindowsRuntimeMarshal.GetIid<KeyValuePair<int, int>>();
            }
            """);
        Assert.AreEqual(6, result.GeneratedTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Count());
        Assert.IsEmpty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
    }

    private static MetadataReference[] GetReferences() =>
    [
        .. Net100.References.All,
        MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location)
    ];

    private static MetadataReference CreateRuntimeClassProjection(Guid iid) => CompileReference($$"""
        using System.Runtime.InteropServices;
        using WindowsRuntime.InteropServices;
        [assembly: WindowsRuntimeReferenceAssembly]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.RuntimeClass", "Projection.IRuntimeClass")]
        namespace Projection
        {
            [Guid("{{iid}}")]
            public interface IRuntimeClass { }
            public sealed class RuntimeClass { }
        }
        """, "Projection");

    private static MetadataReference CompileReference(string source, string name)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(name,
            [CSharpSyntaxTree.ParseText(source, ParseOptions)], GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.IsTrue(emit.Success, string.Join("\n", emit.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static (Compilation Compilation, GeneratorDriverRunResult Result) Run(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool useUwp = false,
        string? assemblyName = null,
        bool? generateInterceptors = null,
        string[]? suppressedDiagnostics = null,
        CSharpParseOptions? parseOptions = null)
    {
        parseOptions ??= ParseOptions;

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName ?? $"InterceptorTest_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(source, parseOptions, path: @"C:\Tests\Input.cs")],
            [.. GetReferences(), .. additionalReferences ?? []],
            new CSharpCompilationOptions(outputKind).WithSpecificDiagnosticOptions(
                (suppressedDiagnostics ?? []).ToDictionary(static id => id, static _ => ReportDiagnostic.Suppress)));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new GetIidInterceptorGenerator().AsSourceGenerator()], parseOptions: parseOptions, optionsProvider: new OptionsProvider(useUwp, generateInterceptors));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);
        Assert.IsEmpty(diagnostics, string.Join("\n", diagnostics));
        return (output, driver.GetRunResult());
    }

    private sealed class OptionsProvider(bool useUwp, bool? generateInterceptors) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(useUwp, generateInterceptors);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class Options(bool useUwp, bool? generateInterceptors) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (key == "build_property.CsWinRTGenerateGetIidInterceptors" && generateInterceptors is bool enabled)
            {
                value = enabled.ToString();

                return true;
            }

            value = useUwp.ToString();
            return key == "build_property.CsWinRTUseWindowsUIXamlProjections";
        }
    }
}

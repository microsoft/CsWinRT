// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WindowsRuntime.SourceGenerator.Tests;

public partial class Test_GetIidInterceptorGenerator
{
    private static readonly string[] SdkAuditProjectionAssemblies = ["Microsoft.Windows.SDK.NET", "Microsoft.Windows.UI.Xaml", "Microsoft.WinUI"];

    // Audits the default interface of every runtime class in the Windows SDK (including XAML), against independently computed IIDs
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SdkRuntimeClasses_DefaultInterfaces_MatchWinMdIids(bool useWinUI)
    {
        using WinMdIidOracle oracle = new(useWindowsUIXaml: !useWinUI);

        oracle.Add(FindWindowsWinmd());

        if (useWinUI)
        {
            foreach (string directory in GetAssemblyMetadata("WinUIMetadataDirectories").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (string path in Directory.EnumerateFiles(directory, "*.winmd").Order(StringComparer.OrdinalIgnoreCase))
                {
                    oracle.Add(path);
                }
            }
        }

        // The Windows SDK projection plus the XAML projection for the selected mode. Both are reference projections
        MetadataReference[] projections =
        [
            MetadataReference.CreateFromFile(FindReferenceProjection("Windows", "Microsoft.Windows.SDK.NET")),
            MetadataReference.CreateFromFile(useWinUI
                ? FindReferenceProjection("WinAppSDK", "Microsoft.WinUI")
                : FindReferenceProjection("Windows.UI.Xaml", "Microsoft.Windows.UI.Xaml"))
        ];

        CSharpCompilation baseCompilation = CSharpCompilation.Create(
            "SdkAuditBase",
            references: [.. GetReferences(), .. projections],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Dictionary<string, string> recordedDefaultInterfaces = [];

        foreach (MetadataReference projection in projections)
        {
            foreach (AttributeData attribute in ((IAssemblySymbol)baseCompilation.GetAssemblyOrModuleSymbol(projection)!).GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "WindowsRuntimeReferenceAssemblyMetadataAttribute" &&
                    attribute.ConstructorArguments is [{ Value: string key }, { Value: string value }] &&
                    key.StartsWith("CsWinRT.DefaultInterface.v1:", StringComparison.Ordinal))
                {
                    recordedDefaultInterfaces[key["CsWinRT.DefaultInterface.v1:".Length..]] = value;
                }
            }
        }

        List<(string Name, string Type)> classes = [];

        foreach (string name in oracle.GetRuntimeClassNames().Order(StringComparer.Ordinal))
        {
            // Classes that are custom-mapped, excluded, or defined by the other XAML flavor aren't part of the selected projections
            if (baseCompilation.GetTypeByMetadataName(name) is
                {
                    TypeKind: TypeKind.Class,
                    IsStatic: false,
                    DeclaredAccessibility: Accessibility.Public
                } symbol &&
                SdkAuditProjectionAssemblies.Contains(symbol.ContainingAssembly.Name))
            {
                classes.Add((name, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }
        }

        StringBuilder source = new("public static class Test\n{\n    public static void Main() { }\n");

        for (int i = 0; i < classes.Count; i++)
        {
            source.AppendLine($"    public static System.Guid C{i}() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<{classes[i].Type}>();");
            source.AppendLine($"    public static System.Guid L{i}() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<{classes[i].Type}>>();");
        }

        source.AppendLine("}");

        // Experimental and obsolete APIs are still projected, so they must be callable for the audit
        (Compilation compilation, GeneratorDriverRunResult result) = Run(
            source.ToString(),
            projections,
            OutputKind.ConsoleApplication,
            useUwp: !useWinUI,
            assemblyName: "SdkAuditConsumer",
            suppressedDiagnostics: ["CSWINRT3005"]);

        using MemoryStream stream = new();

        var emit = compilation.Emit(stream);

        Assert.IsTrue(emit.Success, string.Join("\n", emit.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Take(20)));

        Type test = Assembly.Load(stream.ToArray()).GetType("Test")!;

        List<string> mismatches = [];
        List<string> notIntercepted = [];

        for (int i = 0; i < classes.Count; i++)
        {
            (string name, _) = classes[i];

            string recorded = recordedDefaultInterfaces.GetValueOrDefault(name) ?? "<none>";
            Guid expectedClass = oracle.GetClassIid(name);
            Guid expectedList = WinMdIidOracle.ComputeIid(WinMdIidOracle.GetVectorSignature(oracle.GetClassSignature(name)));

            Check(test, $"C{i}", expectedClass, $"{name} => {recorded}");
            Check(test, $"L{i}", expectedList, $"IList<{name}> => {recorded}");
        }

        void Check(Type test, string method, Guid expected, string description)
        {
            Guid? actual;

            try
            {
                actual = (Guid?)test.GetMethod(method)!.Invoke(null, null);
            }
            catch (Exception)
            {
                notIntercepted.Add(description);

                return;
            }

            if (actual != expected)
            {
                mismatches.Add($"{description} (expected {expected}, actual {actual})");
            }
        }

        Assert.IsEmpty(mismatches, string.Join("\n", mismatches.Take(30)));
        Assert.IsEmpty(notIntercepted, string.Join("\n", notIntercepted.Take(30)));

        // Make sure the audit can't pass vacuously (e.g. if no projections or metadata were loaded)
        Assert.IsGreaterThan(4000, classes.Count);

        // Names must be plain (no 'global::' or ABI types), and custom-mapped default interfaces must use their .NET names
        Assert.IsTrue(recordedDefaultInterfaces.Values.All(static name => name.All(static c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '<' or '>' or ',' or ' ')));
        Assert.AreEqual("System.Collections.Generic.IDictionary<string, string>", recordedDefaultInterfaces["Windows.Foundation.Collections.StringMap"]);
    }

    // The projection output assembly is a type forwarder: the reference projection (which carries the metadata the generator reads) is in 'ref'
    private static string FindReferenceProjection(string projectFolder, string assemblyName)
    {
        string directory = Path.Combine(GetAssemblyMetadata("ProjectionsDirectory"), projectFolder, "bin");
        string configuration = $"{Path.DirectorySeparatorChar}{GetAssemblyMetadata("BuildConfiguration")}{Path.DirectorySeparatorChar}";

        // The same project can be built for different platforms, so take the most recent one
        return Directory.EnumerateFiles(directory, $"{assemblyName}.dll", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(Path.GetDirectoryName(path)) == "ref" && path.Contains(configuration, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .First();
    }

    private static string FindWindowsWinmd()
    {
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits", "10", "UnionMetadata");

        return Directory.EnumerateDirectories(root)
            .Select(static directory => (Directory: directory, Version: Version.TryParse(Path.GetFileName(directory), out Version? version) ? version : null))
            .Where(static item => item.Version is not null && File.Exists(Path.Combine(item.Directory, "Windows.winmd")))
            .OrderByDescending(static item => item.Version)
            .Select(static item => Path.Combine(item.Directory, "Windows.winmd"))
            .First();
    }

    private static string GetAssemblyMetadata(string key)
    {
        return typeof(Test_GetIidInterceptorGenerator).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(attribute => attribute.Key == key).Value!;
    }
}

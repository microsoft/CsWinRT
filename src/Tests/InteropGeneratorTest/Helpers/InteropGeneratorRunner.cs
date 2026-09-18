// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace InteropGeneratorTest.Helpers;

internal sealed class InteropGeneratorRunner : IDisposable
{
    private const string DictionarySource = """
        using System.Collections.Generic;
        using System.Collections.ObjectModel;

        public static class DictionaryTypes
        {
            public static readonly ReadOnlyDictionary<string, object> Objects = new(new Dictionary<string, object>());
            public static readonly ReadOnlyDictionary<string, string> Strings = new(new Dictionary<string, string>());
            public static readonly ReadOnlyDictionary<string, string>.KeyCollection Keys = Strings.Keys;
            public static readonly ReadOnlyDictionary<string, string>.ValueCollection Values = Strings.Values;
            public static readonly List<ReadOnlyDictionary<string, string>.KeyCollection> Lists = new();
            public static readonly ReadOnlyDictionary<string, string>.KeyCollection[] Arrays = [Keys];
        }
        """;

    private const string DistinctTypeSource = """
        namespace IdentityControl;
        public sealed class SameName<T> : System.IDisposable
        {
            public void Dispose() { }
        }
        public static class Factory
        {
            public static object Create() => new SameName<string>();
        }
        """;

    private readonly string[] referencePaths;
    private readonly string[] implementationPaths;
    private readonly string applicationPath;
    private readonly string sdkProjectionPath;

    public InteropGeneratorRunner(bool useFrameworkImplementations = false, bool overlappingFrameworkReferences = false)
    {
        Root = Directory.CreateTempSubdirectory("InteropGeneratorTest_").FullName;
        string referencesDirectory = Directory.CreateDirectory(Path.Combine(Root, "references")).FullName;

        referencePaths = Net100.ReferenceInfos.All.Select(reference =>
        {
            string path = Path.Combine(referencesDirectory, reference.FileName);
            File.WriteAllBytes(path, reference.ImageBytes);
            return path;
        }).ToArray();

        if (overlappingFrameworkReferences)
        {
            referencePaths = [
                .. referencePaths,
                Path.Combine(GetAssemblyMetadata("NetStandard20ReferenceDirectory"), "netstandard.dll")];
        }

        string current = Compile("CurrentTypes", DictionarySource);
        string forwarded = Compile("ForwardedTypes", DictionarySource);
        string implementation = Compile("ImplementationTypes", DictionarySource);

        RewriteDictionaryScope(forwarded, new AssemblyReference(
            "System.ObjectModel", new Version(8, 0, 0, 0), false,
            [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A]));
        RewriteDictionaryScope(implementation, new AssemblyReference(
            "System.Private.CoreLib", new Version(10, 0, 0, 0), false,
            [0x7C, 0xEC, 0x85, 0xD7, 0xBE, 0xA7, 0x79, 0x8E]));

        string firstDistinct = Compile("DistinctTypesA", DistinctTypeSource);
        string secondDistinct = Compile("DistinctTypesB", DistinctTypeSource);
        string standard = Compile("NetStandardTypes", DictionarySource + """

            namespace StandardInput
            {
                public sealed class Callback : System.IDisposable
                {
                    public void Dispose() { }
                }
            }
            """, references: Directory.GetFiles(GetAssemblyMetadata("NetStandard20ReferenceDirectory"), "*.dll")
                .Select(path => MetadataReference.CreateFromFile(path)));

        applicationPath = Compile("Application", """
            [assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]
            public static class Program { public static void Main() { } }
            """, OutputKind.ConsoleApplication);

        sdkProjectionPath = Compile("WinRT.Sdk.Projection", "");
        string runtimePath = typeof(Windows.Foundation.IStringable).Assembly.Location;
        string sdkReference = Compile("Microsoft.Windows.SDK.NET", """
            [assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssembly]
            namespace ProjectionInput;
            [WindowsRuntime.WindowsRuntimeType]
            public class Base { }
            [WindowsRuntime.WindowsRuntimeType]
            public class Derived : Base { }
            """, references: [.. Net100.References.All, MetadataReference.CreateFromFile(runtimePath)]);

        implementationPaths =
        [
            .. useFrameworkImplementations ? GetFrameworkImplementationPaths() : referencePaths,
            runtimePath, sdkReference, current, forwarded, firstDistinct, secondDistinct, standard,
            .. useFrameworkImplementations ? new[] { implementation } : []
        ];
    }

    public string Root { get; }

    public async Task<string> GenerateAsync(
        string name,
        bool reverseInputs = false,
        int parallelism = 1,
        string marshallingMode = "Minimal",
        bool optInNetStandard = false)
    {
        (int exitCode, string log, string outputPath) = await RunAsync(name, reverseInputs, parallelism, marshallingMode, optInNetStandard);
        Assert.AreEqual(0, exitCode, log);
        Assert.IsTrue(File.Exists(outputPath), "The generator did not produce an interop assembly.");
        return outputPath;
    }

    private async Task<(int ExitCode, string Log, string OutputPath)> RunAsync(
        string name,
        bool reverseInputs,
        int parallelism,
        string marshallingMode,
        bool optInNetStandard)
    {
        string directory = Directory.CreateDirectory(Path.Combine(Root, name)).FullName;
        string responsePath = Path.Combine(directory, "interop.rsp");
        IEnumerable<string> references = reverseInputs ? referencePaths.Reverse() : referencePaths;
        IEnumerable<string> implementations = reverseInputs ? implementationPaths.Reverse() : implementationPaths;

        File.WriteAllText(responsePath, $"""
            --reference-assembly-paths {string.Join(",", references)}
            --implementation-assembly-paths {string.Join(",", implementations)}
            --output-assembly-path {applicationPath}
            --winrt-sdk-projection-assembly-path {sdkProjectionPath}
            --generated-assembly-directory {directory}
            --use-windows-ui-xaml-projections False
            --marshalling-mode {marshallingMode}
            {(optInNetStandard ? "--marshalling-enabled-assembly-names NetStandardTypes" : "")}
            --generate-collection-changed-list-vtables False
            --validate-winrt-runtime-assembly-version True
            --validate-winrt-runtime-dll-version-2-references True
            --enable-incremental-generation False
            --treat-warnings-as-errors True
            --max-degrees-of-parallelism {parallelism}
            """);

        string toolPath = GetAssemblyMetadata("InteropGeneratorAssemblyPath");

        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(Path.GetFullPath(toolPath));
        startInfo.ArgumentList.Add("@" + responsePath);

        using Process process = Process.Start(startInfo)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        Task completion = process.WaitForExitAsync();

        if (await Task.WhenAny(completion, Task.Delay(TimeSpan.FromMinutes(3))) != completion)
        {
            process.Kill(entireProcessTree: true);
            await completion;
            Assert.Fail($"Interop generation timed out.\n{await output}\n{await error}");
        }

        return (process.ExitCode, $"{await output}\n{await error}", Path.Combine(directory, "WinRT.Interop.dll"));
    }

    public (RuntimeContext Context, ModuleDefinition Module) LoadOutput(string path)
    {
        Assert.IsTrue(TargetRuntimeProber.TryGetLikelyTargetRuntime(PEImage.FromFile(applicationPath), out DotNetRuntimeInfo runtime));
        PathAssemblyResolver resolver = new([
            .. GetFrameworkImplementationPaths(),
            .. implementationPaths.Except(referencePaths),
            applicationPath, sdkProjectionPath, path]);
        RuntimeContext context = new(runtime, resolver);
        return (context, context.LoadAssembly(path).Modules[0]);
    }

    public static string ResolvedTypeKey(TypeSignature signature, RuntimeContext context)
    {
        return signature switch
        {
            GenericInstanceTypeSignature generic => $"{DefinitionKey(generic.GenericType, context)}<{string.Join(",", generic.TypeArguments.Select(argument => ResolvedTypeKey(argument, context)))}>",
            SzArrayTypeSignature array => ResolvedTypeKey(array.BaseType, context) + "[]",
            _ => DefinitionKey(signature, context)
        };
    }

    public static IEnumerable<TypeSignature> EnumerateTypes(TypeSignature signature)
    {
        yield return signature;

        if (signature is GenericInstanceTypeSignature generic)
        {
            foreach (TypeSignature argument in generic.TypeArguments.SelectMany(EnumerateTypes))
            {
                yield return argument;
            }
        }
        else if (signature is SzArrayTypeSignature array)
        {
            foreach (TypeSignature element in EnumerateTypes(array.BaseType))
            {
                yield return element;
            }
        }
    }

    public void Dispose()
    {
        Directory.Delete(Root, recursive: true);
    }

    private static string GetAssemblyMetadata(string key)
    {
        return typeof(InteropGeneratorRunner).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == key).Value!;
    }

    private static string[] GetFrameworkImplementationPaths()
    {
        return Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll");
    }

    private static string DefinitionKey(ITypeDescriptor type, RuntimeContext context)
    {
        Assert.IsTrue(type.TryResolve(context, out TypeDefinition? definition), $"Could not resolve '{type}'.");
        return $"{definition!.DeclaringModule!.Assembly!.FullName}:{definition.FullName}";
    }

    private string Compile(
        string name,
        string source,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        IEnumerable<MetadataReference>? references = null)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            references ?? Net100.References.All,
            new CSharpCompilationOptions(outputKind, deterministic: true));

        string path = Path.Combine(Root, name + ".dll");
        EmitResult result = compilation.Emit(path);
        Assert.IsTrue(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return path;
    }

    private static void RewriteDictionaryScope(string path, AssemblyReference scope)
    {
        ModuleDefinition module = ModuleDefinition.FromFile(path);
        TypeReference dictionary = module.EnumerateTableMembers<TypeReference>(TableIndex.TypeRef)
            .Single(type => type.Namespace == "System.Collections.ObjectModel" && type.Name == "ReadOnlyDictionary`2");

        Assert.AreEqual("System.Runtime", dictionary.Scope!.GetAssembly()!.Name!.ToString());
        dictionary.Scope = scope;

        // Model valid inputs from older reference packs and implementation metadata, not patched generator output.
        using MemoryStream stream = new();
        module.Write(stream);
        File.WriteAllBytes(path, stream.ToArray());
    }
}

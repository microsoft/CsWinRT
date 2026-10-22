// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
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

    private const string EnumerableAliasSource = """
        using System.Collections;
        using System.Collections.Generic;
        using Windows.Foundation;

        public sealed class EnumerableAliasInput : IEnumerable<IStringable>
        {
            public static IEnumerable<IStringable>[] Arrays = [];

            public IEnumerator<IStringable> GetEnumerator() => new List<IStringable>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        """;

    private const string GroupingMemberSource = """
        public sealed class Shared{0} : {1}
        {{
            public void Dispose() {{ }}
            public IEnumerator<IStringable> GetEnumerator() => ((IEnumerable<IStringable>)Array.Empty<IStringable>()).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }}
        """;

    private const string EnumerableProviderSource = """
        using System.Collections.Generic;
        using Windows.Foundation.Collections;

        public static class EnumerableProviderRoots
        {
            public static IList<int> List;
            public static IReadOnlyList<int> ReadOnlyList;
            public static IList<KeyValuePair<int, string>> Pairs;
            public static IDictionary<int, string> Dictionary;
            public static IReadOnlyDictionary<int, string> ReadOnlyDictionary;
            public static ICollection<KeyValuePair<int, string>> PairCollection;
            public static IReadOnlyCollection<KeyValuePair<int, string>> ReadOnlyPairCollection;
            public static IObservableVector<int> ObservableVector;
            public static IObservableMap<int, string> ObservableMap;
            internal static IList<InternalElement> InternalList;
        }

        [WindowsRuntime.WindowsRuntimeType]
        internal enum InternalElement { Zero }
        """;

    private readonly string[] referencePaths;
    private readonly string[] implementationPaths;
    private readonly string applicationPath;
    private readonly string sdkProjectionPath;
    private readonly string? componentProjectionPath;
    private readonly string? iidLibraryPath;
    private int msbuildInvocation;
    private bool publishFailed;

    public InteropGeneratorRunner(
        bool useFrameworkImplementations = false,
        bool overlappingFrameworkReferences = false,
        bool forwardedEnumerableAliases = false,
        bool ccwGroupingCases = false,
        bool iidNameCollisionCases = false,
        bool enumerableProviders = false,
        string? applicationSource = null,
        string sdkProjectionSource = "",
        string? componentProjectionSource = null,
        string? iidLibrarySource = null,
        bool publicSignIidLibrary = false,
        string? sdkReferenceSource = null,
        string iidLibraryName = "IidLibrary",
        bool forwardUriToFutureAssembly = false)
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

        string runtimePath = typeof(Windows.Foundation.IStringable).Assembly.Location;
        MetadataReference[] runtimeReferences = [.. Net100.References.All, MetadataReference.CreateFromFile(runtimePath)];
        sdkProjectionPath = Compile("WinRT.Sdk.Projection", sdkProjectionSource, references: runtimeReferences);

        if (iidLibrarySource is not null)
        {
            byte[]? publicKey = publicSignIidLibrary
                ? AssemblyName.GetAssemblyName(referencePaths.Single(path => Path.GetFileName(path) == "System.Runtime.dll")).GetPublicKey()
                : null;
            iidLibraryPath = Compile(iidLibraryName, iidLibrarySource,
                references: [.. runtimeReferences, MetadataReference.CreateFromFile(sdkProjectionPath)], publicKey: publicKey);
        }

        applicationPath = Compile("Application", applicationSource ?? """
            public static class Program { public static void Main() { } }
            """, OutputKind.ConsoleApplication,
            [
                .. runtimeReferences, MetadataReference.CreateFromFile(sdkProjectionPath),
                .. iidLibraryPath is not null ? new[] { MetadataReference.CreateFromFile(iidLibraryPath) } : []
            ]);

        if (componentProjectionSource is not null)
        {
            componentProjectionPath = Compile("WinRT.Component", componentProjectionSource,
                references: [.. runtimeReferences, MetadataReference.CreateFromFile(applicationPath)]);
        }

        string sdkReference = Compile("Microsoft.Windows.SDK.NET", sdkReferenceSource ?? """
            [assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssembly]
            namespace ProjectionInput;
            [WindowsRuntime.WindowsRuntimeType]
            public class Base { }
            [WindowsRuntime.WindowsRuntimeType]
            public class Derived : Base { }
            """, references: [.. Net100.References.All, MetadataReference.CreateFromFile(runtimePath)]);

        string[] enumerableAliases = [];
        string[] enumerableRoots = [];

        if (enumerableProviders)
        {
            enumerableRoots = [Compile("EnumerableProviderRoots", EnumerableProviderSource,
                references: [.. Net100.References.All, MetadataReference.CreateFromFile(runtimePath)])];
        }

        if (forwardedEnumerableAliases)
        {
            MetadataReference[] references = [.. Net100.References.All, MetadataReference.CreateFromFile(runtimePath)];
            string coreLib = Compile("CoreLibEnumerable", EnumerableAliasSource, references: references);
            string facade = Compile("FacadeEnumerable", EnumerableAliasSource, references: references);

            RewriteTypeScope(
                coreLib, "System.Collections.Generic", "IEnumerable`1",
                new AssemblyReference(
                    "System.Private.CoreLib", new Version(10, 0, 0, 0), false,
                    [0x7C, 0xEC, 0x85, 0xD7, 0xBE, 0xA7, 0x79, 0x8E]));

            enumerableAliases = [coreLib, facade];
        }

        string[] groupingTypes = [];

        if (ccwGroupingCases)
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using Windows.Foundation;

                namespace GroupingInput;

                """ + string.Join(Environment.NewLine, Enumerable.Range(0, 32)
                .Select(index => string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    GroupingMemberSource,
                    index,
                    index % 2 == 0 ? "IEnumerable<IStringable>, IDisposable" : "IDisposable, IEnumerable<IStringable>")));

            // A separate declaration order still describes the same semantic interface set
            source += """

                public sealed class DisposableOnly : System.IDisposable
                {
                    public void Dispose() { }
                }

                public sealed class ExplicitStringable : Windows.Foundation.IStringable
                {
                    public override string ToString() => "explicit";
                }
                """;
            groupingTypes = [Compile("GroupingTypes", source,
                references: [.. Net100.References.All, MetadataReference.CreateFromFile(runtimePath)])];
        }

        string[] iidNameCollisions = [];

        if (iidNameCollisionCases)
        {
            string path = Compile("IidNameCollisions", """
                namespace IidNames.A
                {
                    [WindowsRuntime.WindowsRuntimeType]
                    public enum Value { First }
                }

                namespace IidNames_B
                {
                    [WindowsRuntime.WindowsRuntimeType]
                    public enum Value { Second }
                }

                public sealed class FirstList : System.Collections.Generic.List<IidNames.A.Value>;
                public sealed class SecondList : System.Collections.Generic.List<IidNames_B.Value>;
                """, references: [.. Net100.References.All, MetadataReference.CreateFromFile(runtimePath)]);
            ModuleDefinition module = ModuleDefinition.FromFile(path);
            // Distinct metadata namespaces collide after '.' is replaced with '-' during name mangling.
            module.TopLevelTypes.Single(type => type.Namespace == "IidNames_B").Namespace = "IidNames-A";
            using MemoryStream stream = new();
            module.Write(stream);
            File.WriteAllBytes(path, stream.ToArray());
            iidNameCollisions = [path];
        }

        if (forwardUriToFutureAssembly)
        {
            string future = Compile("FutureFramework", "namespace System; public sealed class Uri;");
            string facadePath = referencePaths.Single(path => Path.GetFileName(path) == "System.Runtime.dll");
            ModuleDefinition facade = ModuleDefinition.FromFile(facadePath);
            TypeDefinition uri = facade.TopLevelTypes.Single(type => type.FullName == "System.Uri");
            AssemblyReference futureAssembly = new("FutureFramework", new Version(0, 0, 0, 0));
            ForwardedFrameworkTypeImporter importer = new(facade, uri, new TypeReference(futureAssembly, "System", "Uri"));

            foreach (TypeDefinition type in facade.GetAllTypes())
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    method.Signature = importer.ImportMethodSignature(method.Signature!);
                }

                foreach (PropertyDefinition property in type.Properties)
                {
                    property.Signature = importer.ImportPropertySignature(property.Signature!);
                }

                foreach (FieldDefinition field in type.Fields)
                {
                    field.Signature = importer.ImportFieldSignature(field.Signature!);
                }
            }

            facade.TopLevelTypes.Remove(uri);
            facade.ExportedTypes.Add(new ExportedType(
                implementation: futureAssembly,
                ns: "System",
                name: "Uri")
            {
                Attributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes.Forwarder
            });
            using MemoryStream stream = new();
            facade.Write(stream);
            File.WriteAllBytes(facadePath, stream.ToArray());
            referencePaths = [.. referencePaths, future];
        }

        implementationPaths =
        [
            .. useFrameworkImplementations ? GetFrameworkImplementationPaths() : referencePaths,
            runtimePath, sdkReference, current, forwarded, firstDistinct, secondDistinct, standard,
            .. enumerableAliases,
            .. groupingTypes,
            .. iidNameCollisions,
            .. enumerableRoots,
            .. iidLibraryPath is not null ? new[] { iidLibraryPath } : [],
            .. useFrameworkImplementations ? new[] { implementation } : []
        ];
    }

    public string Root { get; }

    public void SetPrivateAttribute(int value, bool implementPrivateInterface = false)
    {
        string attributeAssembly = Compile("PrivateAttributes", """
            namespace MissingAttributes;

            public enum Mode { Enabled }

            public sealed class MarkerAttribute(Mode mode, System.Type type) : System.Attribute
            {
                public int Value { get; set; }
            }

            public sealed class Argument;
            public interface IPrivate;
            """);

        _ = Compile("CurrentTypes", DictionarySource + $$"""

            [MissingAttributes.Marker(MissingAttributes.Mode.Enabled, typeof(MissingAttributes.Argument), Value = {{value}})]
            public sealed class PrivateAttributeInput : System.IDisposable{{(implementPrivateInterface ? ", MissingAttributes.IPrivate" : "")}}
            {
                public void Dispose() { }
            }
            """, references: [.. Net100.References.All, MetadataReference.CreateFromFile(attributeAssembly)]);

        // The compiler-only dependency is absent from the consuming application's generator inputs
        File.Delete(attributeAssembly);
    }

    public string LastGeneratorLog { get; private set; } = "";

    public void RewriteApplicationTypeScope(string typeNamespace, string typeName, string assemblyName)
    {
        ModuleDefinition module = ModuleDefinition.FromFile(applicationPath);
        TypeReference type = module.EnumerateTableMembers<TypeReference>(TableIndex.TypeRef)
            .Single(type => type.Namespace == typeNamespace && type.Name == typeName);
        type.Scope = new AssemblyReference(assemblyName, new Version(0, 0, 0, 0));
        using MemoryStream stream = new();
        module.Write(stream);
        File.WriteAllBytes(applicationPath, stream.ToArray());
    }

    public void RewriteLibraryTypeScope(string typeNamespace, string typeName, string assemblyName)
    {
        Assert.IsNotNull(iidLibraryPath);
        RewriteTypeScope(iidLibraryPath, typeNamespace, typeName, new AssemblyReference(assemblyName, new Version(0, 0, 0, 0)));
    }

    public void StageSdkTypeForwarder(string outputPath, params string[] typeNames)
    {
        string source = string.Join(Environment.NewLine, typeNames.Select(name =>
            $"[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof({name}))]"));
        string forwarder = Compile("Microsoft.Windows.SDK.NET.Forwarder", source,
            references: [.. Net100.References.All, MetadataReference.CreateFromFile(sdkProjectionPath)]);
        ModuleDefinition module = ModuleDefinition.FromFile(forwarder);
        module.Assembly!.Name = "Microsoft.Windows.SDK.NET";
        module.Write(Path.Combine(Path.GetDirectoryName(outputPath)!, "Microsoft.Windows.SDK.NET.dll"));
    }

    public async Task<string> GenerateAsync(
        string name,
        bool reverseInputs = false,
        int parallelism = 1,
        string marshallingMode = "Minimal",
        bool optInNetStandard = false,
        bool? analyzeNetStandardAssemblies = null,
        string? debugReproDirectory = null,
        string[]? marshallingDisabledAssemblyNames = null,
        bool treatWarningsAsErrors = true,
        string? logDirectory = null,
        bool useWindowsUIXamlProjections = false)
    {
        (int exitCode, string log, string outputPath) = await RunAsync(
            name, reverseInputs, parallelism, marshallingMode, optInNetStandard, analyzeNetStandardAssemblies,
            debugReproDirectory, marshallingDisabledAssemblyNames, treatWarningsAsErrors, logDirectory, useWindowsUIXamlProjections);
        Assert.AreEqual(0, exitCode, log);
        Assert.IsTrue(File.Exists(outputPath), "The generator did not produce an interop assembly.");
        return outputPath;
    }

    public async Task<string> AssertFailureAsync(string name, string error, bool reverseInputs = false, int parallelism = 1)
    {
        (int exitCode, string log, string outputPath) = await RunAsync(
            name, reverseInputs, parallelism, marshallingMode: "Minimal", optInNetStandard: false,
            analyzeNetStandardAssemblies: null, debugReproDirectory: null, marshallingDisabledAssemblyNames: null,
            treatWarningsAsErrors: false, logDirectory: null, useWindowsUIXamlProjections: false);

        Assert.AreNotEqual(0, exitCode, log);
        StringAssert.Contains(log, error);
        Assert.IsFalse(File.Exists(outputPath), "Invalid generated names must fail before publishing the interop assembly.");
        return log;
    }

    private async Task<(int ExitCode, string Log, string OutputPath)> RunAsync(
        string name,
        bool reverseInputs,
        int parallelism,
        string marshallingMode,
        bool optInNetStandard,
        bool? analyzeNetStandardAssemblies,
        string? debugReproDirectory,
        string[]? marshallingDisabledAssemblyNames,
        bool treatWarningsAsErrors,
        string? logDirectory,
        bool useWindowsUIXamlProjections)
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
            {(componentProjectionPath is not null ? $"--winrt-component-assembly-path {componentProjectionPath}" : "")}
            --generated-assembly-directory {directory}
            --use-windows-ui-xaml-projections {useWindowsUIXamlProjections}
            --marshalling-mode {marshallingMode}
            {(analyzeNetStandardAssemblies is { } analyze ? $"--analyze-net-standard-assemblies {analyze}" : "")}
            {(optInNetStandard ? "--marshalling-enabled-assembly-names NetStandardTypes" : "")}
            {(marshallingDisabledAssemblyNames is { Length: > 0 } ? $"--marshalling-disabled-assembly-names {string.Join(",", marshallingDisabledAssemblyNames)}" : "")}
            {(debugReproDirectory is not null ? $"--debug-repro-directory {debugReproDirectory}" : "")}
            {(logDirectory is not null ? $"--log-directory {logDirectory}" : "")}
            --generate-collection-changed-list-vtables False
            --validate-winrt-runtime-assembly-version True
            --validate-winrt-runtime-dll-version-2-references True
            --enable-incremental-generation False
            --treat-warnings-as-errors {treatWarningsAsErrors}
            --max-degrees-of-parallelism {parallelism}
            """);

        (int exitCode, string log) = await InvokeGeneratorAsync(responsePath);
        LastGeneratorLog = log;

        return (exitCode, log, Path.Combine(directory, "WinRT.Interop.dll"));
    }

    public static Task<(int ExitCode, string Log)> InvokeGeneratorAsync(string inputPath)
    {
        string generatorPath = Path.GetFullPath(
            Environment.GetEnvironmentVariable("CSWINRT_TEST_INTEROP_GENERATOR") ??
            GetAssemblyMetadata("InteropGeneratorAssemblyPath"));
        string extension = Path.GetExtension(generatorPath);
        ProcessStartInfo startInfo;

        if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            startInfo = CreateProcessStartInfo();
            startInfo.ArgumentList.Add(generatorPath);
        }
        else if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            startInfo = new(generatorPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        }
        else
        {
            throw new NotSupportedException($"Expected a .dll or .exe interop generator, but got '{generatorPath}'.");
        }

        startInfo.ArgumentList.Add(Path.GetExtension(inputPath) == ".zip" ? inputPath : "@" + inputPath);

        return RunProcessAsync(startInfo);
    }

    public async Task RunApplicationAsync(string interopPath)
    {
        string directory = Path.GetDirectoryName(interopPath)!;
        string application = Path.Combine(directory, "Application.dll");
        File.Copy(applicationPath, application);
        File.Copy(sdkProjectionPath, Path.Combine(directory, "WinRT.Sdk.Projection.dll"));
        File.Copy(typeof(Windows.Foundation.IStringable).Assembly.Location, Path.Combine(directory, "WinRT.Runtime.dll"));
        if (componentProjectionPath is not null)
        {
            File.Copy(componentProjectionPath, Path.Combine(directory, "WinRT.Component.dll"));
        }
        if (iidLibraryPath is not null)
        {
            File.Copy(iidLibraryPath, Path.Combine(directory, Path.GetFileName(iidLibraryPath)));
        }
        foreach (string path in implementationPaths.Except(referencePaths))
        {
            string destination = Path.Combine(directory, Path.GetFileName(path));
            if (!File.Exists(destination))
            {
                File.Copy(path, destination);
            }
        }
        File.WriteAllText(Path.Combine(directory, "Application.runtimeconfig.json"), """
            {"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}
            """);

        ProcessStartInfo startInfo = CreateProcessStartInfo();
        startInfo.ArgumentList.Add(application);
        (int exitCode, string log) = await RunProcessAsync(startInfo);
        Assert.AreEqual(0, exitCode, log);
    }

    public async Task<string> PublishApplicationAsync(
        string interopPath,
        bool nativeAot,
        bool isolateUnsupportedTrimTargets = false,
        string? expectedPublishFailure = null)
    {
        string directory = Path.GetDirectoryName(interopPath)!;
        string projectDirectory = Directory.CreateDirectory(Path.Combine(directory, "publish-project")).FullName;
        string projectPath = Path.Combine(projectDirectory, "IidPublished.csproj");
        string publishedInteropPath = interopPath;
        if (isolateUnsupportedTrimTargets)
        {
            // ILLink 10.0 cannot resolve the existing marshalling group's array trim targets.
            // Remove only those invalid dependency sources; CoreCLR and Native AOT use the original sidecar.
            string references = Directory.CreateDirectory(Path.Combine(projectDirectory, "references")).FullName;
            publishedInteropPath = Path.Combine(references, "WinRT.Interop.dll");
            ModuleDefinition interop = ModuleDefinition.FromFile(interopPath);
            int iidEntries = interop.Assembly!.CustomAttributes.Count(attribute =>
                attribute.Constructor?.DeclaringType?.FullName.Contains("WindowsRuntimeIidTypeMapGroup", StringComparison.Ordinal) is true);
            foreach (CustomAttribute attribute in interop.Assembly!.CustomAttributes.ToArray())
            {
                string name = attribute.Constructor?.DeclaringType?.FullName ?? "";
                TypeSignature? dependencySource = name.StartsWith("System.Runtime.InteropServices.TypeMapAttribute`1", StringComparison.Ordinal) &&
                    attribute.Signature?.FixedArguments.Count == 3
                    ? attribute.Signature.FixedArguments[2].Element as TypeSignature
                    : name.StartsWith("System.Runtime.InteropServices.TypeMapAssociationAttribute`1", StringComparison.Ordinal) &&
                        attribute.Signature?.FixedArguments.Count == 2
                        ? attribute.Signature.FixedArguments[0].Element as TypeSignature
                        : null;
                if (dependencySource is not null && dependencySource is not (CorLibTypeSignature or TypeDefOrRefSignature or GenericInstanceTypeSignature))
                {
                    Assert.IsFalse(name.Contains("WindowsRuntimeIidTypeMapGroup", StringComparison.Ordinal),
                        "IID map entries must never require rewriting for the ILLink type-specification bug.");
                    interop.Assembly.CustomAttributes.Remove(attribute);
                }
            }
            Assert.AreEqual(iidEntries, interop.Assembly.CustomAttributes.Count(attribute =>
                attribute.Constructor?.DeclaringType?.FullName.Contains("WindowsRuntimeIidTypeMapGroup", StringComparison.Ordinal) is true));
            interop.Write(publishedInteropPath);
        }
        XDocument project = new(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup",
                new XElement("OutputType", "Exe"),
                new XElement("TargetFramework", "net10.0"),
                new XElement("RuntimeIdentifier", "win-x64"),
                new XElement("SelfContained", "true"),
                new XElement("PublishTrimmed", "true"),
                new XElement("PublishAot", nativeAot.ToString().ToLowerInvariant()),
                new XElement("TreatWarningsAsErrors", "true"),
                new XElement("TrimmerSingleWarn", "false"),
                new XElement("IlcResilient", "false"),
                new XElement("IlcGenerateMapFile", nativeAot.ToString().ToLowerInvariant()),
                new XElement("NoWarn", "CA1416")),
            new XElement("ItemGroup", Directory.GetFiles(directory, "*.dll")
                .Select(path => new XElement("Reference", new XAttribute("Include", Path.GetFileNameWithoutExtension(path)),
                    new XElement("HintPath", Path.GetFileName(path) == "WinRT.Interop.dll" ? publishedInteropPath : path))))));
        project.Save(projectPath);
        File.WriteAllText(Path.Combine(projectDirectory, "Entry.cs"), """
            [assembly: System.Runtime.InteropServices.TypeMapAssemblyTarget<WindowsRuntime.InteropServices.WindowsRuntimeIidTypeMapGroup>("WinRT.Interop")]
            public static class PublishedEntry
            {
                public static int Main() => global::Program.Main();
            }
            """);

        string publishDirectory = Path.Combine(projectDirectory, "published");
        ProcessStartInfo startInfo = CreateProcessStartInfo();
        foreach (string argument in new[] { "publish", projectPath, "-c", "Release", "-o", publishDirectory,
            expectedPublishFailure is null ? "-v:quiet" : "-v:diagnostic", "-m",
            "-bl:" + Path.Combine(projectDirectory, "publish-{}.binlog") })
        {
            startInfo.ArgumentList.Add(argument);
        }

        (int exitCode, string log) = await RunProcessAsync(startInfo);
        if (expectedPublishFailure is not null)
        {
            Assert.AreNotEqual(0, exitCode, "ILLink now accepts array type-map dependencies; remove the known-issue test workaround.");
            Assert.Contains(expectedPublishFailure, log);
            return projectDirectory;
        }
        if (exitCode != 0)
        {
            publishFailed = true;
            System.Console.WriteLine($"Publish failure artifacts: {projectDirectory}");
        }
        Assert.AreEqual(0, exitCode, log);

        ProcessStartInfo application = new(Path.Combine(publishDirectory, "IidPublished.exe"))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        (exitCode, log) = await RunProcessAsync(application);
        Assert.AreEqual(0, exitCode, log);

        return publishDirectory;
    }

    private static ProcessStartInfo CreateProcessStartInfo()
    {
        return new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
    }

    private static async Task<(int ExitCode, string Log)> RunProcessAsync(ProcessStartInfo startInfo)
    {
        using Process process = Process.Start(startInfo)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        Task completion = process.WaitForExitAsync();

        if (await Task.WhenAny(completion, Task.Delay(TimeSpan.FromMinutes(3))) != completion)
        {
            process.Kill(entireProcessTree: true);
            await completion;
            Assert.Fail($"Process timed out.\n{await output}\n{await error}");
        }

        return (process.ExitCode, $"{await output}\n{await error}");
    }

    public string CreateMSBuildProject()
    {
        string directory = Directory.CreateDirectory(Path.Combine(Root, "msbuild")).FullName;
        string outputDirectory = Directory.CreateDirectory(Path.Combine(directory, "output")).FullName;
        string projectPath = Path.Combine(directory, "Discovery.proj");
        XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
        string toolsDirectory = Path.GetDirectoryName(Path.GetFullPath(GetAssemblyMetadata("InteropGeneratorAssemblyPath")))!;

        XDocument project = new(new XElement(ns + "Project",
            new XElement(ns + "PropertyGroup",
                new XElement(ns + "IntermediateOutputPath", outputDirectory + Path.DirectorySeparatorChar),
                new XElement(ns + "CsWinRTGenerateInteropAssembly", "true"),
                new XElement(ns + "CsWinRTGenTasksAssembly", Path.GetFullPath(GetAssemblyMetadata("GeneratorTasksAssemblyPath"))),
                new XElement(ns + "CsWinRTInteropGenEffectiveToolsDirectory", toolsDirectory),
                new XElement(ns + "CsWinRTToolsArchitecture", "AnyCPU"),
                new XElement(ns + "CsWinRTSwapRuntimeReferenceAssembly", "false"),
                new XElement(ns + "CsWinRTUseWindowsUIXamlProjections", "false"),
                new XElement(ns + "CsWinRTMarshallingMode", "strict"),
                new XElement(ns + "CsWinRTGenerateCollectionChangedListVtables", "false"),
                new XElement(ns + "CsWinRTGeneratorTreatWarningsAsErrors", "true")),
            new XElement(ns + "ItemGroup",
                new XElement(ns + "ReferencePathWithRefAssemblies", new XAttribute("Include", string.Join(";", referencePaths))),
                new XElement(ns + "ReferencePath", new XAttribute("Include", string.Join(";", implementationPaths))),
                new XElement(ns + "IntermediateAssembly", new XAttribute("Include", applicationPath)),
                new XElement(ns + "CsWinRTMarshallingEnabledAssembly", new XAttribute("Include", "NetStandardTypes")),
                new XElement(ns + "CsWinRTMarshallingDisabledAssembly",
                    new XAttribute("Include", "$(TestMarshallingDisabledAssembly)"),
                    new XAttribute("Condition", "'$(TestMarshallingDisabledAssembly)' != ''"))),
            new XElement(ns + "Import", new XAttribute("Project", Path.GetFullPath(GetAssemblyMetadata("CsWinRTTargetsPath")))),
            new XElement(ns + "PropertyGroup",
                new XElement(ns + "_CsWinRTSdkProjectionAssemblyPath", sdkProjectionPath)),

            // Inputs are already compiled and resolved; stub preparation outside the interop producer
            new[] { "ResolveAssemblyReferences", "FindReferenceAssembliesForReferences", "CoreCompile",
                "CsWinRTRemoveWinMDReferences", "CsWinRTResolveWindowsMetadata",
                "_RunCsWinRTMergedProjectionGenerator", "_RunCsWinRTComponentProjectionGenerator" }
                .Select(name => new XElement(ns + "Target", new XAttribute("Name", name))),
            new XElement(ns + "Target",
                new XAttribute("Name", "Run"),
                new XAttribute("DependsOnTargets", "_RunCsWinRTInteropGenerator"),
                new XElement(ns + "Message",
                    new XAttribute("Importance", "High"),
                    new XAttribute("Text", "MSBuild parent DOTNET_gcServer=$([System.Environment]::GetEnvironmentVariable('DOTNET_gcServer'))")))));

        project.Save(projectPath);

        return projectPath;
    }

    public async Task<string> RunMSBuildAsync(
        string projectPath,
        bool? analyzeNetStandardAssemblies,
        string? disabledAssemblyName = null,
        bool? treatWarningsAsErrors = null,
        string? logDirectory = null,
        string? debugReproDirectory = null,
        string? enableServerGC = null,
        string? gcServerEnvironment = null,
        int? maxDegreesOfParallelism = null,
        bool expectFailure = false)
    {
        string directory = Path.GetDirectoryName(projectPath)!;
        string binlog = Path.Combine(directory, $"build-{++msbuildInvocation}.binlog");
        ProcessStartInfo startInfo = CreateProcessStartInfo();
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-t:Run");
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-v:detailed");
        startInfo.ArgumentList.Add("-bl:" + binlog);

        if (enableServerGC is not null)
        {
            startInfo.ArgumentList.Add("-p:CsWinRTEnableInteropGeneratorServerGC=" + enableServerGC);
        }

        if (maxDegreesOfParallelism is { } parallelism)
        {
            startInfo.ArgumentList.Add("-p:CsWinRTGeneratorMaxDegreesOfParallelism=" + parallelism);
        }

        if (gcServerEnvironment is not null)
        {
            if (gcServerEnvironment.Length == 0)
            {
                startInfo.Environment.Remove("DOTNET_gcServer");
            }
            else
            {
                startInfo.Environment["DOTNET_gcServer"] = gcServerEnvironment;
            }
        }

        if (analyzeNetStandardAssemblies is { } analyze)
        {
            startInfo.ArgumentList.Add("-p:CsWinRTAnalyzeNetStandardAssemblies=" + analyze.ToString().ToLowerInvariant());
        }

        if (disabledAssemblyName is not null)
        {
            startInfo.ArgumentList.Add("-p:TestMarshallingDisabledAssembly=" + disabledAssemblyName);
        }

        if (treatWarningsAsErrors is { } treatAsErrors)
        {
            startInfo.ArgumentList.Add("-p:CsWinRTGeneratorTreatWarningsAsErrors=" + treatAsErrors.ToString().ToLowerInvariant());
        }

        if (logDirectory is not null)
        {
            startInfo.ArgumentList.Add("-p:CsWinRTGeneratorLogDirectory=" + logDirectory);
        }

        if (debugReproDirectory is not null)
        {
            startInfo.ArgumentList.Add("-p:CsWinRTGeneratorDebugReproDirectory=" + debugReproDirectory);
        }

        (int exitCode, string log) = await RunProcessAsync(startInfo);
        File.WriteAllText(Path.ChangeExtension(binlog, ".log"), log);
        Assert.IsTrue(File.Exists(binlog), "MSBuild must produce its diagnostic log.");

        if (expectFailure)
        {
            Assert.AreNotEqual(0, exitCode, log);
            return log;
        }

        Assert.AreEqual(0, exitCode, log);

        return Path.Combine(directory, "output", "WinRT.Interop.dll");
    }

    public (RuntimeContext Context, ModuleDefinition Module) LoadOutput(string path)
    {
        Assert.IsTrue(TargetRuntimeProber.TryGetLikelyTargetRuntime(PEImage.FromFile(applicationPath), out DotNetRuntimeInfo runtime));
        PathAssemblyResolver resolver = new([
            .. GetFrameworkImplementationPaths(),
            .. implementationPaths.Except(referencePaths),
            applicationPath, sdkProjectionPath, path,
            .. componentProjectionPath is not null ? new[] { componentProjectionPath } : []]);
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
        if (!publishFailed)
        {
            Directory.Delete(Root, recursive: true);
        }
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
        IEnumerable<MetadataReference>? references = null,
        byte[]? publicKey = null)
    {
        CSharpCompilationOptions options = new(outputKind, deterministic: true, allowUnsafe: true);
        if (publicKey is not null)
        {
            options = options.WithCryptoPublicKey(ImmutableArray.CreateRange(publicKey)).WithPublicSign(true);
        }
        CSharpCompilation compilation = CSharpCompilation.Create(
            name,
            [
                CSharpSyntaxTree.ParseText(source),
                .. outputKind == OutputKind.ConsoleApplication
                    ? new[] { CSharpSyntaxTree.ParseText("""
                        [assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]
                        [assembly: System.Runtime.InteropServices.TypeMapAssemblyTarget<WindowsRuntime.InteropServices.WindowsRuntimeIidTypeMapGroup>("WinRT.Interop")]
                        """) }
                    : []
            ],
            references ?? Net100.References.All,
            options);

        string path = Path.Combine(Root, name + ".dll");
        EmitResult result = compilation.Emit(path);
        Assert.IsTrue(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return path;
    }

    private static void RewriteDictionaryScope(string path, AssemblyReference scope)
    {
        RewriteTypeScope(path, "System.Collections.ObjectModel", "ReadOnlyDictionary`2", scope);
    }

    private static void RewriteTypeScope(string path, string typeNamespace, string typeName, AssemblyReference scope)
    {
        ModuleDefinition module = ModuleDefinition.FromFile(path);
        TypeReference typeReference = module.EnumerateTableMembers<TypeReference>(TableIndex.TypeRef)
            .Single(type => type.Namespace == typeNamespace && type.Name == typeName);

        Assert.AreEqual("System.Runtime", typeReference.Scope!.GetAssembly()!.Name!.ToString());
        typeReference.Scope = scope;

        // Model valid inputs from older reference packs and implementation metadata, not patched generator output
        using MemoryStream stream = new();
        module.Write(stream);
        File.WriteAllBytes(path, stream.ToArray());
    }
}

file sealed class ForwardedFrameworkTypeImporter(ModuleDefinition module, TypeDefinition source, ITypeDefOrRef target)
    : ReferenceImporter(module)
{
    public override TypeSignature ImportTypeSignature(TypeSignature signature)
    {
        return signature is TypeDefOrRefSignature type && ReferenceEquals(type.Type, source)
            ? target.ToTypeSignature(isValueType: false)
            : base.ImportTypeSignature(signature);
    }
}

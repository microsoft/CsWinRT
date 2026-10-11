// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;

namespace ProjectionWriterTest;

public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    [DataRow("interface")]
    [DataRow("direct-is")]
    [DataRow("abstract")]
    [DataRow("virtual")]
    [DataRow("sealed")]
    [DataRow("closed-argument")]
    [DataRow("static-chain")]
    public void NativeGenericDispatch_QueriesOpaqueRcwInFreshProcesses(string scenario)
    {
        AssertNativeDispatch(scenario, nativeAot: false);
    }

    [TestMethod]
    [TestCategory("NativeAOT")]
#if !CSWINRT_GENERIC_MARSHALLERS_AOT
    [Ignore("Build with CSWINRT_TEST_GENERIC_MARSHALLERS_AOT=1 to publish the dispatch consumer with Native AOT.")]
#endif
    [DataRow("interface")]
    [DataRow("abstract")]
    [DataRow("virtual")]
    public void NativeGenericDispatch_NativeAot(string scenario)
    {
        AssertNativeDispatch(scenario, nativeAot: true);
    }

    private static void AssertNativeDispatch(string scenario, bool nativeAot)
    {
        string directory = Directory.CreateTempSubdirectory("NativeDispatchTest_").FullName;
        try
        {
            using Stream resource = typeof(Test_InteropGenericDiscovery).Assembly.GetManifestResourceStream(
                "ProjectionWriterTest.Resources.DispatchContextTests.cs")!;
            using StreamReader reader = new(resource);
            (string declarations, string invocation) = scenario switch
            {
                "interface" or "direct-is" => ("public interface IOuter { bool Check<T>(bool cast); } public sealed class Outer : IOuter { public bool Check<T>(bool cast) => Program.Build<T>(cast); }",
                    "((IOuter)new Outer()).Check<DispatchElement>(cast)"),
                "abstract" => ("public abstract class Base { public abstract bool Check<T>(bool cast); } public sealed class Outer : Base { public override bool Check<T>(bool cast) => Program.Build<T>(cast); }",
                    "((Base)new Outer()).Check<DispatchElement>(cast)"),
                "virtual" or "sealed" => ("public class Base { public virtual bool Check<T>(bool cast) => false; } public sealed class Outer : Base { public override bool Check<T>(bool cast) => Program.Build<T>(cast); }",
                    scenario == "sealed" ? "new Outer().Check<DispatchElement>(cast)" : "((Base)new Outer()).Check<DispatchElement>(cast)"),
                "closed-argument" => ("public interface ICast { bool Check<T>(bool cast); } public sealed class Caster : ICast { public bool Check<T>(bool cast) => Program.Cast<T>(cast); }",
                    "((ICast)new Caster()).Check<IList<DispatchElement>>(cast)"),
                "static-chain" => ("", "Forward<DispatchElement>(cast)"),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
            string source = reader.ReadToEnd().Replace("/*DECLARATIONS*/", declarations, StringComparison.Ordinal)
                .Replace("/*INVOCATION*/", invocation, StringComparison.Ordinal)
                .Replace("/*CALLERS*/", "private static bool Forward<T>(bool cast) => Build<T>(cast);", StringComparison.Ordinal);
            if (scenario == "direct-is")
            {
                source = source.Replace("public bool Check<T>(bool cast) => Program.Build<T>(cast);",
                    "public bool Check<T>(bool cast) => cast ? Program.Build<T>(true) : Program.Source is IList<T>;", StringComparison.Ordinal)
                    .Replace("private static object Value", "public static object Source => Value;\n    private static object Value", StringComparison.Ordinal);
            }

            File.WriteAllText(Path.Combine(directory, "NativeDispatch.cs"), source);
            string app = ProjectionWriterRunner.CompileSources([source], Path.Combine(directory, "NativeDispatch.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [Path.Combine(SdkFixture.Value, "WinRT.Sdk.Projection.dll")]);
            (int generatorExitCode, string generatorLog) = RunGenerator(directory, app, treatWarningsAsErrors: true);
            Assert.AreEqual(0, generatorExitCode, generatorLog);
            string? map = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"), "DynamicInterfaceCastableImplementationTypeMapGroup")
                .FirstOrDefault(type => type.StartsWith("System.Collections.Generic.IList`1[[Contoso.DispatchElement,", StringComparison.Ordinal));
            bool hasMap = map is not null;

            File.Copy(typeof(WindowsRuntimeObject).Assembly.Location, Path.Combine(directory, "WinRT.Runtime.dll"));
            File.Copy(Path.Combine(SdkFixture.Value, "WinRT.Sdk.Projection.dll"), Path.Combine(directory, "WinRT.Sdk.Projection.dll"));
            File.WriteAllText(Path.Combine(directory, "NativeDispatch.runtimeconfig.json"), """
                {"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}
                """);

            string executable = nativeAot ? PublishNativeDispatch(directory) : app;
            // Both operations always run, even when the first fails, and never share an object or process.
            (int isExitCode, string isLog) = nativeAot
                ? ProjectionWriterRunner.RunCommand(executable, ["is"])
                : ProjectionWriterRunner.Run(executable, "is");
            (int castExitCode, string castLog) = nativeAot
                ? ProjectionWriterRunner.RunCommand(executable, ["cast"])
                : ProjectionWriterRunner.Run(executable, "cast");
            Console.WriteLine($"{scenario}: IList map={hasMap}; {map}\nIs: {isLog}\nCast: {castLog}");
            StringAssert.Contains(isLog, "Raw native QI succeeded.");
            StringAssert.Contains(castLog, "Raw native QI succeeded.");
            Assert.IsTrue(hasMap, isLog + castLog);
            Assert.AreEqual(0, isExitCode, isLog);
            Assert.AreEqual(0, castExitCode, castLog);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string PublishNativeDispatch(string directory)
    {
        string aot = Directory.CreateDirectory(Path.Combine(directory, "aot")).FullName;
        File.WriteAllText(Path.Combine(aot, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(aot, "Directory.Build.targets"), "<Project />");
        File.WriteAllText(Path.Combine(aot, "NativeDispatch.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>14.0</LangVersion>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <PublishAot>true</PublishAot>
                <IsAotCompatible>true</IsAotCompatible>
                <DefineConstants>NATIVEAOT</DefineConstants>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="..\NativeDispatch.cs" />
                <Reference Include="WinRT.Runtime" HintPath="..\WinRT.Runtime.dll" />
                <Reference Include="WinRT.Sdk.Projection" HintPath="..\WinRT.Sdk.Projection.dll" />
                <Reference Include="WinRT.Interop" HintPath="..\WinRT.Interop.dll" />
                <TrimmerRootAssembly Include="WinRT.Interop" />
              </ItemGroup>
            </Project>
            """);
        string rid = $"win-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
        string artifacts = Environment.GetEnvironmentVariable("CSWINRT_TEST_ARTIFACTS") ?? Path.GetFullPath("TestResults");
        Directory.CreateDirectory(artifacts);
        (int exitCode, string log) = ProjectionWriterRunner.RunCommand("dotnet",
            ["publish", Path.Combine(aot, "NativeDispatch.csproj"), "-c", "Release", "-r", rid, "-o", Path.Combine(aot, "publish"),
                "-m", $"-bl:{Path.Combine(artifacts, "dispatch-aot-{}.binlog")}"],
            TimeSpan.FromMinutes(5));
        Assert.AreEqual(0, exitCode, log);
        return Path.Combine(aot, "publish", "NativeDispatch.exe");
    }
}

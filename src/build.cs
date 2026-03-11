// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#:package ConsoleAppFramework

// -------------------------------------------------------------------------
// CsWinRT Build Script
// -------------------------------------------------------------------------
// C# replacement for build.cmd. Execute with:
//
//   dotnet run build.cs -- [options]
//
// Examples:
//   dotnet run build.cs                                          # x64 Release (defaults)
//   dotnet run build.cs -- --platform x64 --configuration Debug  # x64 Debug
//   dotnet run build.cs -- --platform all                        # All platforms and configs
//   dotnet run build.cs -- --platform all --configuration all    # All platforms and configs
//   dotnet run build.cs -- --label package                       # Jump to packaging only
//
// Environment variables (for CI and advanced scenarios):
//   cswinrt_echo                                     "only" to print commands without executing
//   cswinrt_setup_local_dotnet                       "true" to install .NET SDK locally
//   cswinrt_build_params                             Additional MSBuild parameters
//   cswinrt_baseline_breaking_compat_errors          "true" to baseline API compat errors
//   cswinrt_baseline_assembly_version_compat_errors  "true" to baseline version compat errors
//   cswinrt_build_only                               "true" to skip tests (with test labels)
// -------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using ConsoleAppFramework;

ConsoleApp.Run(args, Build.Run);

// =====================================================================
// Build context
// =====================================================================

/// <summary>
/// Holds all resolved build settings for a single platform/configuration pass.
/// </summary>
sealed class BuildContext(
    string srcDir,
    string platform,
    string configuration,
    string versionNumber,
    string versionString,
    string assemblyVersion,
    string? label,
    bool echoOnly,
    string buildParams,
    bool baselineBreakingCompatErrors,
    bool baselineAssemblyVersionCompatErrors,
    bool buildOnly)
{
    public string SrcDir => srcDir;
    public string Platform => platform;
    public string Configuration => configuration;
    public string VersionNumber => versionNumber;
    public string VersionString => versionString;
    public string AssemblyVersion => assemblyVersion;
    public string? Label => label;
    public bool EchoOnly => echoOnly;
    public string BuildParams => buildParams;
    public bool BaselineBreakingCompatErrors => baselineBreakingCompatErrors;
    public bool BaselineAssemblyVersionCompatErrors => baselineAssemblyVersionCompatErrors;
    public bool BuildOnly => buildOnly;

    /// <summary>Path to the cswinrt.slnx solution file.</summary>
    public string SolutionPath => Path.Combine(SrcDir, "cswinrt.slnx");

    /// <summary>Path to the local nuget.exe tool.</summary>
    public string NugetExe => Path.Combine(SrcDir, ".nuget", "nuget.exe");

    /// <summary>Path to the nuspec directory (<c>nuget/</c> in repo root).</summary>
    public string NuspecDir => Path.GetFullPath(Path.Combine(SrcDir, "..", "nuget"));

    /// <summary>Platform-specific build output directory (<c>_build/{platform}/{configuration}</c>).</summary>
    public string BuildOutputDir => Path.Combine(SrcDir, "_build", Platform, Configuration);

    /// <summary>Whether functional tests can be run on the current platform (x86 or x64 only).</summary>
    public bool CanRunFunctionalTests =>
        Platform.Equals("x86", StringComparison.OrdinalIgnoreCase) ||
        Platform.Equals("x64", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the current platform is an ARM variant.</summary>
    public bool IsArmPlatform =>
        Platform.Equals("arm", StringComparison.OrdinalIgnoreCase) ||
        Platform.Equals("arm64", StringComparison.OrdinalIgnoreCase);
}

// =====================================================================
// Build script
// =====================================================================

/// <summary>
/// CsWinRT build orchestrator. Handles restore, build, test, and packaging stages.
/// </summary>
static class Build
{
    /// <summary>.NET SDK version required for building the projection.</summary>
    const string NetSDKVersion = "10.0.104";

    /// <summary>Functional test project names.</summary>
    static readonly string[] FunctionalTests =
    [
        "JsonValueFunctionCalls",
        "ClassActivation",
        "Structs",
        "Events",
        "DynamicInterfaceCasting",
        "Collections",
        "Async",
        "DerivedClassActivation",
        "DerivedClassAsBaseClass",
        "CCW"
    ];

    static readonly string[] AllPlatforms = ["x86", "x64", "arm", "arm64"];

    /// <summary>
    /// Ordered list of test stage names. Used for label-based stage selection
    /// with fall-through semantics (starting from the named stage and continuing
    /// through all subsequent stages).
    /// </summary>
    static readonly string[] TestStages =
    [
        "objectlifetimetests",
        "unittest",
        "sourcegeneratortest",
        "hosttest",
        "authortest",
        "functionaltest"
    ];

    /// <summary>
    /// Tracks whether the .NET SDK has already been installed in this process,
    /// to avoid redundant installations during recursive 'all' expansion.
    /// </summary>
    static bool s_dotNetInstalled;

    // -----------------------------------------------------------------
    // Entry point
    // -----------------------------------------------------------------

    /// <summary>
    /// Main entry point, invoked by ConsoleAppFramework.
    /// </summary>
    /// <param name="platform">Target platform: x86, x64, arm, arm64, or 'all' (default: x64).</param>
    /// <param name="configuration">
    /// Build configuration: Debug, Release, or 'all'. When omitted, defaults to 'all'
    /// if platform is 'all', otherwise defaults to Release.
    /// </param>
    /// <param name="versionNumber">Version number, e.g. 1.0.0.0 (default: 0.0.0.0).</param>
    /// <param name="versionString">NuGet version string, e.g. 1.0.0-preview.1 (default: 0.0.0-private.0).</param>
    /// <param name="assemblyVersion">Assembly version number (default: 0.0.0.0).</param>
    /// <param name="label">
    /// Build stage to jump to. Valid values: restore, build, test,
    /// objectlifetimetests, unittest, sourcegeneratortest, hosttest,
    /// authortest, functionaltest, package. Execution proceeds sequentially
    /// from the named stage through all subsequent stages.
    /// </param>
    public static void Run(
        string platform = "x64",
        string? configuration = null,
        string versionNumber = "0.0.0.0",
        string versionString = "0.0.0-private.0",
        string assemblyVersion = "0.0.0.0",
        string? label = null)
    {
        // Install .NET SDK locally if requested (once per process, before any builds).
        // NOTE: In the original build.cmd this was done outside 'setlocal', so the PATH
        // and DOTNET_ROOT changes persisted for the parent shell (e.g. VS). In C#, env
        // changes are always scoped to the current process and its children.
        if (!s_dotNetInstalled && GetEnvBool("cswinrt_setup_local_dotnet"))
        {
            InstallDotNet();
            s_dotNetInstalled = true;
        }

        // Handle 'all' platforms: recurse for each one
        if (platform.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // When platform is 'all' and no configuration was specified,
            // default to building all configurations as well.
            configuration ??= "all";

            foreach (string p in AllPlatforms)
            {
                Run(p, configuration, versionNumber, versionString, assemblyVersion);
            }

            return;
        }

        // Handle 'all' configurations: recurse for each one
        if (configuration is not null && configuration.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            Run(platform, "Debug", versionNumber, versionString, assemblyVersion);
            Run(platform, "Release", versionNumber, versionString, assemblyVersion);
            return;
        }

        // Apply remaining defaults
        configuration ??= "Release";

        // Build the context with all resolved settings
        BuildContext ctx = new(
            srcDir: GetSrcDir(),
            platform: platform,
            configuration: configuration,
            versionNumber: versionNumber,
            versionString: versionString,
            assemblyVersion: assemblyVersion,
            label: label,
            echoOnly: GetEnvString("cswinrt_echo").Equals("only", StringComparison.OrdinalIgnoreCase),
            buildParams: GetEnvString("cswinrt_build_params"),
            baselineBreakingCompatErrors: GetEnvBool("cswinrt_baseline_breaking_compat_errors"),
            baselineAssemblyVersionCompatErrors: GetEnvBool("cswinrt_baseline_assembly_version_compat_errors"),
            buildOnly: GetEnvBool("cswinrt_build_only"));

        ExecuteStages(ctx);
    }

    // -----------------------------------------------------------------
    // Stage orchestration
    // -----------------------------------------------------------------

    /// <summary>
    /// Determines which stages to execute based on the label, then runs them in order.
    /// </summary>
    /// <remarks>
    /// The default flow (no label) is: restore → build → package.
    /// Tests are currently skipped in the default flow, matching the original script.
    /// Test stages are only reachable by specifying an explicit label (e.g. --label test).
    /// </remarks>
    static void ExecuteStages(BuildContext ctx)
    {
        string stage = ctx.Label?.ToLowerInvariant() ?? "restore";

        switch (stage)
        {
            // Default flow: restore → build → package (tests are currently skipped)
            case "restore":
                Restore(ctx);
                BuildSolution(ctx);
                Package(ctx);
                break;

            case "build":
                BuildSolution(ctx);
                Package(ctx);
                break;

            // Test labels: execute from the named test stage through subsequent stages,
            // then package (unless the label was specifically "functionaltest").
            case "test":
            case "objectlifetimetests":
            case "unittest":
            case "sourcegeneratortest":
            case "hosttest":
            case "authortest":
            case "functionaltest":
                RunTestStages(ctx, stage);
                break;

            case "package":
                Package(ctx);
                break;

            default:
                throw new ArgumentException(
                    $"Unknown label '{ctx.Label}'. " +
                    "Valid labels: restore, build, test, objectlifetimetests, unittest, " +
                    "sourcegeneratortest, hosttest, authortest, functionaltest, package.");
        }
    }

    // -----------------------------------------------------------------
    // .NET SDK installation
    // -----------------------------------------------------------------

    /// <summary>
    /// Downloads and installs the required .NET SDK version locally for both x64 and x86.
    /// Sets DOTNET_ROOT, DOTNET_ROOT(x86), and updates PATH for child processes.
    /// </summary>
    static void InstallDotNet()
    {
        string dotnetRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "dotnet");
        string dotnetRootX86 = Path.Combine(dotnetRoot, "x86");

        // Set environment for the current process and all child processes
        Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);
        Environment.SetEnvironmentVariable("DOTNET_ROOT(x86)", dotnetRootX86);

        string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        Environment.SetEnvironmentVariable("PATH", $"{dotnetRoot};{dotnetRootX86};{currentPath}");

        const int downloadTimeout = 1200;

        // PowerShell command template to download and run dotnet-install.ps1
        const string installCommandTemplate =
            "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
            "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) " +
            "-Version '{0}' -InstallDir '{1}' -Architecture '{2}' -DownloadTimeout {3} " +
            "-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet'";

        // Install .NET SDK for x64
        Exec("powershell",
            $"-NoProfile -ExecutionPolicy unrestricted -Command " +
            string.Format(installCommandTemplate, NetSDKVersion, dotnetRoot, "x64", downloadTimeout),
            echoOnly: false);

        // Install .NET SDK for x86
        Exec("powershell",
            $"-NoProfile -ExecutionPolicy unrestricted -Command " +
            string.Format(installCommandTemplate, NetSDKVersion, dotnetRootX86, "x86", downloadTimeout),
            echoOnly: false);
    }

    // -----------------------------------------------------------------
    // Restore
    // -----------------------------------------------------------------

    /// <summary>Restores NuGet packages for the solution.</summary>
    static void Restore(BuildContext ctx)
    {
        Exec(ctx,
            "msbuild.exe",
            $"{ctx.BuildParams} " +
            $"/p:RestorePackagesConfig=true " +
            $"/t:restore " +
            $"/p:platform={ctx.Platform}" +
            $";configuration={ctx.Configuration}" +
            $";RuntimeIdentifier=win-{ctx.Platform} " +
            ctx.SolutionPath);
    }

    // -----------------------------------------------------------------
    // Build
    // -----------------------------------------------------------------

    /// <summary>Builds the entire CsWinRT solution with all required MSBuild properties.</summary>
    static void BuildSolution(BuildContext ctx)
    {
        Console.WriteLine($"Building cswinrt for {ctx.Platform} {ctx.Configuration}");

        int exitCode = Exec(ctx,
            "msbuild.exe",
            $"{ctx.BuildParams} " +
            $"/p:platform={ctx.Platform}" +
            $";configuration={ctx.Configuration}" +
            $";VersionNumber={ctx.VersionNumber}" +
            $";VersionString={ctx.VersionString}" +
            $";AssemblyVersionNumber={ctx.AssemblyVersion}" +
            $";GenerateTestProjection=true" +
            $";BaselineAllAPICompatError={BoolToLower(ctx.BaselineBreakingCompatErrors)}" +
            $";BaselineAllMatchingRefApiCompatError={BoolToLower(ctx.BaselineAssemblyVersionCompatErrors)}" +
            $";PublishBuildTool=true" +
            $";BuildToolArch={ctx.Platform} " +
            ctx.SolutionPath);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Build failed with exit code {exitCode}.");
        }
    }

    // -----------------------------------------------------------------
    // Test stages
    // -----------------------------------------------------------------

    /// <summary>
    /// Runs test stages starting from <paramref name="startFrom"/> and falling through
    /// to each subsequent stage. After all applicable tests complete, runs the package
    /// stage (unless <paramref name="startFrom"/> was specifically "functionaltest").
    /// </summary>
    /// <remarks>
    /// Mirrors the label-based fall-through behavior of the original batch script,
    /// where jumping to a label executes that code and everything below it sequentially.
    /// </remarks>
    static void RunTestStages(BuildContext ctx, string startFrom)
    {
        // If build_only is set, skip all tests
        if (ctx.BuildOnly)
        {
            return;
        }

        // Tests are not supported on ARM platforms
        if (ctx.IsArmPlatform)
        {
            Package(ctx);
            return;
        }

        // Resolve the dotnet.exe path for managed test execution
        string dotnetExe = ResolveDotNetExe(ctx.Platform);

        // Map test stage names to their implementations
        var stageActions = new (string Name, Action Execute)[]
        {
            ("objectlifetimetests", () => RunObjectLifetimeTests(ctx)),
            ("unittest",            () => RunUnitTests(ctx, dotnetExe)),
            ("sourcegeneratortest", () => RunSourceGeneratorTests(ctx, dotnetExe)),
            ("hosttest",            () => RunHostTests(ctx)),
            ("authortest",          () => RunAuthorTests(ctx)),
            ("functionaltest",      () => RunFunctionalTests(ctx)),
        };

        // Start execution from the requested stage ("test" means start from the beginning)
        bool started = startFrom == "test";

        foreach (var (name, execute) in stageActions)
        {
            if (name == startFrom)
            {
                started = true;
            }

            if (started)
            {
                execute();
            }
        }

        // If the label was specifically "functionaltest", exit without packaging
        if (startFrom == "functionaltest")
        {
            return;
        }

        Package(ctx);
    }

    /// <summary>Runs the object lifetime unit tests using vstest.console.exe.</summary>
    static void RunObjectLifetimeTests(BuildContext ctx)
    {
        Console.WriteLine($"Running object lifetime tests for {ctx.Platform} {ctx.Configuration}");

        string nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        string appxRecipe = Path.Combine(
            ctx.SrcDir, "Tests", "ObjectLifetimeTests", "bin",
            ctx.Platform, ctx.Configuration,
            "net10.0-windows10.0.19041.0", $"win-{ctx.Platform}",
            "ObjectLifetimeTests.Lifted.build.appxrecipe");

        string testAdapterPath = Path.Combine(
            nugetPackages, "mstest.testadapter", "3.10.1", "build", "_common");

        string trxPath = Path.Combine(ctx.SrcDir, "VsTestResults.trx");

        int exitCode = Exec(ctx,
            "vstest.console.exe",
            $"{appxRecipe} " +
            $"/TestAdapterPath:\"{testAdapterPath}\" " +
            $"/framework:FrameworkUap10 " +
            $"/logger:trx;LogFileName={trxPath}");

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Object lifetime tests failed with exit code {exitCode}.");
        }
    }

    /// <summary>Runs the main xUnit unit tests via <c>dotnet test</c>.</summary>
    static void RunUnitTests(BuildContext ctx, string dotnetExe)
    {
        Console.WriteLine($"Running cswinrt unit tests for {ctx.Platform} {ctx.Configuration}");

        // WinUI NuGet package's Microsoft.WinUI.AppX.targets attempts to import a file
        // that does not exist. Work around by providing a dummy targets file.
        string emptyTargets = Path.Combine(Path.GetTempPath(), "EmptyMsAppxPackage.Targets");
        File.WriteAllText(emptyTargets, "<Project/>");

        string testProject = Path.Combine(ctx.SrcDir, "Tests", "unittest", "UnitTest.csproj");
        string logFile = Path.Combine(ctx.SrcDir, $"unittest_{ctx.VersionString}.xml");

        int exitCode = Exec(ctx, dotnetExe,
            $"test --verbosity normal --no-build " +
            $"--logger xunit;LogFilePath={logFile} " +
            $"{testProject} " +
            $"/nologo /m " +
            $"/p:platform={ctx.Platform}" +
            $";configuration={ctx.Configuration}" +
            $";MsAppxPackageTargets={emptyTargets} " +
            $"-- RunConfiguration.TreatNoTestsAsError=true");

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Unit tests failed with exit code {exitCode}.");
        }
    }

    /// <summary>Runs the source generator unit tests via <c>dotnet test</c>.</summary>
    static void RunSourceGeneratorTests(BuildContext ctx, string dotnetExe)
    {
        Console.WriteLine($"Running source generator tests for {ctx.Platform} {ctx.Configuration}");

        string testProject = Path.Combine(
            ctx.SrcDir, "Tests", "SourceGeneratorTest", "SourceGeneratorTest.csproj");
        string trxFile = Path.Combine(
            ctx.SrcDir, $"sourcegeneratortest_{ctx.VersionString}.trx");

        int exitCode = Exec(ctx, dotnetExe,
            $"test --verbosity normal --no-build " +
            $"--logger trx;LogFilePath={trxFile} " +
            $"{testProject} " +
            $"/nologo /m " +
            $"/p:configuration={ctx.Configuration} " +
            $"-- RunConfiguration.TreatNoTestsAsError=true");

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Source generator tests failed with exit code {exitCode}.");
        }
    }

    /// <summary>Runs the WinRT.Host native tests (Google Test executable).</summary>
    static void RunHostTests(BuildContext ctx)
    {
        Console.WriteLine($"Running cswinrt host tests for {ctx.Platform} {ctx.Configuration}");

        string hostTestExe = Path.Combine(
            ctx.BuildOutputDir, "HostTest", "bin", "HostTest.exe");
        string xmlOutput = Path.Combine(
            ctx.SrcDir, $"hosttest_{ctx.VersionString}.xml");

        int exitCode = Exec(ctx, hostTestExe,
            $"--gtest_output=xml:{xmlOutput}");

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Host tests failed with exit code {exitCode}.");
        }
    }

    /// <summary>
    /// Runs the authoring consumption tests (Google Test executable).
    /// Failures are logged as warnings but do not abort the build, matching the
    /// original script behavior (suppressed due to known issues).
    /// </summary>
    static void RunAuthorTests(BuildContext ctx)
    {
        Console.WriteLine($"Running cswinrt authoring tests for {ctx.Platform} {ctx.Configuration}");

        string authorTestExe = Path.Combine(
            ctx.BuildOutputDir, "AuthoringConsumptionTest", "bin", "AuthoringConsumptionTest.exe");
        string xmlOutput = Path.Combine(
            ctx.SrcDir, $"hosttest_{ctx.VersionString}.xml");

        int exitCode = Exec(ctx, authorTestExe,
            $"--gtest_output=xml:{xmlOutput}");

        if (exitCode != 0)
        {
            // Not failing the build due to known issues (matches original script behavior)
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"WARNING: Authoring tests failed with exit code {exitCode} (non-fatal).");
        }
    }

    /// <summary>
    /// Runs functional tests. Only executes on x86/x64 platforms.
    /// Each test executable must return exit code 100 to indicate success.
    /// </summary>
    static void RunFunctionalTests(BuildContext ctx)
    {
        if (!ctx.CanRunFunctionalTests)
        {
            return;
        }

        Console.WriteLine($"Running cswinrt functional tests for {ctx.Platform} {ctx.Configuration}");

        foreach (string test in FunctionalTests)
        {
            Console.WriteLine($"Running {test}");

            string testExe = Path.Combine(
                ctx.SrcDir, "Tests", "FunctionalTests", test, "bin",
                ctx.Configuration, "net10.0", $"win-{ctx.Platform}", "publish", $"{test}.exe");

            int exitCode = Exec(ctx, testExe, "");

            if (exitCode != 100)
            {
                throw new InvalidOperationException(
                    $"Functional test '{test}' failed with exit code {exitCode} (expected 100).");
            }
        }
    }

    // -----------------------------------------------------------------
    // Package
    // -----------------------------------------------------------------

    /// <summary>
    /// Creates the CsWinRT and CsWinMD NuGet packages by invoking <c>nuget pack</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WinRT.Host paths (<c>winrt_host_*</c>) are only populated for the current platform.
    /// Other platform slots are left empty, matching the original script behavior where
    /// <c>set winrt_host_%cswinrt_platform%=...</c> only sets one platform variable per run.
    /// </para>
    /// <para>
    /// When building all platforms (<c>--platform all</c>), each platform build produces its
    /// own package with only its own host DLL path populated.
    /// </para>
    /// </remarks>
    static void Package(BuildContext ctx)
    {
        Console.WriteLine("Creating NuGet packages");

        string binDir = Path.Combine(ctx.BuildOutputDir, "cswinrt", "bin") + Path.DirectorySeparatorChar;

        // ----- CsWinRT package -----

        // Build artifact paths
        string cswinrtExe = Path.Combine(binDir, "cswinrt.exe");
        string interopWinmd = Path.Combine(binDir, "WinRT.Interop.winmd");
        string net10Runtime = Path.Combine(ctx.SrcDir, "WinRT.Runtime", "bin", ctx.Configuration, "net10.0", "WinRT.Runtime.dll");
        string net10RuntimeXml = Path.Combine(ctx.SrcDir, "WinRT.Runtime", "bin", ctx.Configuration, "net10.0", "WinRT.Runtime.xml");
        string sourceGenerator = Path.Combine(ctx.SrcDir, "Authoring", "WinRT.SourceGenerator2", "bin", ctx.Configuration, "net10.0", "WinRT.SourceGenerator2.dll");
        string winrtShim = Path.Combine(ctx.SrcDir, "Authoring", "WinRT.Host.Shim", "bin", ctx.Configuration, "net10.0", "WinRT.Host.Shim.dll");

        // WinRT.Host paths (only the current platform is populated)
        string winrtHost = Path.Combine(ctx.BuildOutputDir, "WinRT.Host", "bin", "WinRT.Host.dll");
        string winrtHostResource = Path.Combine(ctx.BuildOutputDir, "WinRT.Host", "bin", "WinRT.Host.dll.mui");
        string HostPathForPlatform(string p) => p.Equals(ctx.Platform, StringComparison.OrdinalIgnoreCase) ? winrtHost : "";
        string HostResourcePathForPlatform(string p) => p.Equals(ctx.Platform, StringComparison.OrdinalIgnoreCase) ? winrtHostResource : "";

        // Generator tool paths (always built for x64 and arm64)
        string interopGenX64 = Path.Combine(ctx.SrcDir, "WinRT.Interop.Generator", "bin", "x64", ctx.Configuration, "net10.0", "win-x64", "publish", "cswinrtinteropgen.exe");
        string interopGenArm64 = Path.Combine(ctx.SrcDir, "WinRT.Interop.Generator", "bin", "arm64", ctx.Configuration, "net10.0", "win-arm64", "publish", "cswinrtinteropgen.exe");
        string implGenX64 = Path.Combine(ctx.SrcDir, "WinRT.Impl.Generator", "bin", "x64", ctx.Configuration, "net10.0", "win-x64", "publish", "cswinrtimplgen.exe");
        string implGenArm64 = Path.Combine(ctx.SrcDir, "WinRT.Impl.Generator", "bin", "arm64", ctx.Configuration, "net10.0", "win-arm64", "publish", "cswinrtimplgen.exe");
        string projGenX64 = Path.Combine(ctx.SrcDir, "WinRT.Projection.Generator", "bin", "x64", ctx.Configuration, "net10.0", "win-x64", "publish", "cswinrtprojectiongen.exe");
        string projGenArm64 = Path.Combine(ctx.SrcDir, "WinRT.Projection.Generator", "bin", "arm64", ctx.Configuration, "net10.0", "win-arm64", "publish", "cswinrtprojectiongen.exe");
        string generatorTask = Path.Combine(ctx.SrcDir, "WinRT.Generator.Tasks", "bin", ctx.Configuration, "netstandard2.0", "WinRT.Generator.Tasks.dll");

        // Assemble properties for the nuspec
        string cswinrtProperties = string.Join(";",
            $"cswinrt_exe={cswinrtExe}",
            $"interop_winmd={interopWinmd}",
            $"net10_runtime={net10Runtime}",
            $"net10_runtime_xml={net10RuntimeXml}",
            $"source_generator={sourceGenerator}",
            $"cswinrt_nuget_version={ctx.VersionString}",
            $"winrt_host_x86={HostPathForPlatform("x86")}",
            $"winrt_host_x64={HostPathForPlatform("x64")}",
            $"winrt_host_arm={HostPathForPlatform("arm")}",
            $"winrt_host_arm64={HostPathForPlatform("arm64")}",
            $"winrt_host_resource_x86={HostResourcePathForPlatform("x86")}",
            $"winrt_host_resource_x64={HostResourcePathForPlatform("x64")}",
            $"winrt_host_resource_arm={HostResourcePathForPlatform("arm")}",
            $"winrt_host_resource_arm64={HostResourcePathForPlatform("arm64")}",
            $"winrt_shim={winrtShim}",
            $"cswinrtinteropgen_x64={interopGenX64}",
            $"cswinrtinteropgen_arm64={interopGenArm64}",
            $"cswinrtimplgen_x64={implGenX64}",
            $"cswinrtimplgen_arm64={implGenArm64}",
            $"cswinrtprojectiongen_x64={projGenX64}",
            $"cswinrtprojectiongen_arm64={projGenArm64}",
            $"run_cswinrt_generator_task={generatorTask}");

        Exec(ctx, ctx.NugetExe,
            $"pack {Path.Combine(ctx.NuspecDir, "Microsoft.Windows.CsWinRT.nuspec")} " +
            $"-Properties {cswinrtProperties} " +
            $"-OutputDirectory {binDir} " +
            $"-NonInteractive -Verbosity Detailed -NoPackageAnalysis");

        // ----- CsWinMD package -----

        string sourceGeneratorRoslyn4120 = Path.Combine(
            ctx.SrcDir, "Authoring", "WinRT.SourceGenerator.Roslyn4120", "bin",
            ctx.Configuration, "netstandard2.0", "WinRT.SourceGenerator.dll");
        string cswinmdOutpath = Path.Combine(
            ctx.SrcDir, "Authoring", "cswinmd", "bin", ctx.Configuration, "net10.0");

        string cswinmdProperties = string.Join(";",
            $"cswinmd_outpath={cswinmdOutpath}",
            $"source_generator_roslyn4120={sourceGeneratorRoslyn4120}",
            $"cswinmd_nuget_version={ctx.VersionString}");

        Exec(ctx, ctx.NugetExe,
            $"pack {Path.Combine(ctx.NuspecDir, "Microsoft.Windows.CsWinMD.nuspec")} " +
            $"-Properties {cswinmdProperties} " +
            $"-OutputDirectory {binDir} " +
            $"-NonInteractive -Verbosity Detailed -NoPackageAnalysis");
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Resolves the path to <c>dotnet.exe</c> for the given platform.
    /// Checks DOTNET_ROOT first, then falls back to Program Files.
    /// </summary>
    static string ResolveDotNetExe(string platform)
    {
        bool isX86 = platform.Equals("x86", StringComparison.OrdinalIgnoreCase);

        // Try DOTNET_ROOT / DOTNET_ROOT(x86) first
        string? dotnetRoot = isX86
            ? Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)")
            : Environment.GetEnvironmentVariable("DOTNET_ROOT");

        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            string candidate = Path.Combine(dotnetRoot, "dotnet.exe");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Fall back to Program Files
        string programFiles = isX86
            ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFiles, "dotnet", "dotnet.exe");
    }

    /// <summary>
    /// Executes an external process and returns its exit code.
    /// In echo-only mode (cswinrt_echo=only), prints the command without executing.
    /// </summary>
    static int Exec(BuildContext ctx, string fileName, string arguments) =>
        Exec(fileName, arguments, ctx.EchoOnly);

    /// <summary>
    /// Executes an external process and returns its exit code.
    /// In echo-only mode, prints the command without executing.
    /// </summary>
    static int Exec(string fileName, string arguments, bool echoOnly)
    {
        if (echoOnly)
        {
            Console.WriteLine("Command Line:");
            Console.WriteLine($"{fileName} {arguments}");
            Console.WriteLine();
            return 0;
        }

        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>Gets an environment variable value, returning empty string if not set.</summary>
    static string GetEnvString(string name) =>
        Environment.GetEnvironmentVariable(name) ?? "";

    /// <summary>
    /// Gets an environment variable as a boolean.
    /// Returns <see langword="true"/> only when the value is exactly "true" (case-insensitive).
    /// </summary>
    static bool GetEnvBool(string name) =>
        GetEnvString(name).Equals("true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Converts a boolean to a lowercase string ("true"/"false") for MSBuild properties.</summary>
    static string BoolToLower(bool value) =>
        value ? "true" : "false";

    /// <summary>
    /// Returns the directory containing this source file (the <c>src</c> directory).
    /// Uses <see cref="CallerFilePathAttribute"/> to resolve the path at compile time.
    /// </summary>
    static string GetSrcDir([CallerFilePath] string? path = null) =>
        Path.GetDirectoryName(path)!;
}

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]

namespace Benchmarks
{
    public class Program
    {
        // Default CsWinRT versions — keep in sync with Directory.Build.props.
        private const string DefaultCsWinRT3Version = "3.0.0-prerelease-ci.260302.3";
        private const string DefaultCsWinRT2Version = "2.3.0-prerelease.251115.2";
        private const string DefaultWindowsSdkVersion = "10.0.26100.7706-preview.ge-release-svc-prod3";

        static void Main(string[] args)
        {
            var mode = GetArgValue(args, "--mode")
                ?? Environment.GetEnvironmentVariable("BENCHMARK_MODE")
                ?? "compare-versions";

            // Strip --mode from args before passing to BenchmarkDotNet
            var bdnArgs = StripArg(args, "--mode");

            IConfig config = mode switch
            {
                "compare-versions" => CreateCompareVersionsConfig(),
                "regression" => CreateRegressionConfig(),
                _ => CreateCompareVersionsConfig()
            };

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(bdnArgs, config);
        }

        /// <summary>
        /// compare-versions mode: CsWinRT 2.x (baseline) vs CsWinRT 3.x.
        /// Shows the performance improvements of v3 over v2.
        /// </summary>
        private static IConfig CreateCompareVersionsConfig()
        {
            var cswinrt3 = Env("BENCHMARK_CSWINRT3_VERSION", DefaultCsWinRT3Version);
            var cswinrt2 = Env("BENCHMARK_CSWINRT2_VERSION", DefaultCsWinRT2Version);
            var sdk = Env("BENCHMARK_WINDOWS_SDK_VERSION", DefaultWindowsSdkVersion);

            return DefaultConfig.Instance
                .AddJob(Job.Default
                    .WithNuGet("Microsoft.Windows.CsWinRT", cswinrt3)
                    .WithArguments(new Argument[]
                    {
                        new MsBuildArgument($"/p:BenchmarkCsWinRT3Version={cswinrt3}"),
                        new MsBuildArgument($"/p:WindowsSdkPackageVersion={sdk}"),
                        new MsBuildArgument("/p:TargetFramework=net10.0-windows10.0.26100.1")
                    })
                    .WithId("CsWinRT_3x"))
                .AddJob(Job.Default
                    .WithNuGet("Microsoft.Windows.CsWinRT", cswinrt2)
                    .WithArguments(new Argument[]
                    {
                        new MsBuildArgument($"/p:BenchmarkCsWinRT2Version={cswinrt2}"),
                        new MsBuildArgument($"/p:WindowsSdkPackageVersion={sdk}"),
                        new MsBuildArgument("/p:TargetFramework=net10.0-windows10.0.26100.0")
                    })
                    .WithBaseline(true)
                    .WithId("CsWinRT_2x"))
                .AddExporter(JsonExporter.Full);
        }

        /// <summary>
        /// regression mode: Known-good CsWinRT 3.x (baseline) vs currently-built CsWinRT 3.x.
        /// Detects performance regressions in new builds.
        /// </summary>
        private static IConfig CreateRegressionConfig()
        {
            var currentVersion = Env("BENCHMARK_CSWINRT_CURRENT_VERSION", DefaultCsWinRT3Version);
            var baselineVersion = Env("BENCHMARK_CSWINRT_BASELINE_VERSION", DefaultCsWinRT3Version);
            var sdk = Env("BENCHMARK_WINDOWS_SDK_VERSION", DefaultWindowsSdkVersion);

            return DefaultConfig.Instance
                .AddJob(Job.Default
                    .WithNuGet("Microsoft.Windows.CsWinRT", currentVersion)
                    .WithArguments(new Argument[]
                    {
                        new MsBuildArgument($"/p:BenchmarkCsWinRT3Version={currentVersion}"),
                        new MsBuildArgument($"/p:WindowsSdkPackageVersion={sdk}"),
                        new MsBuildArgument("/p:TargetFramework=net10.0-windows10.0.26100.1")
                    })
                    .WithId("CsWinRT_Current"))
                .AddJob(Job.Default
                    .WithNuGet("Microsoft.Windows.CsWinRT", baselineVersion)
                    .WithArguments(new Argument[]
                    {
                        new MsBuildArgument($"/p:BenchmarkCsWinRT2Version={baselineVersion}"),
                        new MsBuildArgument($"/p:WindowsSdkPackageVersion={sdk}"),
                        new MsBuildArgument("/p:TargetFramework=net10.0-windows10.0.26100.0")
                    })
                    .WithBaseline(true)
                    .WithId("CsWinRT_Baseline"))
                .AddExporter(JsonExporter.Full);
        }

        private static string Env(string name, string defaultValue)
            => Environment.GetEnvironmentVariable(name) ?? defaultValue;

        private static string GetArgValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }

        private static string[] StripArg(string[] args, string name)
        {
            var result = new System.Collections.Generic.List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    i++; // skip name and value
                    continue;
                }
                result.Add(args[i]);
            }
            return result.ToArray();
        }
    }
}
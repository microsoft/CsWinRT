﻿using BenchmarkDotNet.Running;
using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Characteristics;
using System.IO;

namespace Benchmarks
{
    public class Program
    {
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new CustomConfig().Config);

        private class CustomConfig : Attribute, IConfigSource
        {
            public IConfig Config { get; } = DefaultConfig.Instance;

            public CustomConfig()
            {
                // Test CsWinRT projection
                var job = Job.Default
                    .WithPlatform(BenchmarkDotNet.Environments.Platform.X64)
                    .WithArguments(
                        new Argument[] {
                            new MsBuildArgument("/p:platform=x64")
                        }
                    ).AsDefault();

                // Test WinMD support
#if NETCOREAPP3_1
                // BenchmarkDotNet will rebuild the project with a project reference to this project when this project's output exe is ran.  It
                // will be ran from the same folder as where we have the application manifest binplaced which we want to embed in the new exe.
                string manifestFile = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "Benchmarks.manifest");

                var winmdJob = Job.Default
                    .WithPlatform(BenchmarkDotNet.Environments.Platform.X64)
                    .WithToolchain(new NetCore3ToolChainWithNativeExecution())
                    .WithArguments(
                        new Argument[] {
                            new MsBuildArgument("/p:platform=x64"),
                            new MsBuildArgument("/p:ApplicationManifest=" + manifestFile),
                            new MsBuildArgument("/p:BenchmarkWinmdSupport=true")
                        }
                    )
                    .WithId("WinMD NetCoreApp31");

                // Optimizer needs to be diabled as it errors on WinMDs
                Config = Config.WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                        .AddJob(winmdJob);
#else
                Config = Config.AddJob(job);
#endif
            }
        }

        // Custom tool chain for building the benchmark with WinMDs as we need to execute the
        // exe version of the benchmark rather than the dll version which runs under dotnet cli.
        // This is because we need to be able to embed a side by side manifest for reg free winrt
        // and we need COM to be able to find the WinMDs.
        private class NetCore3ToolChainWithNativeExecution : Toolchain
        {
            public NetCore3ToolChainWithNativeExecution()
                : base("netcoreapp3.1-native",
                      new CsProjGeneratorWithNativeExe(NetCoreAppSettings.NetCoreApp31),
                      CsProjCoreToolchain.NetCoreApp31.Builder,
                      new Executor())
            {
            }

            public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
            {
                return CsProjCoreToolchain.NetCoreApp31.IsSupported(benchmarkCase, logger, resolver);
            }
        }

        private class CsProjGeneratorWithNativeExe : CsProjGenerator
        {
            public CsProjGeneratorWithNativeExe(NetCoreAppSettings settings)
                :base(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion)
            {
            }

            protected override string GetExecutableExtension()
            {
                return ".exe";
            }
        }
    }
}

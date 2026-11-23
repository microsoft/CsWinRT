using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace WeakReferenceBenchmarks;

public static class Program
{
    /// <summary>
    /// Runs the benchmarks on both CoreCLR and Native AOT (pass <c>--coreclr-only</c> or <c>--aot-only</c> to run just one).
    /// </summary>
    public static void Main(string[] args)
    {
        bool coreClrOnly = args.Contains("--coreclr-only");
        bool aotOnly = args.Contains("--aot-only");

        args = args.Where(static a => a is not ("--coreclr-only" or "--aot-only")).ToArray();

        ManualConfig config = ManualConfig.Create(DefaultConfig.Instance).WithBuildTimeout(TimeSpan.FromMinutes(15));

        if (!aotOnly)
        {
            config = config.AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0));
        }

        if (!coreClrOnly)
        {
            config = config.AddJob(Job.Default.WithRuntime(NativeAotRuntime.Net10_0));
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}

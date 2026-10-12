// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    public void SupplementalFanout_PreservesEveryOrdinaryMemberInstantiation()
    {
        string ordinaryMarkers = string.Join('\n', Enumerable.Range(0, 64).Select(index => $"public struct Marker{index};"));
        string ordinaryRoots = string.Join('\n', Enumerable.Range(0, 64).Select(index => $"_ = new Root<Marker{index}>();"));
        string targets = string.Join('\n', Enumerable.Range(0, 1030).Select(index =>
            $"public sealed class Target{index} : IDispatch {{ public void Check<T>() {{ }} }}"));
        string directory = Directory.CreateTempSubdirectory("InteropAdditiveBudget_").FullName;
        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                using Windows.Foundation;
                namespace Additive;
                {{ordinaryMarkers}}
                public struct DispatchMarker;
                public sealed class Root<T> : IStringable
                {
                    public Middle<T> Create() => new();
                    public override string ToString() => "root";
                }
                public sealed class Middle<T> { public Leaf<T> Next() => new(); }
                public sealed class Leaf<T> : IStringable { public override string ToString() => "leaf"; }
                public interface IDispatch { void Check<T>(); }
                {{targets}}
                public static class Program
                {
                    public static void Main()
                    {
                        {{ordinaryRoots}}
                        ((IDispatch)new Target0()).Check<DispatchMarker>();
                    }
                }
                """], Path.Combine(directory, "Additive.dll"), outputKind: OutputKind.ConsoleApplication);
            byte[]? initial = null;
            foreach (int parallelism in new[] { 1, 4 })
            {
                (int exitCode, string log) = RunGenerator(directory, app, maxDegreesOfParallelism: parallelism);
                Assert.AreEqual(0, exitCode, log);
                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                var types = GetComWrappersTypeAssociations(interop);
                for (int index = 0; index < 64; index++)
                {
                    Assert.IsTrue(types.Any(type => type.StartsWith($"Additive.Leaf`1[[Additive.Marker{index},", StringComparison.Ordinal)),
                        $"Ordinary member 'Leaf<Marker{index}>' was displaced by dispatch fan-out.\n{log}");
                }
                StringAssert.Contains(log, "Supplemental generic discovery");
                Assert.IsFalse(log.Contains("Generic member discovery reached", StringComparison.Ordinal), log);

                byte[] output = File.ReadAllBytes(interop);
                if (initial is not null)
                {
                    CollectionAssert.AreEqual(initial, output);
                }

                initial = output;
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void ExhaustedPrimaryBudget_DoesNotDisableSupplementalDispatch()
    {
        string directory = Directory.CreateTempSubdirectory("InteropPrimaryExhaustion_").FullName;
        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                using Windows.Foundation;
                namespace Additive;
                public sealed class A<T>;
                public sealed class B<T>;
                public sealed class Expanding<T>
                {
                    public Expanding<A<T>> First() => new();
                    public Expanding<B<T>> Second() => new();
                }
                public struct Marker;
                public sealed class Required<T> : IStringable { public override string ToString() => "required"; }
                public interface IDispatch { void Check<T>(); }
                public sealed class Target : IDispatch
                {
                    public void Check<T>() => _ = new Required<T>();
                }
                public static class Program
                {
                    public static void Main()
                    {
                        _ = new Expanding<int>();
                        ((IDispatch)new Target()).Check<Marker>();
                    }
                }
                """], Path.Combine(directory, "Additive.dll"), outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app);
            Assert.AreEqual(0, exitCode, log);
            StringAssert.Contains(log, "Generic member discovery reached");
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll")).Any(type =>
                type.StartsWith("Additive.Required`1[[Additive.Marker,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

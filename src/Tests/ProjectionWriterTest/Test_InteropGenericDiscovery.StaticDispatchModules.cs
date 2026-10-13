// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    [DataRow("minimal", false, false, true)]
    [DataRow("strict", false, false, false)]
    [DataRow("strict", true, false, true)]
    [DataRow("minimal", false, true, false)]
    [DataRow("strict", true, true, false)]
    [DataRow("all", false, false, true)]
    [DataRow("all", false, true, false)]
    public void StaticDispatch_BodyModulePolicy(string mode, bool optIn, bool optOut, bool expected)
    {
        string directory = Directory.CreateTempSubdirectory("InteropStaticPolicy_").FullName;

        try
        {
            string foreign = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Foreign;
                public interface IFactory { static abstract object Create(); }
                public interface IFactory<T> : IFactory
                {
                    static object IFactory.Create() => new T[1];
                }
                public sealed class Factory<T> : IFactory<T>;
                """], Path.Combine(directory, "Foreign.dll"));
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                using Foreign;
                using Windows.Foundation;
                public static class Program
                {
                    public static void Main() => _ = Invoke<Factory<AsyncStatus>>();
                    private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [foreign]);
            string[] excludedAssemblies =
            [
                // Bound the test's runtime closure while preserving 'all' mode's inclusion decision for 'Foreign'
                .. mode == "all"
                    ? ProjectionWriterRunner.GetRuntimeReferencePaths()
                        .Select(static path => Path.GetFileNameWithoutExtension(path)!)
                        .Where(static name => name != "WinRT.Runtime")
                    : [],
                .. optOut ? new[] { "Foreign" } : []
            ];
            (int exitCode, string log) = RunGenerator(directory, app, additionalReferences: [foreign],
                additionalArguments: [
                    $"--marshalling-mode {mode}",
                    .. optIn ? new[] { "--marshalling-enabled-assembly-names Foreign" } : [],
                    .. excludedAssemblies.Length > 0 ? new[] { $"--marshalling-disabled-assembly-names {string.Join(",", excludedAssemblies)}" } : []],
                treatWarningsAsErrors: false,
                timeout: mode == "all" ? TimeSpan.FromMinutes(2) : null);

            Assert.AreEqual(0, exitCode, log);
            Assert.AreEqual(expected, GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(static type => type.StartsWith("Windows.Foundation.AsyncStatus[],", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false, false, true)]
    [DataRow(true, false, false)]
    [DataRow(true, true, true)]
    public void StaticDispatch_CallerPreservesRootOneHop(bool forward, bool optIn, bool expected)
    {
        string directory = Directory.CreateTempSubdirectory("InteropStaticCallerPolicy_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["public interface IFactory { static abstract object Create(); }"],
                Path.Combine(directory, "Contracts.dll"));
            string implementation = ProjectionWriterRunner.CompileSources(
                ["""
                using System.Collections.Generic;
                using Windows.Foundation;
                public interface IFactory<T> : IFactory
                {
                    static object IFactory.Create() => new List<IAsyncOperation<T>>();
                }
                public sealed class Factory<T> : IFactory<T>;
                """], Path.Combine(directory, "Implementation.dll"), additionalReferences: [contracts]);
            string caller = ProjectionWriterRunner.CompileSources(
                [$$"""
                public static class Caller
                {
                    public static object Invoke<TFactory>() where TFactory : IFactory =>
                        {{(forward ? "Forward<TFactory>()" : "TFactory.Create()")}};
                    private static object Forward<TFactory>() where TFactory : IFactory => TFactory.Create();
                }
                """], Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public sealed class Root : Windows.Foundation.IStringable
                {
                    public override string ToString() => "root";
                }
                public static class Program
                {
                    public static void Main()
                    {
                        _ = new Root();
                        _ = Caller.Invoke<Factory<long>>();
                    }
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, implementation, caller]);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                additionalReferences: [contracts, implementation, caller],
                additionalArguments: [
                    "--marshalling-mode strict",
                    .. optIn ? new[] { "--marshalling-enabled-assembly-names Caller" } : []]);

            Assert.AreEqual(0, exitCode, log);
            Assert.AreEqual(expected, GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(static type => type.StartsWith(StaticOperationList, StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_PublicationsJoinEarlierContexts(bool publishesReceiver)
    {
        string directory = Directory.CreateTempSubdirectory("InteropStaticPublications_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                [$$"""
                using System.Collections.Generic;
                using Windows.Foundation;
                public interface IDispatch { void Check<T>(); }
                public sealed class Target<U> : IDispatch
                {
                    public void Check<T>() => _ = new List<IAsyncOperationWithProgress<U, T>>();
                }
                public interface IFactory { static abstract object Create(); }
                public interface IFactory<T> : IFactory
                {
                    static object IFactory.Create()
                    {
                        {{(publishesReceiver ? "return new Target<T>();" : "((IDispatch)null!).Check<T>(); return null!;")}}
                    }
                }
                public sealed class Factory<T> : IFactory<T>;
                public static class FactoryCalls
                {
                    public static object Create<TFactory>() where TFactory : IFactory => TFactory.Create();
                }
                """], Path.Combine(directory, "Contracts.dll"));
            string producer = ProjectionWriterRunner.CompileSources(
                [$$"""
                public static class Producer
                {
                    public static object Create() => {{(publishesReceiver ? "FactoryCalls.Create<Factory<long>>()" : "new Target<long>()")}};
                }
                """], Path.Combine(directory, "Producer.dll"), additionalReferences: [contracts]);
            string caller = ProjectionWriterRunner.CompileSources(
                [$$"""
                public static class Caller
                {
                    public static void Invoke()
                    {
                        {{(publishesReceiver ? "((IDispatch)null!).Check<int>();" : "_ = FactoryCalls.Create<Factory<int>>();")}}
                    }
                }
                """], Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main()
                    {
                        _ = Producer.Create();
                        Caller.Invoke();
                    }
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, producer, caller]);
            byte[]? previous = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                    additionalReferences: reverse ? [caller, producer, contracts] : [contracts, producer, caller],
                    maxDegreesOfParallelism: reverse ? 4 : 1);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");

                Assert.IsTrue(GetComWrappersTypeAssociations(interop).Any(static type =>
                    type.StartsWith("System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.Int64,", StringComparison.Ordinal) &&
                    type.Contains("],[System.Int32,", StringComparison.Ordinal)), log);

                byte[] output = File.ReadAllBytes(interop);

                if (previous is not null)
                {
                    CollectionAssert.AreEqual(previous, output);
                }

                previous = output;
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void StaticDispatch_CallFirstClosedByLateJoin()
    {
        string directory = Directory.CreateTempSubdirectory("InteropLateStaticCall_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                using System.Collections.Generic;
                using Windows.Foundation;
                public interface IDispatch { object Create<T>(); }
                public interface IFactory { static abstract object Create(); }
                public interface IFactory<X, Y> : IFactory
                {
                    static object IFactory.Create() => new List<IAsyncOperationWithProgress<X, Y>>();
                }
                public sealed class Factory<X, Y> : IFactory<X, Y>;
                public static class FactoryCalls
                {
                    public static object Create<TFactory>() where TFactory : IFactory => TFactory.Create();
                }
                public sealed class Target<U> : IDispatch
                {
                    public object Create<T>() => FactoryCalls.Create<Factory<U, T>>();
                }
                """], Path.Combine(directory, "Contracts.dll"));
            string producer = ProjectionWriterRunner.CompileSources(
                ["public static class Producer { public static IDispatch Create() => new Target<long>(); }"],
                Path.Combine(directory, "Producer.dll"), additionalReferences: [contracts]);
            string caller = ProjectionWriterRunner.CompileSources(
                ["public static class Caller { public static object Invoke<T>(IDispatch value) => value.Create<T>(); }"],
                Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["public static class Program { public static void Main() => _ = Caller.Invoke<int>(Producer.Create()); }"],
                Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, producer, caller]);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                additionalReferences: [contracts, producer, caller]);

            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll")).Any(static type =>
                type.StartsWith("System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.Int64,", StringComparison.Ordinal) &&
                type.Contains("],[System.Int32,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

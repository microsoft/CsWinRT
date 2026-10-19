// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, false, true)]
    [DataRow(true, false, false)]
    [DataRow(true, false, true)]
    [DataRow(false, true, false)]
    [DataRow(false, true, true)]
    [DataRow(true, true, false)]
    [DataRow(true, true, true)]
    public void LateDispatch_NewDemandJoinsLaterImportedReceiver(
        bool staticReceiver,
        bool defaultDemand,
        bool arrayResult)
    {
        string directory = Directory.CreateTempSubdirectory("InteropLateDemand_").FullName;

        try
        {
            LateDispatchFixture fixture = CompileLateDispatchFixture(
                directory: directory,
                staticReceiver: staticReceiver,
                defaultDemand: defaultDemand,
                arrayResult: arrayResult);
            byte[]? previous = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(
                    directory,
                    fixture.App,
                    treatWarningsAsErrors: true,
                    additionalReferences: reverse ? [.. fixture.References.Reverse()] : fixture.References,
                    maxDegreesOfParallelism: reverse ? 4 : 1);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");

                AssertLateDispatchAssociation(interop, log, arrayResult: arrayResult);

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
    [DataRow(false)]
    [DataRow(true)]
    public void LateDispatch_RepairedBodyContinuesThroughDefaultStaticAndGenericCalls(bool staticReceiver)
    {
        string directory = Directory.CreateTempSubdirectory("InteropLateDemandChain_").FullName;

        try
        {
            LateDispatchFixture fixture = CompileLateDispatchFixture(
                directory: directory,
                staticReceiver: staticReceiver,
                defaultDemand: false,
                arrayResult: false,
                repairChain: true);
            (int exitCode, string log) = RunGenerator(
                directory,
                fixture.App,
                treatWarningsAsErrors: true,
                additionalReferences: fixture.References);

            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(static type => IsLateDispatchAssociation(type) &&
                    type.Contains("],[LatePairing.Pair`2[[LatePairing.C,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LateDispatch_ShallowerDemandRecoversDepthLimitedDescendants(bool shallower)
    {
        string directory = Directory.CreateTempSubdirectory("InteropLateShallowerDemand_").FullName;

        try
        {
            // A long generic path observes the demand before a shorter default body sees the same arguments
            LateDispatchFixture fixture = CompileLateDispatchFixture(
                directory: directory,
                staticReceiver: true,
                defaultDemand: shallower,
                arrayResult: false,
                shallowerDemand: true);
            (int exitCode, string log) = RunGenerator(
                directory,
                fixture.App,
                treatWarningsAsErrors: shallower,
                additionalReferences: fixture.References);

            Assert.AreEqual(0, exitCode, log);
            AssertLateDispatchAssociation(Path.Combine(directory, "WinRT.Interop.dll"), log);
            Assert.AreEqual(shallower, GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(static type => type.StartsWith("LatePairing.Recovered`1[[LatePairing.Pair`2[[LatePairing.C,", StringComparison.Ordinal) &&
                    type.Contains("],[LatePairing.Pair`2[[LatePairing.A,", StringComparison.Ordinal) &&
                    type.Contains("],[LatePairing.B,", StringComparison.Ordinal)), log);

            if (!shallower)
            {
                StringAssert.Contains(log, "CSWINRTINTEROPGEN0104");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LateDispatch_RootReceiverAlreadyJoinsNewDemand(bool defaultDemand)
    {
        string directory = Directory.CreateTempSubdirectory("InteropLateRootReceiver_").FullName;

        try
        {
            LateDispatchFixture fixture = CompileLateDispatchFixture(
                directory: directory,
                staticReceiver: false,
                defaultDemand: defaultDemand,
                arrayResult: false,
                rootReceiver: true);
            (int exitCode, string log) = RunGenerator(
                directory,
                fixture.App,
                treatWarningsAsErrors: true,
                additionalReferences: fixture.References);

            Assert.AreEqual(0, exitCode, log);
            AssertLateDispatchAssociation(Path.Combine(directory, "WinRT.Interop.dll"), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(1020, true)]
    [DataRow(1021, true)]
    [DataRow(1023, false)]
    public void LateDispatch_NewPairsUseOnlyRemainingAllowance(int existingTypes, bool expected)
    {
        string directory = Directory.CreateTempSubdirectory("InteropLateDemandBudget_").FullName;

        try
        {
            LateDispatchFixture fixture = CompileLateDispatchFixture(
                directory: directory,
                staticReceiver: false,
                defaultDemand: false,
                arrayResult: false,
                existingTypes: existingTypes);
            (int exitCode, string log) = RunGenerator(
                directory,
                fixture.App,
                additionalReferences: fixture.References,
                timeout: TimeSpan.FromMinutes(2));

            Assert.AreEqual(0, exitCode, log);

            HashSet<string> associations = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            string[] existing = [.. associations.Where(static type =>
                type.StartsWith("LatePairing.Existing`1", StringComparison.Ordinal))];

            Assert.AreEqual(existingTypes, existing.Length, log);

            for (int index = 0; index < existingTypes; index++)
            {
                Assert.IsTrue(existing.Any(type =>
                    type.StartsWith($"LatePairing.Existing`1[[LatePairing.Pair`2[[LatePairing.Marker{index},", StringComparison.Ordinal)), log);
            }

            Assert.AreEqual(expected, associations.Any(IsLateDispatchAssociation), log);

            if (existingTypes == 1020)
            {
                Assert.IsFalse(log.Contains("CSWINRTINTEROPGEN0106", StringComparison.Ordinal), log);
            }
            else if (!expected)
            {
                StringAssert.Contains(log, "CSWINRTINTEROPGEN0106");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static LateDispatchFixture CompileLateDispatchFixture(
        string directory,
        bool staticReceiver,
        bool defaultDemand,
        bool arrayResult,
        bool rootReceiver = false,
        int existingTypes = 0,
        bool repairChain = false,
        bool shallowerDemand = false)
    {
        string markers = string.Join('\n', Enumerable.Range(0, existingTypes).Select(static index =>
            $"public struct Marker{index};"));
        string constructions = string.Join('\n', Enumerable.Range(0, existingTypes).Select(static index =>
            $"_ = new Existing<Pair<Marker{index}, Pair<U, T>>>();"));
        string demandChain = shallowerDemand
            ? string.Join('\n', Enumerable.Range(0, 20).Select(static index =>
                $"private static object DeepDemand{index}<T>(IFinish target) => " +
                (index == 19 ? "target.Finish<T>();" : $"DeepDemand{index + 1}<T>(target);")))
            : "";
        string resultChain = shallowerDemand
            ? string.Join('\n', Enumerable.Range(0, 12).Select(static index =>
                $"public static class Recovery{index}<T> {{ public static object Make() => " +
                (index == 11 ? "new Recovered<T>();" : $"Recovery{index + 1}<T>.Make();") + " }"))
            : "";
        string contracts = ProjectionWriterRunner.CompileSources(
            [$$"""
            using Windows.Foundation;
            namespace LatePairing;
            public struct A;
            public struct B;
            public struct C;
            public readonly struct Pair<X, Y>;
            {{markers}}
            public sealed class Final<T> : IStringable
            {
                {{(shallowerDemand ? "public Final() { _ = Recovery0<T>.Make(); }" : "")}}
                public override string ToString() => "final";
            }
            {{resultChain}}
            {{(shallowerDemand
                ? """
                  public sealed class Recovered<T> : IStringable
                  {
                      public override string ToString() => "recovered";
                  }
                  """
                : "")}}
            public sealed class Existing<T> : IStringable
            {
                public override string ToString() => "existing";
            }
            public interface IFinish { object Finish<T>(); }
            public sealed class Receiver<U> : IFinish
            {
                public object Finish<T>() => {{(repairChain
                    ? "new ContinuationFactory<Pair<U, T>>()"
                    : arrayResult ? "new Final<Pair<U, T>>[1]" : "new Final<Pair<U, T>>()")}};
            }
            public static class Shelf { public static IFinish Target = null!; }
            public interface ISeed { object Go<T>(IFinish target); }
            public sealed class Seed<U> : ISeed
            {
                public object Go<T>(IFinish target)
                {
                    {{constructions}}
                    {{(shallowerDemand && defaultDemand ? "_ = DeepDemand0<Pair<U, T>>(target);" : "")}}
                    {{(defaultDemand
                        ? "return new DemandFactory<Pair<U, T>>();"
                        : shallowerDemand ? "return DeepDemand0<Pair<U, T>>(target);" : "return target.Finish<Pair<U, T>>();")}}
                }
                {{demandChain}}
            }
            public interface IDemand { object Make(); }
            public interface IDemand<T> : IDemand
            {
                object IDemand.Make() => Shelf.Target.Finish<T>();
            }
            public sealed class DemandFactory<T> : IDemand<T>;
            {{(repairChain
                ? """
                  public interface ITail { object Finish<T>(); }
                  public sealed class TailReceiver<U> : ITail
                  {
                      public object Finish<T>() => new Final<Pair<U, T>>();
                  }
                  public static class TailShelf { public static ITail Target = null!; }
                  public interface IContinuation { object Continue(); }
                  public interface IContinuation<T> : IContinuation
                  {
                      object IContinuation.Continue() => ContinuationRunner.Invoke<StaticContinuation<T>>();
                  }
                  public sealed class ContinuationFactory<T> : IContinuation<T>;
                  public interface IStaticContinuation { static abstract object Make(); }
                  public interface IStaticContinuation<T> : IStaticContinuation
                  {
                      static object IStaticContinuation.Make() => TailShelf.Target.Finish<T>();
                  }
                  public sealed class StaticContinuation<T> : IStaticContinuation<T>;
                  public static class ContinuationRunner
                  {
                      public static object Invoke<TFactory>() where TFactory : IStaticContinuation => TFactory.Make();
                  }
                  """
                : "")}}
            {{(staticReceiver
                ? $$"""
                  public interface IFactory { static abstract object Make(); }
                  public interface IFactory<T> : IFactory
                  {
                      static object IFactory.Make()
                      {
                          {{(repairChain ? "TailShelf.Target = new TailReceiver<T>();" : "")}}
                          return new Receiver<T>();
                      }
                  }
                  """
                : $$"""
                  public interface IFactory { object Make(); }
                  public interface IFactory<T> : IFactory
                  {
                      object IFactory.Make()
                      {
                          {{(repairChain ? "TailShelf.Target = new TailReceiver<T>();" : "")}}
                          return new Receiver<T>();
                      }
                  }
                  """)}}
            public sealed class Factory<T> : IFactory<T>;
            """], Path.Combine(directory, "Contracts.dll"));
        string producer = ProjectionWriterRunner.CompileSources(
            ["""
            namespace LatePairing;
            public static class Producer
            {
                public static ISeed Make() => new Seed<A>();
            }
            """], Path.Combine(directory, "A_Producer.dll"), additionalReferences: [contracts]);
        string caller = ProjectionWriterRunner.CompileSources(
            ["""
            namespace LatePairing;
            public static class Caller
            {
                public static object Invoke(ISeed seed, IFinish target) => seed.Go<B>(target);
            }
            """], Path.Combine(directory, "B_Caller.dll"), additionalReferences: [contracts]);
        string receiver = ProjectionWriterRunner.CompileSources(
            [$$"""
            namespace LatePairing;
            public static class ReceiverPublisher
            {
                public static object Make() => {{(rootReceiver
                    ? "new Receiver<C>()"
                    : staticReceiver ? "Invoke<Factory<C>>()" : "((IFactory)new Factory<C>()).Make()")}};
                {{(staticReceiver ? "private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Make();" : "")}}
            }
            """], Path.Combine(directory, "C_Receiver.dll"), additionalReferences: [contracts]);
        string app = ProjectionWriterRunner.CompileSources(
            [$$"""
            using LatePairing;
            public static class Program
            {
                public static void Main()
                {
                    Shelf.Target = (IFinish)ReceiverPublisher.Make();
                    object value = Caller.Invoke(Producer.Make(), Shelf.Target);
                    {{(defaultDemand ? "_ = ((IDemand)value).Make();" : "_ = value;")}}
                    {{(repairChain ? "_ = ((IContinuation)value).Continue();" : "")}}
                }
            }
            """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
            additionalReferences: [contracts, producer, caller, receiver]);

        return new LateDispatchFixture(
            App: app,
            References: [contracts, producer, caller, receiver]);
    }

    private static bool IsLateDispatchAssociation(string type)
    {
        return type.StartsWith("LatePairing.Final`1[[LatePairing.Pair`2[[LatePairing.C,", StringComparison.Ordinal) &&
            type.Contains("],[LatePairing.Pair`2[[LatePairing.A,", StringComparison.Ordinal) &&
            type.Contains("],[LatePairing.B,", StringComparison.Ordinal);
    }

    private static void AssertLateDispatchAssociation(string interop, string log, bool arrayResult = false)
    {
        HashSet<string> associations = GetComWrappersTypeAssociations(interop);

        Assert.IsTrue(associations.Any(IsLateDispatchAssociation), log);

        if (arrayResult)
        {
            Assert.IsTrue(associations.Any(static type =>
                IsLateDispatchAssociation(type) && type.Contains("]][], Contracts,", StringComparison.Ordinal)), log);
        }
    }

    private sealed record LateDispatchFixture(string App, string[] References);
}

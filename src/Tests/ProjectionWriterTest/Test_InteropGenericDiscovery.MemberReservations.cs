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
    [DataRow(false)]
    [DataRow(true)]
    public void MemberReservations_SameArgumentsShareReservationsAcrossNestedTypes(bool generated)
    {
        const int rootCount = 80;
        const int memberCount = 14;
        string markers = string.Join('\n', Enumerable.Range(0, rootCount).Select(index => $"public sealed class Marker{index};"));
        string roots = string.Join('\n', Enumerable.Range(0, rootCount).Select(index => $"_ = new Family<Marker{index}>();"));
        string members = string.Join('\n', Enumerable.Range(0, memberCount).Select(index => generated
            ? $$"""
                public async Task<object> Read{{index}}()
                {
                    await Task.Yield();
                    return new Result<T>();
                }
                """
            : $$"""
                public sealed class Nested{{index}}
                {
                    public object Read() => new Result<T>();
                    public object Next() => new Nested{{(index + 1) % memberCount}}();
                }
                public object Read{{index}}() => new Nested{{index}}();
                """));

        AssertMemberReservationSource($$"""
            using System.Threading.Tasks;
            using Windows.Foundation;
            namespace MemberReservations;
            {{markers}}
            public sealed class Result<T> : IStringable { public override string ToString() => "result"; }
            public sealed class Family<T>
            {
                {{members}}
            }
            public static class Program
            {
                public static void Main() { {{roots}} }
            }
            """, associations =>
        {
            Assert.AreEqual(rootCount, associations.Count(static type => type.StartsWith("MemberReservations.Result`1[", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    public void MemberReservations_GenericMethodLoweringPreservesTypeAndMethodArguments()
    {
        AssertMemberReservationSource("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Windows.Foundation;
            namespace MemberReservations;
            public sealed class Left;
            public sealed class Right;
            public sealed class Result<X, Y> : IStringable { public override string ToString() => "result"; }
            public sealed class Family<T>
            {
                public async Task<object> Read<U>()
                {
                    await Task.Yield();
                    return new Result<T, U>();
                }
                public IEnumerable<Result<T, U>> Enumerate<U>()
                {
                    yield return new Result<T, U>();
                }
                public async IAsyncEnumerable<object> EnumerateAsync<U>()
                {
                    await Task.Yield();
                    yield return new Result<U, T>();
                }
            }
            public static class Program
            {
                public static void Main()
                {
                    _ = new Family<Left>().Read<Right>();
                    _ = new Family<Left>().Enumerate<Right>();
                    _ = new Family<Left>().EnumerateAsync<Right>();
                }
            }
            """, associations =>
        {
            Assert.IsTrue(associations.Any(static type =>
                type.StartsWith("MemberReservations.Result`2[[MemberReservations.Left,", StringComparison.Ordinal) &&
                type.Contains("],[MemberReservations.Right,", StringComparison.Ordinal)));
            Assert.IsTrue(associations.Any(static type =>
                type.StartsWith("MemberReservations.Result`2[[MemberReservations.Right,", StringComparison.Ordinal) &&
                type.Contains("],[MemberReservations.Left,", StringComparison.Ordinal)));
            Assert.IsTrue(associations.Any(static type =>
                type.StartsWith("MemberReservations.Family`1+<Enumerate>d__", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    public void MemberReservations_NestedGenericOwnerPreservesFlattenedArguments()
    {
        AssertMemberReservationSource("""
            using System.Threading.Tasks;
            using Windows.Foundation;
            namespace MemberReservations;
            public sealed class Left;
            public sealed class Middle;
            public sealed class Right;
            public sealed class Result<X, Y, Z> : IStringable { public override string ToString() => "result"; }
            public sealed class Family<T>
            {
                public sealed class Nested<U>
                {
                    public async Task<object> Read<V>()
                    {
                        await Task.Yield();
                        return new Result<T, U, V>();
                    }
                }
            }
            public static class Program
            {
                public static void Main() => _ = new Family<Left>.Nested<Middle>().Read<Right>();
            }
            """, associations =>
        {
            Assert.IsTrue(associations.Any(static type =>
                type.StartsWith("MemberReservations.Result`3[[MemberReservations.Left,", StringComparison.Ordinal) &&
                type.Contains("],[MemberReservations.Middle,", StringComparison.Ordinal) &&
                type.Contains("],[MemberReservations.Right,", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MemberReservations_GenericMethodsShareClosedReservations(bool separateOwners)
    {
        const int rootCount = 80;
        const int memberCount = 14;
        string markers = string.Join('\n', Enumerable.Range(0, rootCount).Select(index => $"public sealed class Marker{index};"));
        string roots = string.Join('\n', Enumerable.Range(0, rootCount).Select(index => $"Family.Read<Marker{index}>();"));
        string helpers = string.Join('\n', Enumerable.Range(0, memberCount).Select(index => separateOwners
            ? $"public static class Helper{index} {{ public static object Read<T>() => new Result<T>(); }}"
            : $"private static object Helper{index}<T>() => new Result<T>();"));
        string calls = string.Join('\n', Enumerable.Range(0, memberCount).Select(index => separateOwners
            ? $"_ = Helper{index}.Read<T>();"
            : $"_ = Helper{index}<T>();"));

        AssertMemberReservationSource($$"""
            using Windows.Foundation;
            namespace MemberReservations;
            {{markers}}
            public sealed class Result<T> : IStringable { public override string ToString() => "result"; }
            {{(separateOwners ? helpers : "")}}
            public static class Family
            {
                {{(separateOwners ? "" : helpers)}}
                public static void Read<T>() { {{calls}} }
                public static void Main() { {{roots}} }
            }
            """, associations =>
        {
            Assert.AreEqual(rootCount, associations.Count(static type => type.StartsWith("MemberReservations.Result`1[", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    public void MemberReservations_SupplementalDispatchSharesOrdinaryDescendantReservations()
    {
        const int rootCount = 80;
        const int memberCount = 14;
        string markers = string.Join('\n', Enumerable.Range(0, rootCount).Select(index => $"public sealed class Marker{index};"));
        string calls = string.Join('\n', Enumerable.Range(0, rootCount).Select(index => $"_ = visitor.Visit<Marker{index}>();"));
        string helpers = string.Join('\n', Enumerable.Range(0, memberCount).Select(index => $$"""
            public sealed class Helper{{index}}<T>
            {
                public object Read() => new Result<T>();
                public object Next() => new Helper{{(index + 1) % memberCount}}<T>();
            }
            """));
        string members = string.Join('\n', Enumerable.Range(0, memberCount).Select(index =>
            $"public object Read{index}() => new Helper{index}<T>();"));

        AssertMemberReservationSource($$"""
            using Windows.Foundation;
            namespace MemberReservations;
            {{markers}}
            {{helpers}}
            public sealed class Result<T> : IStringable { public override string ToString() => "result"; }
            public sealed class Family<T> { {{members}} }
            public interface IVisitor { object Visit<T>(); }
            public sealed class Visitor : IVisitor
            {
                public object Visit<T>() => new Family<T>();
            }
            public static class Program
            {
                public static void Main()
                {
                    IVisitor visitor = new Visitor();
                    {{calls}}
                }
            }
            """, associations =>
        {
            Assert.AreEqual(rootCount, associations.Count(static type => type.StartsWith("MemberReservations.Result`1[", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MemberReservations_ChangingArgumentsStillConsumesExpansionBudget(bool methods)
    {
        AssertMemberReservationLimit($$"""
            using Windows.Foundation;
            namespace MemberReservations;
            public sealed class A<T>;
            public sealed class B<T>;
            public sealed class Family
            {
                public sealed class Result<T> : IStringable { public override string ToString() => "result"; }
                public sealed class Node<T>
                {
                    public Node<A<T>> NextA() => new();
                    public Node<B<T>> NextB() => new();
                }
                public static object Expand<T>()
                {
                    _ = Expand<A<T>>();
                    _ = Expand<B<T>>();
                    return new Result<T>();
                }
                public static void Main() => {{(methods ? "_ = Expand<int>()" : "_ = new Node<int>()")}};
            }
            """, ExpansionWarning);
    }

    [TestMethod]
    [DataRow("Node<T>", RecursionWarning)]
    [DataRow("Pair<T, T>", ComplexityWarning)]
    public void MemberReservations_GrowingNestedArgumentsRemainBounded(string argument, string warning)
    {
        AssertMemberReservationLimit($$"""
            namespace MemberReservations;
            public sealed class Family
            {
                public sealed class Pair<X, Y>;
                public sealed class Node<T>
                {
                    public Node<{{argument}}> Next() => new();
                }
                public static void Main() => _ = new Node<int>();
            }
            """, warning);
    }

    [TestMethod]
    public void MemberReservations_UnchangedArgumentsStillRespectDepthLimit()
    {
        const int memberCount = 36;
        string members = string.Join('\n', Enumerable.Range(0, memberCount).Select(index =>
            $"public sealed class Nested{index} {{ public object Next() => new Nested{(index + 1) % memberCount}(); }}"));

        AssertMemberReservationLimit($$"""
            namespace MemberReservations;
            public sealed class Family<T>
            {
                {{members}}
                public object Start() => new Nested0();
            }
            public static class Program
            {
                public static void Main() => _ = new Family<int>();
            }
            """, RecursionWarning);
    }

    [TestMethod]
    public void MemberReservations_UnrelatedTypesShareMatchingArguments()
    {
        const int helperCount = 1030;
        string helpers = string.Join('\n', Enumerable.Range(0, helperCount - 1).Select(index =>
            $"public sealed class Other{index}<T>;"));
        string members = string.Join('\n', Enumerable.Range(0, helperCount).Select(index =>
            $"public object Next{index}() => new Other{index}<T>();"));

        AssertMemberReservationSource($$"""
            using Windows.Foundation;
            namespace MemberReservations;
            {{helpers}}
            public sealed class Other{{helperCount - 1}}<T>
            {
                public object Read() => new Result<T>();
            }
            public sealed class Result<T> : IStringable { public override string ToString() => "result"; }
            public sealed class Family<T> { {{members}} }
            public static class Program
            {
                public static void Main() => _ = new Family<int>();
            }
            """, associations =>
        {
            Assert.IsTrue(associations.Any(static type =>
                type.StartsWith("MemberReservations.Result`1[[System.Int32,", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MemberReservations_CrossModuleTransitionsCannotShareMatchingArguments(bool methods)
    {
        string helpers = string.Join('\n', Enumerable.Range(0, 1030).Select(index => methods
            ? $"public static void Read{index}<T>() {{ }}"
            : $"public sealed class Other{index}<T>;"));
        string calls = string.Join('\n', Enumerable.Range(0, 1030).Select(index => methods
            ? $"Helpers.Read{index}<T>();"
            : $"_ = new Other{index}<T>();"));

        AssertMemberReservationLimit($$"""
            namespace MemberReservations;
            public sealed class Family<T>
            {
                public void Expand() { {{calls}} }
            }
            public static class Program
            {
                public static void Main() => _ = new Family<int>();
            }
            """, ExpansionWarning, dependencySource: $$"""
            namespace MemberReservations;
            {{(methods ? $"public static class Helpers {{ {helpers} }}" : helpers)}}
            """);
    }

    [TestMethod]
    public void MemberReservations_PartiallyOpenMethodContextsCannotShareReservations()
    {
        string helpers = string.Join('\n', Enumerable.Range(0, 1030).Select(index =>
            $"public static void Helper{index}<X, Y>() {{ }}"));
        string calls = string.Join('\n', Enumerable.Range(0, 1030).Select(index => $"Helper{index}<A, B>();"));

        AssertMemberReservationLimit($$"""
            namespace MemberReservations;
            public static class Family
            {
                {{helpers}}
                public static void Enter<A, B>() { {{calls}} }
                public static void Start<T>() => Enter<int, T>();
                public static void Main() { }
            }
            """, ExpansionWarning);
    }

    private static void AssertMemberReservationSource(string source, Action<HashSet<string>> assertAssociations)
    {
        string directory = Directory.CreateTempSubdirectory("InteropMemberReservations_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                [source],
                Path.Combine(directory, "MemberReservations.dll"),
                outputKind: OutputKind.ConsoleApplication);
            byte[]? previous = null;

            foreach (int parallelism in new[] { 1, 4 })
            {
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true, maxDegreesOfParallelism: parallelism);

                Assert.AreEqual(0, exitCode, log);
                Assert.IsFalse(log.Contains(ExpansionWarning, StringComparison.Ordinal), log);
                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                assertAssociations(GetComWrappersTypeAssociations(interop));
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

    private static void AssertMemberReservationLimit(string source, string warning, string? dependencySource = null)
    {
        string directory = Directory.CreateTempSubdirectory("InteropMemberReservationsLimit_").FullName;

        try
        {
            string[] references = dependencySource is null
                ? []
                : [ProjectionWriterRunner.CompileSources([dependencySource], Path.Combine(directory, "Helpers.dll"))];
            string app = ProjectionWriterRunner.CompileSources(
                [source],
                Path.Combine(directory, "MemberReservations.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: references);

            foreach (bool treatWarningsAsErrors in new[] { false, true })
            {
                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                File.Delete(interop);
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors,
                    additionalReferences: references, timeout: TimeSpan.FromSeconds(60));

                StringAssert.Contains(log, warning);
                Assert.AreEqual(treatWarningsAsErrors, exitCode != 0, log);
                Assert.AreEqual(!treatWarningsAsErrors, File.Exists(interop), log);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

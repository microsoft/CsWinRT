// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MethodBodies_TypeMentionedOnlyByCastOrTokenIsDiscovered(bool token)
    {
        AssertDispatchSource($$"""
            using Windows.Foundation;

            namespace RawFacts;

            public sealed class Marker;
            public sealed class Only<T> : IStringable
            {
                public override string ToString() => "only";
            }

            public static class Program
            {
                public static void Main() => _ = Read<Marker>(null!);
                private static object? Read<T>(object value) => {{(token ? "typeof(Only<T>)" : "value as Only<T>")}};
            }
            """, "RawFacts.Only`1[[RawFacts.Marker,");
    }

    [TestMethod]
    public void MethodBodies_SharedBodyPreservesDeclaringAndMethodArgumentsAcrossModules()
    {
        string directory = Directory.CreateTempSubdirectory("InteropSharedMethodFacts_").FullName;

        try
        {
            string shared = ProjectionWriterRunner.CompileSources(
                ["""
                using Windows.Foundation;

                namespace RawFacts;

                public sealed class Left;
                public sealed class Alternate;
                public sealed class Right;
                public sealed class Third;
                public sealed class Pair<X, Y>;
                public sealed class Allocated<T> : IStringable { public override string ToString() => "allocated"; }
                public sealed class ArrayNode<T> : IStringable { public override string ToString() => "array"; }
                public sealed class FieldNode<T> : IStringable { public override string ToString() => "field"; }
                public sealed class CastOnly<T> : IStringable { public override string ToString() => "cast"; }
                public sealed class TokenOnly<T> : IStringable { public override string ToString() => "token"; }

                public static class Storage<T>
                {
                    public static FieldNode<T>? Value;
                }

                public static class Source<TDeclaring>
                {
                    public static object Read<TMethod>(object value) => new object?[]
                    {
                        new Allocated<TDeclaring>(),
                        new ArrayNode<TMethod>[1],
                        Storage<TMethod>.Value,
                        value as CastOnly<Pair<TMethod, TDeclaring>>,
                        typeof(TokenOnly<Pair<TDeclaring, TMethod>>)
                    };
                }
                """],
                Path.Combine(directory, "RawFacts.dll"));
            string first = ProjectionWriterRunner.CompileSources(
                ["""
                public static class First
                {
                    public static object Read() => RawFacts.Source<RawFacts.Left>.Read<RawFacts.Right>(null!);
                }
                """],
                Path.Combine(directory, "FirstFacts.dll"),
                additionalReferences: [shared]);
            string second = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Second
                {
                    public static object Read() => new object[]
                    {
                        RawFacts.Source<RawFacts.Left>.Read<RawFacts.Right>(null!),
                        RawFacts.Source<RawFacts.Left>.Read<RawFacts.Third>(null!),
                        RawFacts.Source<RawFacts.Alternate>.Read<RawFacts.Right>(null!)
                    };
                }
                """],
                Path.Combine(directory, "SecondFacts.dll"),
                additionalReferences: [shared]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main()
                    {
                        _ = First.Read();
                        _ = Second.Read();
                    }
                }
                """],
                Path.Combine(directory, "SharedFactsApp.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [shared, first, second]);
            byte[]? previous = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(
                    directory,
                    app,
                    treatWarningsAsErrors: true,
                    additionalReferences: reverse ? [second, first, shared] : [shared, first, second],
                    maxDegreesOfParallelism: reverse ? 4 : 1);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                HashSet<string> associations = GetComWrappersTypeAssociations(interop);

                foreach (string argument in new[] { "Left", "Alternate" })
                {
                    Assert.IsTrue(associations.Any(type => type.StartsWith($"RawFacts.Allocated`1[[RawFacts.{argument},", StringComparison.Ordinal)), log);
                    Assert.IsFalse(associations.Any(type => type.StartsWith($"RawFacts.FieldNode`1[[RawFacts.{argument},", StringComparison.Ordinal)), log);
                }

                foreach (string argument in new[] { "Right", "Third" })
                {
                    Assert.IsTrue(associations.Any(type => type.StartsWith($"RawFacts.FieldNode`1[[RawFacts.{argument},", StringComparison.Ordinal)), log);
                    Assert.IsTrue(associations.Any(type =>
                        type.StartsWith($"RawFacts.ArrayNode`1[[RawFacts.{argument},", StringComparison.Ordinal) &&
                        type.Contains("]][],", StringComparison.Ordinal)), log);
                }

                foreach ((string declaring, string method) in new[] { ("Left", "Right"), ("Left", "Third"), ("Alternate", "Right") })
                {
                    Assert.IsTrue(associations.Any(type =>
                        type.StartsWith($"RawFacts.CastOnly`1[[RawFacts.Pair`2[[RawFacts.{method},", StringComparison.Ordinal) &&
                        type.Contains($"],[RawFacts.{declaring},", StringComparison.Ordinal)), log);
                    Assert.IsTrue(associations.Any(type =>
                        type.StartsWith($"RawFacts.TokenOnly`1[[RawFacts.Pair`2[[RawFacts.{declaring},", StringComparison.Ordinal) &&
                        type.Contains($"],[RawFacts.{method},", StringComparison.Ordinal)), log);
                }

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
    public void MethodBodies_UnresolvedAllocationOrCastDoesNotBlockClosedTypes(bool cast)
    {
        string directory = Directory.CreateTempSubdirectory("InteropUnresolvedMethodFacts_").FullName;

        try
        {
            string compilerHost = Directory.CreateDirectory(Path.Combine(directory, "compiler-host")).FullName;
            string dependency = ProjectionWriterRunner.CompileSources(
                ["namespace Hidden; public sealed class PrivateType;"],
                Path.Combine(compilerHost, "Hidden.dll"));
            string library = ProjectionWriterRunner.CompileSources(
                [$$"""
                using Windows.Foundation;

                namespace RawFacts;

                public sealed class Marker;
                public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
                public static class Consumer<T>
                {
                    public static object Read(object value) => new object?[]
                    {
                        {{(cast ? "value as Hidden.PrivateType" : "new Hidden.PrivateType()")}},
                        new Node<T>()
                    };
                }
                """],
                Path.Combine(directory, "UnresolvedFacts.dll"),
                additionalReferences: [dependency]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main() => _ = RawFacts.Consumer<RawFacts.Marker>.Read(null!);
                }
                """],
                Path.Combine(directory, "UnresolvedFactsApp.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [library, dependency]);
            (int exitCode, string log) = RunGenerator(
                directory,
                app,
                treatWarningsAsErrors: true,
                additionalReferences: [library]);

            Assert.IsTrue(File.Exists(dependency), "The private assembly must remain available only to its compiler host.");
            Assert.AreEqual(0, exitCode, log);
            Assert.IsFalse(log.Contains("Hidden.PrivateType", StringComparison.Ordinal), log);
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(static type => type.StartsWith("RawFacts.Node`1[[RawFacts.Marker,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MethodBodies_InterveningNonCallClearsReceiverConstraint(bool insertNonCall)
    {
        string directory = Directory.CreateTempSubdirectory("InteropMethodConstraintFacts_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                using Windows.Foundation;

                namespace RawFacts;

                public sealed class Marker;
                public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
                public interface IFactory { static abstract object Create(); }
                public interface IFactory<T> : IFactory
                {
                    static object IFactory.Create() => new Node<T>();
                }
                public sealed class Factory<T> : IFactory<T>;
                public static class Program
                {
                    public static void Main() => _ = Invoke<Factory<Marker>>();
                    private static object Invoke<T>() where T : IFactory => T.Create();
                }
                """],
                Path.Combine(directory, "ConstraintFacts.dll"),
                outputKind: OutputKind.ConsoleApplication);
            ModuleDefinition module = ModuleDefinition.FromBytes(File.ReadAllBytes(app));
            MethodDefinition invoke = module.GetAllTypes().SelectMany(static type => type.Methods)
                .Single(static method => method.Name == "Invoke");
            CilInstructionCollection instructions = invoke.CilMethodBody!.Instructions;
            int constraint = Enumerable.Range(0, instructions.Count)
                .Single(index => instructions[index].OpCode.Code == CilCode.Constrained);

            Assert.AreEqual(CilCode.Call, instructions[constraint + 1].OpCode.Code);

            if (insertNonCall)
            {
                // This parser-only fixture checks interrupted prefixes, not executable constrained-call IL
                instructions.Insert(constraint + 1, new CilInstruction(CilOpCodes.Nop));
                module.Write(app);
            }

            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true);

            Assert.AreEqual(0, exitCode, log);
            Assert.AreEqual(!insertNonCall, GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(static type => type.StartsWith("RawFacts.Node`1[[RawFacts.Marker,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

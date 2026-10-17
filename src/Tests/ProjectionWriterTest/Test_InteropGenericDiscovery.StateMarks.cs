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
    public void DiscoveryMarks_RepeatedCrossModuleCascadesRemainDeterministic()
    {
        string directory = Directory.CreateTempSubdirectory("InteropRepeatedDiscoveryMarks_").FullName;

        try
        {
            string shared = ProjectionWriterRunner.CompileSources(
                ["""
                using System.Collections.Generic;
                using System.Collections.ObjectModel;
                using Windows.Foundation;

                namespace StateMarks;

                public sealed class Marker;
                public sealed class Node<T> : IStringable
                {
                    public override string ToString() => "node";
                }

                public static class Factory
                {
                    public static object Create<T>() => new object[]
                    {
                        new Node<T>(),
                        new Node<T>[1],
                        new List<int>(),
                        new ReadOnlyCollection<int>(new List<int>())
                    };
                }
                """],
                Path.Combine(directory, "StateMarks.dll"));
            const int moduleCount = 8;
            List<string> references = [shared];

            for (int index = 0; index < moduleCount; index++)
            {
                references.Add(ProjectionWriterRunner.CompileSources(
                    [$$"""
                    public static class Consumer{{index}}
                    {
                        public static object Create() => StateMarks.Factory.Create<StateMarks.Marker>();
                    }
                    """],
                    Path.Combine(directory, $"Consumer{index}.dll"),
                    additionalReferences: [shared]));
            }

            string calls = string.Join('\n', Enumerable.Range(0, moduleCount).Select(static index => $"_ = Consumer{index}.Create();"));
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                public static class Program
                {
                    public static void Main()
                    {
                        {{calls}}
                    }
                }
                """],
                Path.Combine(directory, "StateMarksApp.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [.. references]);
            byte[]? previous = null;

            foreach (int parallelism in new[] { 1, 4, 4 })
            {
                (int exitCode, string log) = RunGenerator(
                    directory,
                    app,
                    treatWarningsAsErrors: true,
                    additionalReferences: parallelism == 1 ? [.. references] : [.. references.AsEnumerable().Reverse()],
                    maxDegreesOfParallelism: parallelism);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                HashSet<string> associations = GetComWrappersTypeAssociations(interop);
                string[] nodes = [.. associations.Where(static type =>
                    type.StartsWith("StateMarks.Node`1[[StateMarks.Marker,", StringComparison.Ordinal))];

                // The same class, array, and recursive collection cascades are seen by every module and both visitors
                Assert.HasCount(2, nodes, log);
                Assert.AreEqual(1, nodes.Count(static type => type.Contains("]][],", StringComparison.Ordinal)), log);
                Assert.IsTrue(associations.Any(static type =>
                    type.StartsWith("System.Collections.Generic.List`1[[System.Int32,", StringComparison.Ordinal)), log);
                Assert.IsTrue(associations.Any(static type =>
                    type.StartsWith("System.Collections.ObjectModel.ReadOnlyCollection`1[[System.Int32,", StringComparison.Ordinal)), log);

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
}

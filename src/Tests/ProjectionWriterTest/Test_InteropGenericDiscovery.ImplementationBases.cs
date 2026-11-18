// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    /// <summary>
    /// Collection covariance exposes the generated instance and activation factory bases as candidate element types.
    /// These are managed helpers, not WinRT metadata types, even though they are declared in a reference projection.
    /// </summary>
    [TestMethod]
    [DataRow("List")]
    [DataRow("Queue")]
    public void CollectionsOfImplementations_DoNotTreatGeneratedBasesAsWinRTTypes(string collection)
    {
        string directory = Directory.CreateTempSubdirectory("InteropImplementationCollectionTest_").FullName;

        try
        {
            string metadata = ImplementableHierarchyMetadata.Create(directory);
            string[] projections = new string[2];

            // Compile against the reference surface, but supply the implementation projection to the interop generator
            for (int i = 0; i < projections.Length; i++)
            {
                bool referenceProjection = i == 0;
                string output = Path.Combine(directory, referenceProjection ? "reference" : "implementation");
                ProjectionWriter.Run(new ProjectionWriterOptions
                {
                    InputPaths = [metadata, ImplementableHierarchyMetadata.FindWindowsFoundationWinmd()],
                    OutputFolder = output,
                    OutputAssemblyName = "WinRT.Projection",
                    Include = ["Contoso"],
                    ImplementWinMDTypes = true,
                    ReferenceProjection = referenceProjection
                });
                projections[i] = ProjectionWriterRunner.CompileSources(
                    Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                    Path.Combine(directory, referenceProjection ? "Contoso.Projection.dll" : "WinRT.Projection.dll"),
                    referenceProjection);
            }

            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                using System.Collections.Generic;

                public static class Program
                {
                    public static void Main()
                    {
                        {{collection}}<Implementation> snapshot = new(new[] { new Implementation() });
                        foreach (Implementation item in snapshot)
                        {
                            item.Dispose();
                        }

                        // Cover direct helper-typed collections as well as the concrete implementation above
                        List<ABI.Contoso.ClosableBase> bases = [new Implementation()];
                        List<Factory> factories = [new Factory()];

                        // The actual projected runtime class remains a valid WinRT collection element
                        List<Contoso.ClosableBase> projected = [new Implementation()];
                    }

                    private sealed class Implementation : ABI.Contoso.ClosableBase
                    {
                        public override void ClosableBaseMethod() { }
                        public override void Dispose() { }
                    }

                    private sealed class Factory : ABI.Contoso.ClosableBaseActivationFactory
                    {
                        public override ABI.Contoso.ClosableBase ActivateInstance() => new Implementation();
                    }
                }
                """],
                Path.Combine(directory, "ImplementationCollections.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [projections[0]]);
            (int exitCode, string log) = RunGenerator(directory, app,
                treatWarningsAsErrors: true,
                additionalReferences: [projections[0]],
                additionalArguments: [$"--winrt-projection-assembly-path {projections[1]}"]);

            Assert.AreEqual(0, exitCode, log);
            string[] associations = [.. GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))];

            // Filtering helper bases must retain the implementation's CCW and valid projected/interface adapters
            Assert.IsTrue(associations.Any(type => type.StartsWith("Program+Implementation,", StringComparison.Ordinal)));
            Assert.IsTrue(associations.Any(type => type.Contains("IEnumeratorAdapter`1[[Contoso.ClosableBase,", StringComparison.Ordinal)));
            Assert.IsTrue(associations.Any(type => type.Contains("IEnumeratorAdapter`1[[System.IDisposable,", StringComparison.Ordinal)));
            Assert.IsFalse(associations.Any(type => type.Contains("IEnumeratorAdapter`1[[ABI.Contoso.ClosableBase", StringComparison.Ordinal)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

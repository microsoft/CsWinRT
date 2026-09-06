// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using ImplGeneratorTest.Helpers;
using Microsoft.CodeAnalysis.Emit;

namespace ImplGeneratorTest;

/// <summary>
/// End-to-end tests for the debug information of the forwarder assembly the impl generator produces.
/// </summary>
/// <remarks>
/// The forwarder replaces the compiled output of a reference projection, so it is the assembly that ends up
/// in <c>lib/&lt;tfm&gt;</c> of the resulting NuGet package. It is emitted directly as metadata rather than
/// compiled, so unless its debug information is synthesized it ships with no symbols at all: no Source Link,
/// no compiler flags, and no way to tell it was built deterministically.
/// </remarks>
[TestClass]
public class Test_DebugDirectory
{
    /// <summary>
    /// The custom debug information kind for an embedded source document.
    /// </summary>
    private static readonly Guid EmbeddedSourceKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

    /// <summary>
    /// The custom debug information kind for the compilation options (the compiler flags).
    /// </summary>
    private static readonly Guid CompilationOptionsKind = new("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");

    /// <summary>
    /// The custom debug information kind for the compilation metadata references.
    /// </summary>
    private static readonly Guid CompilationMetadataReferencesKind = new("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");

    [TestMethod]
    public void DebugDirectory_HasTheEntriesOfADeterministicEmbeddedSymbolsBuild()
    {
        ImplGeneratorRunner.Run(DebugInformationFormat.Embedded, static (_, forwarderPath) =>
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    DebugDirectoryEntryType.CodeView,
                    DebugDirectoryEntryType.PdbChecksum,
                    DebugDirectoryEntryType.Reproducible,
                    DebugDirectoryEntryType.EmbeddedPortablePdb
                },
                ImplGeneratorRunner.GetDebugDirectoryEntryTypes(forwarderPath),
                "Expected the forwarder to have the debug directory of a deterministic build with embedded symbols.");
        });
    }

    [TestMethod]
    public void DebugDirectory_WithNoInputSymbols_IsStillProduced()
    {
        // The input assembly is compiled as a reference assembly by the real build, so it never has any
        // symbols to carry over. The debug information of the forwarder must not depend on it.
        ImplGeneratorRunner.Run(null, static (inputAssemblyPath, forwarderPath) =>
        {
            CollectionAssert.DoesNotContain(
                ImplGeneratorRunner.GetDebugDirectoryEntryTypes(inputAssemblyPath),
                DebugDirectoryEntryType.EmbeddedPortablePdb,
                "Expected the input assembly to have no symbols to carry over.");

            CollectionAssert.Contains(
                ImplGeneratorRunner.GetDebugDirectoryEntryTypes(forwarderPath),
                DebugDirectoryEntryType.EmbeddedPortablePdb,
                "Expected the forwarder to have an embedded portable PDB anyway.");
        });
    }

    [TestMethod]
    public void DebugDirectory_CodeViewAndChecksumEntries_MatchTheEmbeddedPortablePdb()
    {
        // A CodeView record that does not identify the PDB next to it would make every consumer that
        // resolves symbols through a symbol server (rather than through the embedded copy) fail
        ImplGeneratorRunner.Run(DebugInformationFormat.Embedded, static (_, forwarderPath) =>
        {
            Assert.IsTrue(
                ImplGeneratorRunner.IsCodeViewEntryConsistent(forwarderPath),
                "Expected the CodeView entry of the forwarder to identify its embedded portable PDB.");

            Assert.IsTrue(
                ImplGeneratorRunner.IsPdbChecksumValid(forwarderPath),
                "Expected the PDB checksum entry of the forwarder to match its embedded portable PDB.");
        });
    }

    [TestMethod]
    public void EmbeddedPortablePdb_HasAnEmbeddedDeterministicallyNamedDocument()
    {
        // These are the two things a NuGet package health check looks at to decide whether an assembly has
        // Source Link and was built deterministically: every document must be embedded (or source linked),
        // and every document name must be path mapped (which is what the '/_' prefix marks).
        ImplGeneratorRunner.Run(DebugInformationFormat.Embedded, static (_, forwarderPath) =>
        {
            ImmutableArray<string> documentNames = ImplGeneratorRunner.GetEmbeddedPortablePdbDocumentNames(forwarderPath);

            Assert.AreEqual(1, documentNames.Length, $"Expected exactly one document, but got [{string.Join(", ", documentNames)}].");
            Assert.IsTrue(documentNames[0].StartsWith("/_/", StringComparison.Ordinal), $"Expected a deterministic document name, but got '{documentNames[0]}'.");

            CollectionAssert.Contains(
                ImplGeneratorRunner.GetEmbeddedPortablePdbDebugInformationKinds(forwarderPath),
                EmbeddedSourceKind,
                "Expected the document of the forwarder to be embedded.");
        });
    }

    [TestMethod]
    public void EmbeddedPortablePdb_HasCompilerFlags()
    {
        ImplGeneratorRunner.Run(DebugInformationFormat.Embedded, static (_, forwarderPath) =>
        {
            ImmutableArray<Guid> kinds = ImplGeneratorRunner.GetEmbeddedPortablePdbDebugInformationKinds(forwarderPath);

            CollectionAssert.Contains(kinds, CompilationOptionsKind, "Expected the forwarder to have compilation options information.");
            CollectionAssert.Contains(kinds, CompilationMetadataReferencesKind, "Expected the forwarder to have compilation metadata references information.");

            // Tooling ignores the compilation options entirely below version 2, so emitting them without
            // this entry (or with a lower value) would be equivalent to emitting nothing at all
            Assert.AreEqual(
                "2",
                ImplGeneratorRunner.GetCompilationOptionValue(forwarderPath, "version"),
                "Expected the compilation options of the forwarder to declare a supported version.");
        });
    }

    [TestMethod]
    public void EmbeddedPortablePdb_EmbeddedDocument_DescribesTheTypeForwards()
    {
        // The embedded document is what a consumer resolving symbols for this assembly is shown, so it has
        // to actually describe the assembly rather than just be a placeholder that satisfies a check
        ImplGeneratorRunner.Run(DebugInformationFormat.Embedded, static (_, forwarderPath) =>
        {
            string source = ImplGeneratorRunner.GetEmbeddedPortablePdbDocumentText(forwarderPath);

            StringAssert.Contains(source, "<auto-generated/>");
            StringAssert.Contains(source, "TypeForwardedTo(typeof(global::TestInput.PublicType))");
        });
    }

    [TestMethod]
    public void Forwarder_IsDeterministic()
    {
        // The 'Reproducible' entry above claims the output is a pure function of its input, so it must be
        (byte[] first, byte[] second) = ImplGeneratorRunner.RunTwice();

        CollectionAssert.AreEqual(first, second, "Expected two runs of the generator to produce the same forwarder.");
    }

    [TestMethod]
    public void Forwarder_WhenStrongNamed_StillHasSymbols()
    {
        // The forwarder is signed after being written, so the debug directory has to be laid out without
        // disturbing the space reserved for the strong name signature (and vice versa)
        ImplGeneratorRunner.Run(
            DebugInformationFormat.Embedded,
            static (_, forwarderPath) =>
            {
                Assert.IsTrue(ImplGeneratorRunner.IsStrongNamed(forwarderPath), "Expected the forwarder to be signed.");

                CollectionAssert.Contains(
                    ImplGeneratorRunner.GetDebugDirectoryEntryTypes(forwarderPath),
                    DebugDirectoryEntryType.EmbeddedPortablePdb,
                    "Expected the strong named forwarder to still have an embedded portable PDB.");

                Assert.IsTrue(
                    ImplGeneratorRunner.IsCodeViewEntryConsistent(forwarderPath),
                    "Expected the CodeView entry of the strong named forwarder to identify its embedded portable PDB.");
            },
            strongName: true);
    }
}

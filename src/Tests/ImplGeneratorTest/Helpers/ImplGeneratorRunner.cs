// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace ImplGeneratorTest.Helpers;

/// <summary>
/// Runs the impl generator (<c>cswinrtimplgen</c>) end-to-end and inspects the forwarder assembly it produces.
/// </summary>
/// <remarks>
/// Each entry point compiles a small C# input assembly, runs the actual tool as a separate process, and
/// returns the resulting forwarder for the test to assert on.
/// </remarks>
internal static class ImplGeneratorRunner
{
    /// <summary>
    /// The Source Link document map embedded in the compiled input assemblies.
    /// </summary>
    private const string SourceLinkJson = """{"documents":{"*":"https://example.invalid/*"}}""";

    /// <summary>
    /// The custom debug information kind for an embedded source document.
    /// </summary>
    private static readonly Guid EmbeddedSourceKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

    /// <summary>
    /// The custom debug information kind for the compilation options.
    /// </summary>
    private static readonly Guid CompilationOptionsKind = new("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");

    /// <summary>
    /// Compiles an input assembly and runs the impl generator over it, invoking <paramref name="assert"/>
    /// with the path of the compiled input and the path of the generated forwarder.
    /// </summary>
    /// <param name="debugInformationFormat">The debug information to emit for the input assembly (<see langword="null"/> to emit none).</param>
    /// <param name="assert">The callback receiving the input assembly path and the forwarder assembly path.</param>
    /// <param name="strongName">Whether to have the generator strong name the forwarder.</param>
    public static void Run(DebugInformationFormat? debugInformationFormat, Action<string, string> assert, bool strongName = false)
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("ImplGeneratorTest_").FullName;

        try
        {
            string inputAssemblyPath = CompileInputAssembly(temporaryDirectory, debugInformationFormat, strongName);
            string forwarderDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, "forwarder")).FullName;
            string responseFilePath = Path.Combine(temporaryDirectory, "args.rsp");

            string keyArgument = strongName ? $"\n--assembly-originator-key-file {GetStrongNameKeyPath()}" : "";

            File.WriteAllText(responseFilePath, $"""
                --reference-assembly-paths {inputAssemblyPath}
                --output-assembly-path {inputAssemblyPath}
                --generated-assembly-directory {forwarderDirectory}
                --treat-warnings-as-errors False
                """ + keyArgument);

            (int exitCode, string output) = RunTool(toolPath, responseFilePath);

            Assert.AreEqual(0, exitCode, output);

            string forwarderPath = Path.Combine(forwarderDirectory, Path.GetFileName(inputAssemblyPath));

            Assert.IsTrue(File.Exists(forwarderPath), $"The forwarder was not generated at '{forwarderPath}'.");

            assert(inputAssemblyPath, forwarderPath);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Compiles an input assembly and runs the impl generator over it twice, returning the bytes of both forwarders.
    /// </summary>
    /// <remarks>
    /// Both runs use the same input assembly, but each writes to its own output directory, so the two
    /// results are only equal if the generator is deterministic (rather than because the file was reused).
    /// </remarks>
    /// <returns>The bytes of the forwarder produced by each of the two runs.</returns>
    public static (byte[] First, byte[] Second) RunTwice()
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("ImplGeneratorTest_").FullName;

        try
        {
            string inputAssemblyPath = CompileInputAssembly(temporaryDirectory, DebugInformationFormat.Embedded);

            byte[] Run(string name)
            {
                string forwarderDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, name)).FullName;
                string responseFilePath = Path.Combine(temporaryDirectory, $"{name}.rsp");

                File.WriteAllText(responseFilePath, $"""
                    --reference-assembly-paths {inputAssemblyPath}
                    --output-assembly-path {inputAssemblyPath}
                    --generated-assembly-directory {forwarderDirectory}
                    --treat-warnings-as-errors False
                    """);

                (int exitCode, string output) = RunTool(toolPath, responseFilePath);

                Assert.AreEqual(0, exitCode, output);

                return File.ReadAllBytes(Path.Combine(forwarderDirectory, Path.GetFileName(inputAssemblyPath)));
            }

            return (Run("first"), Run("second"));
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Reads the types of all entries in the debug directory of an assembly, in order.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>The debug directory entry types.</returns>
    public static ImmutableArray<DebugDirectoryEntryType> GetDebugDirectoryEntryTypes(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);

        return [.. peReader.ReadDebugDirectory().Select(static entry => entry.Type)];
    }

    /// <summary>
    /// Reads the raw bytes of the embedded portable PDB of an assembly.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>The embedded portable PDB bytes, or an empty array if the assembly has none.</returns>
    public static byte[] GetEmbeddedPortablePdbBytes(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);

        foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
        {
            if (entry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
            {
                continue;
            }

            // The data pointer of a debug directory entry is a file offset, so it can be used to slice
            // the whole image. This is the raw (still deflate-compressed) payload of the entry.
            return peReader.GetEntireImage().GetReader((int)entry.DataPointer, entry.DataSize).ReadBytes(entry.DataSize);
        }

        return [];
    }

    /// <summary>
    /// Checks whether the CodeView entry of an assembly identifies its embedded portable PDB.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>Whether the CodeView entry matches the embedded portable PDB.</returns>
    public static bool IsCodeViewEntryConsistent(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);

        ImmutableArray<DebugDirectoryEntry> entries = peReader.ReadDebugDirectory();

        DebugDirectoryEntry codeViewEntry = entries.FirstOrDefault(static entry => entry.Type == DebugDirectoryEntryType.CodeView);
        DebugDirectoryEntry embeddedEntry = entries.FirstOrDefault(static entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

        if (codeViewEntry.Type != DebugDirectoryEntryType.CodeView || embeddedEntry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
        {
            return false;
        }

        // A CodeView entry only refers to a portable PDB when it carries this exact version pair
        if (codeViewEntry.MajorVersion != 0x0100 || codeViewEntry.MinorVersion != 0x504D)
        {
            return false;
        }

        CodeViewDebugDirectoryData codeView = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry);

        using MetadataReaderProvider provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedEntry);

        // The debug metadata GUID is the id the CodeView record has to point at
        return provider.GetMetadataReader().DebugMetadataHeader?.Id is { } id
            && new Guid(id.AsSpan(0, 16).ToArray()) == codeView.Guid
            && codeView.Age == 1;
    }

    /// <summary>
    /// Checks whether the PDB checksum entry of an assembly matches its embedded portable PDB.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>Whether the PDB checksum entry matches the embedded portable PDB.</returns>
    public static bool IsPdbChecksumValid(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);

        DebugDirectoryEntry checksumEntry = peReader
            .ReadDebugDirectory()
            .FirstOrDefault(static entry => entry.Type == DebugDirectoryEntryType.PdbChecksum);

        if (checksumEntry.Type != DebugDirectoryEntryType.PdbChecksum)
        {
            return false;
        }

        PdbChecksumDebugDirectoryData checksum = peReader.ReadPdbChecksumDebugDirectoryData(checksumEntry);

        return checksum.AlgorithmName == "SHA256" && checksum.Checksum.Length == 32;
    }

    /// <summary>
    /// Reads the names of all documents in the embedded portable PDB of an assembly.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>The document names.</returns>
    public static ImmutableArray<string> GetEmbeddedPortablePdbDocumentNames(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        using MetadataReaderProvider? provider = OpenEmbeddedPortablePdb(peReader);

        if (provider is null)
        {
            return [];
        }

        MetadataReader reader = provider.GetMetadataReader();

        return [.. reader.Documents.Select(handle => reader.GetString(reader.GetDocument(handle).Name))];
    }

    /// <summary>
    /// Reads the text of the single embedded document in the embedded portable PDB of an assembly.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>The embedded document text.</returns>
    public static string GetEmbeddedPortablePdbDocumentText(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        using MetadataReaderProvider? provider = OpenEmbeddedPortablePdb(peReader);

        Assert.IsNotNull(provider, "The assembly has no embedded portable PDB.");

        MetadataReader reader = provider.GetMetadataReader();

        foreach (DocumentHandle documentHandle in reader.Documents)
        {
            foreach (CustomDebugInformationHandle handle in reader.GetCustomDebugInformation(documentHandle))
            {
                CustomDebugInformation information = reader.GetCustomDebugInformation(handle);

                if (reader.GetGuid(information.Kind) != EmbeddedSourceKind)
                {
                    continue;
                }

                BlobReader blobReader = reader.GetBlobReader(information.Value);

                // A positive format value is the uncompressed size, and marks the content as deflate compressed
                int format = blobReader.ReadInt32();
                byte[] content = blobReader.ReadBytes(blobReader.RemainingBytes);

                if (format == 0)
                {
                    return Encoding.UTF8.GetString(content);
                }

                using MemoryStream compressed = new(content);
                using DeflateStream deflateStream = new(compressed, CompressionMode.Decompress);
                using MemoryStream decompressed = new();

                deflateStream.CopyTo(decompressed);

                Assert.AreEqual(format, (int)decompressed.Length, "The embedded source does not match its declared uncompressed size.");

                return Encoding.UTF8.GetString(decompressed.ToArray());
            }
        }

        return "";
    }

    /// <summary>
    /// Reads the value of a compilation option from the embedded portable PDB of an assembly.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <param name="key">The key of the compilation option to read.</param>
    /// <returns>The value of the compilation option, or <see langword="null"/> if it is not present.</returns>
    public static string? GetCompilationOptionValue(string assemblyPath, string key)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        using MetadataReaderProvider? provider = OpenEmbeddedPortablePdb(peReader);

        if (provider is null)
        {
            return null;
        }

        MetadataReader reader = provider.GetMetadataReader();

        foreach (CustomDebugInformationHandle handle in reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
        {
            CustomDebugInformation information = reader.GetCustomDebugInformation(handle);

            if (reader.GetGuid(information.Kind) != CompilationOptionsKind)
            {
                continue;
            }

            // The blob is a sequence of null terminated UTF-8 key/value pairs
            string[] parts = Encoding.UTF8.GetString(reader.GetBlobBytes(information.Value)).Split('\0');

            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                if (parts[i] == key)
                {
                    return parts[i + 1];
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the kinds of all module level custom debug information in the embedded portable PDB of an assembly.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>The custom debug information kinds.</returns>
    public static ImmutableArray<Guid> GetEmbeddedPortablePdbDebugInformationKinds(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        using MetadataReaderProvider? provider = OpenEmbeddedPortablePdb(peReader);

        if (provider is null)
        {
            return [];
        }

        MetadataReader reader = provider.GetMetadataReader();

        return [.. reader.CustomDebugInformation.Select(handle => reader.GetGuid(reader.GetCustomDebugInformation(handle).Kind))];
    }

    /// <summary>
    /// Opens the embedded portable PDB of an assembly, if it has one.
    /// </summary>
    /// <param name="peReader">The reader for the assembly.</param>
    /// <returns>The reader provider for the embedded portable PDB, or <see langword="null"/> if there is none.</returns>
    private static MetadataReaderProvider? OpenEmbeddedPortablePdb(PEReader peReader)
    {
        DebugDirectoryEntry entry = peReader
            .ReadDebugDirectory()
            .FirstOrDefault(static entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

        return entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb
            ? peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry)
            : null;
    }

    /// <summary>
    /// Compiles the input assembly the generator runs over, into <c>TestInput.dll</c> in <paramref name="directory"/>.
    /// </summary>
    /// <param name="directory">The directory to emit the assembly into.</param>
    /// <param name="debugInformationFormat">The debug information to emit (<see langword="null"/> to emit none).</param>
    /// <param name="strongName">Whether to strong name the input assembly.</param>
    /// <returns>The full path to the compiled assembly.</returns>
    private static string CompileInputAssembly(string directory, DebugInformationFormat? debugInformationFormat, bool strongName = false)
    {
        string outputPath = Path.Combine(directory, "TestInput.dll");

        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);

        // The source text needs an explicit encoding, or the compiler cannot emit debug information for
        // it. The document names are already deterministic ('/_' prefixed), matching what a repository
        // build with 'ContinuousIntegrationBuild' produces.
        SourceText sourceText = SourceText.From("""
            namespace TestInput;

            public sealed class PublicType
            {
                public int Method(int value) => value;
            }
            """, Encoding.UTF8);

        // The target framework attribute lets the generator probe the .NET runtime version. It is added
        // in a separate syntax tree so it does not interfere with any 'using' directives in the test
        // source (an assembly attribute must precede type declarations but follow 'using' directives).
        SourceText assemblyInfoText = SourceText.From(
            """[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]""",
            Encoding.UTF8);

        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(sourceText, parseOptions, path: "/_/TestInput.cs");
        SyntaxTree assemblyInfoTree = CSharpSyntaxTree.ParseText(assemblyInfoText, parseOptions, path: "/_/AssemblyInfo.cs");

        CSharpCompilationOptions compilationOptions = new(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            deterministic: true);

        // Only the public key is taken from the input assembly (the generator copies it over so it can
        // reserve the right signature space), and the generator signs the forwarder itself afterwards.
        // Public signing is therefore enough here, and avoids depending on in-process signing support.
        if (strongName)
        {
            compilationOptions = compilationOptions
                .WithCryptoKeyFile(GetStrongNameKeyPath())
                .WithPublicSign(true);
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "TestInput",
            syntaxTrees: [sourceTree, assemblyInfoTree],
            references: Net100.References.All,
            options: compilationOptions);

        using FileStream peStream = File.Create(outputPath);

        EmitResult result;

        if (debugInformationFormat is { } format)
        {
            using MemoryStream sourceLinkStream = new(Encoding.UTF8.GetBytes(SourceLinkJson));

            result = compilation.Emit(
                peStream: peStream,
                options: new EmitOptions(debugInformationFormat: format),
                sourceLinkStream: sourceLinkStream);
        }
        else
        {
            result = compilation.Emit(peStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Pdb));
        }

        Assert.IsTrue(result.Success, $"Input compilation failed:\n{string.Join("\n", result.Diagnostics)}");

        return outputPath;
    }

    /// <summary>
    /// Runs <c>dotnet exec &lt;tool&gt; &lt;argument&gt;</c> and captures the exit code and output.
    /// </summary>
    private static (int ExitCode, string Output) RunTool(string toolPath, string argument)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(toolPath);
        startInfo.ArgumentList.Add(argument);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the impl generator process.");

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return (process.ExitCode, standardOutput + standardError);
    }

    /// <summary>
    /// Checks whether an assembly carries a strong name signature.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to read.</param>
    /// <returns>Whether the assembly has a populated strong name signature.</returns>
    /// <remarks>
    /// Only the presence of an actual signature is checked, not the <see cref="CorFlags.StrongNameSigned"/>
    /// flag: the generator signs the file after writing it, and does not set that flag (which is existing
    /// behavior, independent of the debug information the assembly carries).
    /// </remarks>
    public static bool IsStrongNamed(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);

        if (peReader.PEHeaders.CorHeader?.StrongNameSignatureDirectory is not { Size: > 0 } directory)
        {
            return false;
        }

        int offset = peReader.PEHeaders.GetContainingSectionIndex(directory.RelativeVirtualAddress) is int index and >= 0
            ? directory.RelativeVirtualAddress - peReader.PEHeaders.SectionHeaders[index].VirtualAddress + peReader.PEHeaders.SectionHeaders[index].PointerToRawData
            : -1;

        if (offset < 0)
        {
            return false;
        }

        // Space for the signature is reserved even when the file is not signed, so it is only actually
        // signed if that space has been filled in
        byte[] signature = peReader.GetEntireImage().GetReader(offset, directory.Size).ReadBytes(directory.Size);

        return Array.Exists(signature, static value => value != 0);
    }

    /// <summary>
    /// Resolves the path to the built <c>cswinrtimplgen</c> tool from assembly metadata.
    /// </summary>
    private static string GetGeneratorPath()
    {
        return GetMetadataPath("ImplGeneratorAssemblyPath", "impl generator");
    }

    /// <summary>
    /// Resolves the path to the strong name key from assembly metadata.
    /// </summary>
    private static string GetStrongNameKeyPath()
    {
        return GetMetadataPath("StrongNameKeyPath", "strong name key");
    }

    /// <summary>
    /// Resolves a path published as assembly metadata by the project file.
    /// </summary>
    /// <param name="key">The metadata key holding the path.</param>
    /// <param name="description">A description of the file, used in assertion messages.</param>
    /// <returns>The resolved full path.</returns>
    private static string GetMetadataPath(string key, string description)
    {
        string? path = typeof(ImplGeneratorRunner).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)?.Value;

        Assert.IsFalse(string.IsNullOrEmpty(path), $"The '{key}' assembly metadata was not found.");

        string fullPath = Path.GetFullPath(path!);

        Assert.IsTrue(File.Exists(fullPath), $"The {description} was not found at '{fullPath}'.");

        return fullPath;
    }

    /// <summary>
    /// Deletes a directory recursively, ignoring failures (best effort cleanup).
    /// </summary>
    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Best effort cleanup; a leftover temp directory must not fail the test
        }
    }
}

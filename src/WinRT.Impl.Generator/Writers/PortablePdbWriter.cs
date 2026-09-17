// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;

namespace WindowsRuntime.ImplGenerator.Writers;

/// <summary>
/// Writes the portable PDB describing a generated forwarder assembly.
/// </summary>
/// <remarks>
/// <para>
/// The forwarder assembly is not produced by a compiler, so there is no PDB for it to carry over. It is
/// also not produced from any source on disk: it is emitted directly as metadata, from the public API
/// surface of the reference projection it stands in for. Its debug information is therefore synthesized
/// here, describing exactly what the assembly is: a single, embedded, generated document listing the type
/// forwards it contains, plus the compilation information tooling expects to find on a shipped assembly.
/// </para>
/// <para>
/// The document is embedded rather than pointed at through Source Link, for the same reason the .NET SDK
/// embeds untracked sources: it is generated, so it exists in no repository a symbol server could serve it
/// from. Everything written here is derived from the forwarder itself, so the result is deterministic.
/// </para>
/// </remarks>
internal static class PortablePdbWriter
{
    /// <summary>
    /// The language id for C#, as defined by the portable PDB specification.
    /// </summary>
    private static readonly Guid CSharpLanguage = new("3F5162F8-07C6-11D3-9053-00C04FA302A1");

    /// <summary>
    /// The hash algorithm id for SHA-256, as defined by the portable PDB specification.
    /// </summary>
    private static readonly Guid Sha256HashAlgorithm = new("8829D00F-11B8-4213-878B-770E8597AC16");

    /// <summary>
    /// The custom debug information kind for an embedded source document.
    /// </summary>
    private static readonly Guid EmbeddedSourceKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

    /// <summary>
    /// The custom debug information kind for the compilation options.
    /// </summary>
    private static readonly Guid CompilationOptionsKind = new("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");

    /// <summary>
    /// The custom debug information kind for the compilation metadata references.
    /// </summary>
    private static readonly Guid CompilationMetadataReferencesKind = new("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");

    /// <summary>
    /// Writes the portable PDB for a forwarder assembly.
    /// </summary>
    /// <param name="documentName">The name of the generated document (a deterministic, <c>/_</c> prefixed path).</param>
    /// <param name="documentText">The text of the generated document, which is embedded in the PDB.</param>
    /// <param name="references">The assemblies the forwarder references.</param>
    /// <param name="compilationOptions">The compilation options to record, as key/value pairs.</param>
    /// <returns>The resulting portable PDB.</returns>
    public static PortablePdb Write(
        string documentName,
        string documentText,
        IReadOnlyList<MetadataReferenceInfo> references,
        IReadOnlyList<KeyValuePair<string, string>> compilationOptions)
    {
        // The document is synthetic, so its 'file bytes' are defined here: UTF-8, with no byte order mark.
        // The same bytes are both hashed and embedded, so the two are consistent by construction.
        byte[] documentBytes = Encoding.UTF8.GetBytes(documentText);

        MetadataBuilder metadata = new();

        DocumentHandle document = metadata.AddDocument(
            name: metadata.GetOrAddDocumentName(documentName),
            hashAlgorithm: metadata.GetOrAddGuid(Sha256HashAlgorithm),
            hash: metadata.GetOrAddBlob(SHA256.HashData(documentBytes)),
            language: metadata.GetOrAddGuid(CSharpLanguage));

        _ = metadata.AddCustomDebugInformation(
            parent: document,
            kind: metadata.GetOrAddGuid(EmbeddedSourceKind),
            value: metadata.GetOrAddBlob(BuildEmbeddedSource(documentBytes)));

        _ = metadata.AddCustomDebugInformation(
            parent: EntityHandle.ModuleDefinition,
            kind: metadata.GetOrAddGuid(CompilationOptionsKind),
            value: metadata.GetOrAddBlob(BuildCompilationOptions(compilationOptions)));

        _ = metadata.AddCustomDebugInformation(
            parent: EntityHandle.ModuleDefinition,
            kind: metadata.GetOrAddGuid(CompilationMetadataReferencesKind),
            value: metadata.GetOrAddBlob(BuildMetadataReferences(references)));

        byte[]? checksum = null;

        // The PDB id is derived from a hash of the PDB content, which is also the value the 'PdbChecksum'
        // debug directory entry carries. Deriving both from the same hash keeps them consistent, and makes
        // the whole file a pure function of its contents (so two runs produce byte identical output).
        BlobContentId ComputeContentId(IEnumerable<Blob> blobs)
        {
            using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            foreach (Blob blob in blobs)
            {
                incrementalHash.AppendData(blob.GetBytes());
            }

            checksum = incrementalHash.GetHashAndReset();

            return BlobContentId.FromHash(checksum);
        }

        // The PDB carries no method debug information, so it references no type system rows at all
        PortablePdbBuilder pdbBuilder = new(
            tablesAndHeaps: metadata,
            typeSystemRowCounts: ImmutableArray.Create(new int[MetadataTokens.TableCount]),
            entryPoint: default,
            idProvider: ComputeContentId);

        BlobBuilder pdbBlob = new();

        BlobContentId contentId = pdbBuilder.Serialize(pdbBlob);

        return new PortablePdb(pdbBlob.ToArray(), contentId.Guid, contentId.Stamp, checksum!);
    }

    /// <summary>
    /// Builds the blob for an embedded source document.
    /// </summary>
    /// <param name="documentBytes">The raw bytes of the document.</param>
    /// <returns>The embedded source blob.</returns>
    private static byte[] BuildEmbeddedSource(byte[] documentBytes)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        // A positive format value is the uncompressed size, and marks the content as deflate compressed
        writer.Write(documentBytes.Length);

        using (DeflateStream deflateStream = new(stream, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflateStream.Write(documentBytes, 0, documentBytes.Length);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Builds the blob for the compilation options.
    /// </summary>
    /// <param name="compilationOptions">The compilation options, as key/value pairs.</param>
    /// <returns>The compilation options blob.</returns>
    private static byte[] BuildCompilationOptions(IReadOnlyList<KeyValuePair<string, string>> compilationOptions)
    {
        using MemoryStream stream = new();

        foreach (KeyValuePair<string, string> option in compilationOptions)
        {
            WriteNullTerminatedString(stream, option.Key);
            WriteNullTerminatedString(stream, option.Value);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Builds the blob for the compilation metadata references.
    /// </summary>
    /// <param name="references">The assemblies the forwarder references.</param>
    /// <returns>The compilation metadata references blob.</returns>
    private static byte[] BuildMetadataReferences(IReadOnlyList<MetadataReferenceInfo> references)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        foreach (MetadataReferenceInfo reference in references)
        {
            WriteNullTerminatedString(stream, reference.FileName);
            WriteNullTerminatedString(stream, "");

            // The low bit marks the reference as an assembly (rather than a module), and the
            // second bit would mark it as having its interop types embedded, which never applies
            writer.Write((byte)1);

            // The timestamp, image size and MVID identify the exact image a reference resolved to. The
            // forwarder is emitted against assembly references that are only resolved when the consuming
            // application is built (the projection assemblies do not exist yet), so there is no image to
            // describe here. Zero is written rather than an invented value, meaning 'unknown'.
            writer.Write(0);
            writer.Write(0);
            writer.Write(Guid.Empty.ToByteArray());
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Writes a null terminated UTF-8 string to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The string to write.</param>
    private static void WriteNullTerminatedString(Stream stream, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);

        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte(0);
    }
}

/// <summary>
/// A portable PDB produced by <see cref="PortablePdbWriter"/>.
/// </summary>
/// <param name="Bytes">The serialized portable PDB.</param>
/// <param name="Id">The id identifying the PDB, carried by the CodeView debug directory entry.</param>
/// <param name="Stamp">The stamp identifying the PDB, carried by the CodeView debug directory entry.</param>
/// <param name="Checksum">The SHA-256 checksum of the PDB content.</param>
internal sealed record PortablePdb(byte[] Bytes, Guid Id, uint Stamp, byte[] Checksum);

/// <summary>
/// An assembly referenced by a generated forwarder assembly.
/// </summary>
/// <param name="FileName">The file name of the referenced assembly.</param>
internal sealed record MetadataReferenceInfo(string FileName);

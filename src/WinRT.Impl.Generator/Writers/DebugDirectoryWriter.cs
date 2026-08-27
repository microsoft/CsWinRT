// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Compression;
using AsmResolver;
using AsmResolver.PE;
using AsmResolver.PE.Debug;

namespace WindowsRuntime.ImplGenerator.Writers;

/// <summary>
/// Writes the debug directory of a generated forwarder assembly.
/// </summary>
/// <remarks>
/// The entries written here match the ones a deterministic C# compilation with embedded symbols produces,
/// which is what tooling (debuggers, symbol servers, and NuGet package health checks) expects to find on a
/// shipped assembly.
/// </remarks>
internal static class DebugDirectoryWriter
{
    /// <summary>
    /// The signature of an embedded portable PDB payload (<c>MPDB</c>).
    /// </summary>
    private const uint EmbeddedPortablePdbSignature = 0x42_44_50_4D;

    /// <summary>
    /// The debug directory entry type for an embedded portable PDB.
    /// </summary>
    /// <remarks><see cref="DebugDataType"/> has no member for this entry type.</remarks>
    private const DebugDataType EmbeddedPortablePdbType = (DebugDataType)17;

    /// <summary>
    /// The debug directory entry type for a PDB checksum.
    /// </summary>
    /// <remarks><see cref="DebugDataType"/> has no member for this entry type.</remarks>
    private const DebugDataType PdbChecksumType = (DebugDataType)19;

    /// <summary>
    /// The name of the hash algorithm used for the PDB checksum, with its null terminator.
    /// </summary>
    private static ReadOnlySpan<byte> PdbChecksumAlgorithmName => "SHA256\0"u8;

    /// <summary>
    /// Writes the debug directory for a forwarder assembly, embedding its portable PDB.
    /// </summary>
    /// <param name="image">The <see cref="PEImage"/> for the forwarder assembly.</param>
    /// <param name="pdb">The portable PDB describing the forwarder assembly.</param>
    /// <param name="pdbFileName">The file name to record for the PDB in the CodeView entry.</param>
    public static void Write(PEImage image, PortablePdb pdb, string pdbFileName)
    {
        // The CodeView entry identifies the PDB. Its version fields are what marks the record as
        // referring to a portable PDB, rather than to a Windows PDB.
        image.DebugData.Add(new DebugDataEntry(new RsdsDataSegment
        {
            Guid = pdb.Id,
            Age = 1,
            Path = pdbFileName
        })
        {
            MajorVersion = 0x0100,
            MinorVersion = 0x504D,
            TimeDateStamp = pdb.Stamp
        });

        // The checksum lets consumers verify that a PDB they resolved is the exact one this assembly
        // was built with, which matters because the same PDB is also served outside of this assembly.
        image.DebugData.Add(new DebugDataEntry(new CustomDebugDataSegment(PdbChecksumType, new DataSegment(BuildPdbChecksum(pdb))))
        {
            MajorVersion = 1,
            MinorVersion = 0
        });

        // The forwarder is a pure function of its input assembly, so it is always reproducible
        image.DebugData.Add(new DebugDataEntry(new EmptyDebugDataSegment(DebugDataType.Repro)));

        // Embedding the PDB keeps the symbols with the assembly, so they are always available. This
        // matches what 'DebugType=embedded' produces, which is how CsWinRT itself is built.
        image.DebugData.Add(new DebugDataEntry(new CustomDebugDataSegment(EmbeddedPortablePdbType, new DataSegment(BuildEmbeddedPortablePdb(pdb))))
        {
            MajorVersion = 0x0100,
            MinorVersion = 0x0100
        });
    }

    /// <summary>
    /// Builds the payload of a PDB checksum debug directory entry.
    /// </summary>
    /// <param name="pdb">The portable PDB describing the forwarder assembly.</param>
    /// <returns>The PDB checksum payload.</returns>
    private static byte[] BuildPdbChecksum(PortablePdb pdb)
    {
        using MemoryStream stream = new();

        stream.Write(PdbChecksumAlgorithmName);
        stream.Write(pdb.Checksum);

        return stream.ToArray();
    }

    /// <summary>
    /// Builds the payload of an embedded portable PDB debug directory entry.
    /// </summary>
    /// <param name="pdb">The portable PDB describing the forwarder assembly.</param>
    /// <returns>The embedded portable PDB payload.</returns>
    private static byte[] BuildEmbeddedPortablePdb(PortablePdb pdb)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(EmbeddedPortablePdbSignature);
        writer.Write(pdb.Bytes.Length);

        using (DeflateStream deflateStream = new(stream, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflateStream.Write(pdb.Bytes, 0, pdb.Bytes.Length);
        }

        return stream.ToArray();
    }
}

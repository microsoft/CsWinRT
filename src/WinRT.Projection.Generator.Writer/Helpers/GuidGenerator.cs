// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Text;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Mirrors the C++ <c>guid_generator.h</c>. Generates Windows Runtime parameterized GUIDs (PIIDs)
/// using the WinRT-defined namespace GUID (d57af411-737b-c042-abae-878b1e16adee) and SHA-1.
/// </summary>
internal static class GuidGenerator
{
    // The WinRT namespace GUID, in the same byte order as cppwinrt's namespace_guid.
    // Per cppwinrt: { 0xd57af411, 0x737b, 0xc042, { 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee } }
    // Layout (little-endian format): bytes are { 0x11, 0xf4, 0x7a, 0xd5, 0x7b, 0x73, 0x42, 0xc0, 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee }
    private static readonly byte[] s_namespaceBytes =
    {
        0x11, 0xf4, 0x7a, 0xd5,
        0x7b, 0x73,
        0x42, 0xc0,
        0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee
    };

    /// <summary>
    /// Generates a GUID for the given Windows Runtime parameterized type signature.
    /// </summary>
    /// <param name="signature">The parameterized signature (e.g., "pinterface({...};Boolean)").</param>
    /// <returns>The resulting GUID.</returns>
    public static Guid Generate(string signature)
    {
        byte[] sigBytes = Encoding.UTF8.GetBytes(signature);
        byte[] buffer = new byte[s_namespaceBytes.Length + sigBytes.Length];
        Array.Copy(s_namespaceBytes, buffer, s_namespaceBytes.Length);
        Array.Copy(sigBytes, 0, buffer, s_namespaceBytes.Length, sigBytes.Length);

        byte[] hash = SHA1.HashData(buffer);

        // Take first 16 bytes
        byte[] guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        // Endian swap (Data1, Data2, Data3 are in big-endian in the SHA1 hash; .NET Guid expects little-endian)
        Swap(guidBytes, 0, 3);
        Swap(guidBytes, 1, 2);
        Swap(guidBytes, 4, 5);
        Swap(guidBytes, 6, 7);

        // Set named GUID fields: version 5, variant RFC4122
        guidBytes[7] = (byte)((guidBytes[7] & 0x0f) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);

        return new Guid(guidBytes);
    }

    private static void Swap(byte[] arr, int a, int b)
    {
        (arr[a], arr[b]) = (arr[b], arr[a]);
    }
}

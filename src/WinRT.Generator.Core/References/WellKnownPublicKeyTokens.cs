// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.Generator.References;

/// <summary>
/// Well-known public key tokens for assemblies used by the CsWinRT generators.
/// </summary>
internal static class WellKnownPublicKeyTokens
{
    /// <summary>
    /// The public key token for <c>mscorlib</c>.
    /// </summary>
    public static readonly byte[] MSCorLib = [0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89];

    /// <summary>
    /// The public key token for <c>System.Memory.dll</c>.
    /// </summary>
    public static readonly byte[] SystemMemory = [0xCC, 0x7B, 0x13, 0xFF, 0xCD, 0x2D, 0xDD, 0x51];

    /// <summary>
    /// The public key token for <c>System.ObjectModel.dll</c>.
    /// </summary>
    public static readonly byte[] SystemObjectModel = [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A];

    /// <summary>
    /// The public key token for <c>System.Runtime.InteropServices.dll</c>.
    /// </summary>
    public static readonly byte[] SystemRuntimeInteropServices = [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A];

    /// <summary>
    /// The public key token for <c>System.Numerics.Vectors.dll</c>.
    /// </summary>
    public static readonly byte[] SystemNumericsVectors = [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A];

    /// <summary>
    /// The public key token for <c>System.Threading.dll</c>.
    /// </summary>
    public static readonly byte[] SystemThreading = [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A];

    /// <summary>
    /// The public key token for CsWinRT assemblies (<c>31bf3856ad364e35</c>).
    /// </summary>
    public static readonly byte[] CsWinRT = [0x31, 0xBF, 0x38, 0x56, 0xAD, 0x36, 0x4E, 0x35];
}

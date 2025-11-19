// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known public key tokens.
/// </summary>
internal static class WellKnownPublicKeyTokens
{
    /// <summary>
    /// The public key data for <c>System.Memory.dll</c>.
    /// </summary>
    public static readonly byte[] SystemMemory = [0xCC, 0x7B, 0x13, 0xFF, 0xCD, 0x2D, 0xDD, 0x51];

    /// <summary>
    /// The public key data for <c>System.ObjectModel.dll</c>.
    /// </summary>
    public static readonly byte[] SystemObjectModel = [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A];

    /// <summary>
    /// The public key data for <c>System.Runtime.InteropServices.dll</c>.
    /// </summary>
    public static readonly byte[] SystemRuntimeInteropServices = [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A];
}
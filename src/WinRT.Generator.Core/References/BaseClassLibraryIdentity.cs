// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.References;

/// <summary>
/// Helpers to identify assemblies that are part of the .NET base class library (BCL), i.e. the default set
/// of libraries shipped by the .NET SDK (the runtime shared frameworks and their reference assemblies).
/// </summary>
/// <remarks>
/// <para>
/// Detection is based on the assembly's public key token. The .NET runtime and framework assemblies are all
/// strong-name signed with a small, fixed set of Microsoft-owned keys. User code cannot be signed with these
/// keys, so a match reliably identifies a framework assembly (and, conversely, user or third-party assemblies
/// never match).
/// </para>
/// <para>
/// The set below was validated against the assemblies shipped in the <c>Microsoft.NETCore.App</c>,
/// <c>Microsoft.WindowsDesktop.App</c>, and <c>Microsoft.AspNetCore.App</c> shared frameworks.
/// </para>
/// </remarks>
internal static class BaseClassLibraryIdentity
{
    /// <summary>The ECMA public key token (<c>b77a5c561934e089</c>): <c>mscorlib</c>, <c>System</c>, <c>System.Core</c>, <c>System.Xml</c>, etc.</summary>
    private static ReadOnlySpan<byte> EcmaPublicKeyToken => [0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89];

    /// <summary>The Microsoft public key token (<c>b03f5f7f11d50a3a</c>): most <c>System.*</c> assemblies, <c>Microsoft.CSharp</c>, <c>Microsoft.VisualBasic</c>, <c>Microsoft.Win32.*</c>, etc.</summary>
    private static ReadOnlySpan<byte> MicrosoftPublicKeyToken => [0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A];

    /// <summary>The .NET public key token (<c>cc7b13ffcd2ddd51</c>): <c>netstandard</c>, <c>System.Buffers</c>, <c>System.Memory</c>, <c>System.Numerics.Vectors</c>, etc.</summary>
    private static ReadOnlySpan<byte> DotNetPublicKeyToken => [0xCC, 0x7B, 0x13, 0xFF, 0xCD, 0x2D, 0xDD, 0x51];

    /// <summary>The runtime public key token (<c>7cec85d7bea7798e</c>): <c>System.Private.CoreLib</c>.</summary>
    private static ReadOnlySpan<byte> CoreLibPublicKeyToken => [0x7C, 0xEC, 0x85, 0xD7, 0xBE, 0xA7, 0x79, 0x8E];

    /// <summary>The Windows public key token (<c>31bf3856ad364e35</c>): <c>WindowsBase</c>, WCF/WPF assemblies, <c>System.ServiceModel.Web</c>, etc.</summary>
    private static ReadOnlySpan<byte> WindowsPublicKeyToken => [0x31, 0xBF, 0x38, 0x56, 0xAD, 0x36, 0x4E, 0x35];

    /// <summary>The ASP.NET Core public key token (<c>adb9793829ddae60</c>): the <c>Microsoft.AspNetCore.*</c> shared framework assemblies.</summary>
    private static ReadOnlySpan<byte> AspNetCorePublicKeyToken => [0xAD, 0xB9, 0x79, 0x38, 0x29, 0xDD, 0xAE, 0x60];

    /// <summary>
    /// Checks whether a given public key token belongs to a .NET base class library / framework assembly.
    /// </summary>
    /// <param name="publicKeyToken">The 8-byte public key token to check.</param>
    /// <returns>Whether <paramref name="publicKeyToken"/> identifies a .NET base class library / framework assembly.</returns>
    public static bool IsBaseClassLibraryPublicKeyToken(ReadOnlySpan<byte> publicKeyToken)
    {
        // A public key token is always exactly 8 bytes. Anything else (including an empty span for
        // unsigned assemblies) is definitely not a framework assembly. The 'SequenceEqual' checks
        // below would already return 'false' in that case, but the explicit length check documents
        // the invariant and short-circuits the comparisons for non-framework assemblies.
        return
            publicKeyToken.Length == 8 &&
            (publicKeyToken.SequenceEqual(EcmaPublicKeyToken) ||
             publicKeyToken.SequenceEqual(MicrosoftPublicKeyToken) ||
             publicKeyToken.SequenceEqual(DotNetPublicKeyToken) ||
             publicKeyToken.SequenceEqual(CoreLibPublicKeyToken) ||
             publicKeyToken.SequenceEqual(WindowsPublicKeyToken) ||
             publicKeyToken.SequenceEqual(AspNetCorePublicKeyToken));
    }
}

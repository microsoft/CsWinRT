// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from https://github.com/terrafx/terrafx.interop.windows.
// Defined in 'winrt/hstring.h' in the Windows SDK for Windows 10.0.26100.0.

#pragma warning disable CS0649, IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Represents a header for an <c>HSTRING</c>.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/hstring/ns-hstring-hstring_header"/>
internal struct HSTRING_HEADER
{
    /// <summary>
    /// Reserved for future use.
    /// </summary>
    public _Reserved_e__Union Reserved;

    // The definition of this anonymous union in hstring.h is different on 64 bit compared to 32 bit.
    // On 64 bit, Reserved1 is a 24 byte buffer. On 32 bit, Reserved1 is only a 20 byte buffer.
    // To avoid producing multiple builds of the managed bindings, we use a 16 byte buffer and a native int instead, preserving the same layout.
    // Using this strategy, trying to match the native definition of this union would be too contrived.
    // Since the contents of this structure are undefined, it is not important to provide the same definitions.
    public unsafe partial struct _Reserved_e__Union
    {
        internal fixed byte Reserved1_0[16];
        internal nuint Reserved1_1;
    }
}

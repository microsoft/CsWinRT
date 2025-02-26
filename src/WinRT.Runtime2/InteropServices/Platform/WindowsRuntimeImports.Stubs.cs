// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeImports"/>
internal partial class WindowsRuntimeImports
{
    // Handcrafted P/Invoke with TFM-specific handling, or thin high-level abstractions (eg. 'TryGetProcAddress'/'GetProcAddress')

    /// <inheritdoc cref="GetProcAddress(nint, sbyte*)"/>
    /// <remarks>The <paramref name="lpProcNameUtf8"/> parameter is meant to be an UTF8 literal with only ASCII characters.</remarks>
    public static unsafe void* TryGetProcAddress(HMODULE hModule, ReadOnlySpan<byte> lpProcNameUtf8)
    {
        fixed (byte* lpProcName = lpProcNameUtf8)
        {
            return GetProcAddress(hModule, (sbyte*)lpProcName);
        }
    }

    /// <inheritdoc cref="GetProcAddress(nint, sbyte*)"/>
    /// <remarks>The <paramref name="lpProcNameUtf8"/> parameter is meant to be an UTF8 literal with only ASCII characters.</remarks>
    /// <exception cref="Win32Exception">Thrown if loading the target function fails.</exception>
    public static unsafe void* GetProcAddress(HMODULE hModule, ReadOnlySpan<byte> lpProcNameUtf8)
    {
        void* functionPtr = TryGetProcAddress(hModule, lpProcNameUtf8);

        if (functionPtr is null)
        {
            // The 'Win32Exception' constructor will automatically get the last system error
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowWin32Exception() => throw new Win32Exception();

            ThrowWin32Exception();
        }

        return functionPtr;
    }

    /// <inheritdoc cref="LoadLibraryExW(char*, nint, uint)"/>
    public static unsafe HMODULE LoadLibraryExW(string lpLibFileNameUtf16, HANDLE hFile, uint dwFlags)
    {
        fixed (char* lpLibFileName = lpLibFileNameUtf16)
        {
            return LoadLibraryExW(lpLibFileName, hFile, dwFlags);
        }
    }
}

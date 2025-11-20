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
    // Thin high-level abstractions (eg. 'TryGetProcAddress'/'GetProcAddress')

    /// <param name="iid">The IID of an interface that is implemented on the context object.</param>
    /// <returns>The pointer to the interface specified by <paramref name="iid"/> on the context object.</returns>
    /// <exception cref="Exception">Thrown if getting the object context fails.</exception>
    /// <inheritdoc cref="CoGetObjectContext(Guid*, void**)"/>
    public static unsafe void* CoGetObjectContext(in Guid iid)
    {
        void* objectContext;

        fixed (Guid* piid = &iid)
        {
            CoGetObjectContext(piid, &objectContext).Assert();
        }

        return objectContext;
    }

    /// <exception cref="Exception">Thrown if getting the context token fails.</exception>
    /// <inheritdoc cref="CoGetContextToken(nuint*)"/>
    public static unsafe nuint CoGetContextToken()
    {
        nuint contextToken;

        CoGetContextToken(&contextToken).Assert();

        return contextToken;
    }

    /// <remarks>The <paramref name="lpProcNameUtf8"/> parameter is meant to be an UTF8 literal with only ASCII characters.</remarks>
    /// <inheritdoc cref="GetProcAddress(nint, sbyte*)"/>
    public static unsafe void* TryGetProcAddress(HMODULE hModule, ReadOnlySpan<byte> lpProcNameUtf8)
    {
        fixed (byte* lpProcName = lpProcNameUtf8)
        {
            return GetProcAddress(hModule, (sbyte*)lpProcName);
        }
    }

    /// <remarks>The <paramref name="lpProcNameUtf8"/> parameter is meant to be an UTF8 literal with only ASCII characters.</remarks>
    /// <exception cref="Win32Exception">Thrown if loading the target function fails.</exception>
    /// <inheritdoc cref="GetProcAddress(nint, sbyte*)"/>
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
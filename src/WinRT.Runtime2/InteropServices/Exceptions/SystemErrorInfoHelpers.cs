// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides helper methods for working with exceptions via system-provided functionality.
/// </summary>
internal static unsafe class SystemErrorInfoHelpers
{
    /// <summary>
    /// Gets the system error message for the provided <c>HRESULT</c> value.
    /// </summary>
    /// <param name="hresult">The input <c>HRESULT</c> value to get the error message for.</param>
    /// <returns>The error message for <paramref name="hresult"/>, if available.</returns>
    public static string? GetSystemErrorMessageForHR(HRESULT hresult)
    {
        char* message = null;

        if (WindowsRuntimeImports.FormatMessageW(
            dwFlags:
                FORMAT.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                FORMAT.FORMAT_MESSAGE_FROM_SYSTEM |
                FORMAT.FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT.FORMAT_MESSAGE_MAX_WIDTH_MASK,
            lpSource: null,
            dwMessageId: (uint)hresult,
            dwLanguageId: 0,
            lpBuffer: &message,
            nSize: 0,
            pArguments: null) != 0)
        {
            try
            {
                return $"{new string(message)}(0x{hresult:X8})";
            }
            finally
            {
                // 'LocalHandle' isn't needed since 'FormatMessage' uses 'LMEM_FIXED'
                _ = WindowsRuntimeImports.LocalFree(message);
            }
        }

        return null;
    }
}
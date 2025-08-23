// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary><c>HRESULT</c>-s for common scenarios.</summary>
internal static partial class WellKnownErrorCodes
{
    /// <summary>
    /// Checks whether a given <c>HRESULT</c> represents a success code.
    /// </summary>
    /// <param name="hr">The <c>HRESULT</c> to check.</param>
    /// <returns>Whether <paramref name="hr"/> represents a success code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Succeeded(HRESULT hr)
    {
        return hr >= S_OK;
    }
}
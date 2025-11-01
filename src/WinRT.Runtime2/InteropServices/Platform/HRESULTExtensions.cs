// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for <c>HRESULT</c>.
/// </summary>
internal static class HRESULTExtensions
{
    /// <summary>
    /// Throws an exception if <paramref name="hresult"/> represents an error.
    /// </summary>
    /// <param name="hresult">The input <see cref="HRESULT"/> to check.</param>
    /// <exception cref="System.Exception">Thrown if <paramref name="hresult"/> represents an error.</exception>
    /// <remarks>This method directly wraps <see cref="Marshal.ThrowExceptionForHR(int)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(this HRESULT hresult)
    {
        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <summary>
    /// Checks whether a given <c>HRESULT</c> represents a success code.
    /// </summary>
    /// <param name="hresult">The <c>HRESULT</c> to check.</param>
    /// <returns>Whether <paramref name="hresult"/> represents a success code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Succeeded(this HRESULT hresult)
    {
        return hresult >= WellKnownErrorCodes.S_OK;
    }
}

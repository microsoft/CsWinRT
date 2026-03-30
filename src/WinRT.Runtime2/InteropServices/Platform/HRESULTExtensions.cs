// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for <c>HRESULT</c>.
/// </summary>
internal static class HRESULTExtensions
{
    /// <param name="hresult">The input <see cref="HRESULT"/> to check.</param>
    extension(HRESULT hresult)
    {
        /// <summary>
        /// Checks whether a given <c>HRESULT</c> represents a success code.
        /// </summary>
        public bool Succeeded
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => hresult >= WellKnownErrorCodes.S_OK;
        }

        /// <summary>
        /// Checks whether a given <c>HRESULT</c> represents a failure code.
        /// </summary>
        public bool Failed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => hresult < WellKnownErrorCodes.S_OK;
        }

        /// <summary>
        /// Throws an exception if a given <c>HRESULT</c> represents an error.
        /// </summary>
        /// <exception cref="System.Exception">Thrown if the input <c>HRESULT</c> represents an error.</exception>
        /// <remarks>This method directly wraps <see cref="Marshal.ThrowExceptionForHR(int)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert()
        {
            Marshal.ThrowExceptionForHR(hresult);
        }
    }
}
#endif
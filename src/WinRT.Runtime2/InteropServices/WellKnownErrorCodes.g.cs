// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices
{
    /// <summary><c>HRESULT</c>-s for common scenarios.</summary>
    internal static class WellKnownErrorCodes
    {
        public const HRESULT S_OK = unchecked((int)0x0000);
        public const HRESULT E_POINTER = unchecked((int)0x80004003);
    }
}
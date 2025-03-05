// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WellKnownErrorCodes"/>
internal partial class WellKnownErrorCodes
{
    /// <summary>Operation successful.</summary>
    public const HRESULT S_OK = unchecked((int)0x00000000);

    /// <summary>No such interface supported.</summary>
    public const HRESULT E_NOINTERFACE = unchecked((int)0x80004002);

    /// <summary>Pointer that is not valid.</summary>
    public const HRESULT E_POINTER = unchecked((int)0x80004003);
}
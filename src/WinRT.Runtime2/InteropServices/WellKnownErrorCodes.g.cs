﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WellKnownErrorCodes"/>
internal partial class WellKnownErrorCodes
{
    /// <summary>Operation successful.</summary>
    public const HRESULT S_OK = unchecked((int)0x00000000);

    /// <summary>Operation aborted.</summary>
    public const HRESULT E_ABORT = unchecked((int)0x80004004);

    /// <summary>No such interface supported.</summary>
    public const HRESULT E_NOINTERFACE = unchecked((int)0x80004002);

    /// <summary>Pointer that is not valid.</summary>
    public const HRESULT E_POINTER = unchecked((int)0x80004003);

    /// <summary>Class not registered.</summary>
    public const HRESULT REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
}
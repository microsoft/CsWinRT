// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime;

/// <summary>
/// IManagedExceptionErrorInfo
/// </summary>
[Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
[GeneratedComInterface]
internal unsafe partial interface IErrorInfo
{
    Guid GetGuid();

    [return: MarshalAs(UnmanagedType.BStr)]
    string? GetDescription();

    [return: MarshalAs(UnmanagedType.BStr)]
    string? GetHelpFile();

    [return: MarshalAs(UnmanagedType.BStr)]
    string? GetHelpFileContent();

    [return: MarshalAs(UnmanagedType.BStr)]
    string? GetSource();
}
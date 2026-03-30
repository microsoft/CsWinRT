// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;

namespace WindowsRuntime.InteropServices;

/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/ne-windowsstoragecom-handle_access_options"/>
[Flags]
internal enum HANDLE_ACCESS_OPTIONS : uint
{
    HAO_NONE = 0,
    HAO_READ_ATTRIBUTES = 0x80,
    HAO_READ = 0x120089,
    HAO_WRITE = 0x120116,
    HAO_DELETE = 0x10000
}
#endif
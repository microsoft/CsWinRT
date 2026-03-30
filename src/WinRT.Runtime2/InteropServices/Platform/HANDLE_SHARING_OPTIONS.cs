// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;

namespace WindowsRuntime.InteropServices;

/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/ne-windowsstoragecom-handle_sharing_options"/>
[Flags]
internal enum HANDLE_SHARING_OPTIONS : uint
{
    HSO_SHARE_NONE = 0,
    HSO_SHARE_READ = 0x1,
    HSO_SHARE_WRITE = 0x2,
    HSO_SHARE_DELETE = 0x4
}
#endif
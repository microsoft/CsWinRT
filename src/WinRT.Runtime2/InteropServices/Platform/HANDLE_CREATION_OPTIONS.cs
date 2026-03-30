// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/ne-windowsstoragecom-handle_creation_options"/>
internal enum HANDLE_CREATION_OPTIONS : uint
{
    HCO_CREATE_NEW = 0x1,
    HCO_CREATE_ALWAYS = 0x2,
    HCO_OPEN_EXISTING = 0x3,
    HCO_OPEN_ALWAYS = 0x4,
    HCO_TRUNCATE_EXISTING = 0x5
}
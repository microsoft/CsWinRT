// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/ne-inspectable-trustlevel"/>
internal enum TrustLevel
{
    BaseTrust = 0,
    PartialTrust = BaseTrust + 1,
    FullTrust = PartialTrust + 1
}

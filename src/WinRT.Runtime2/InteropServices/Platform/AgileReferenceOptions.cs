// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Runtime.Versioning;

namespace WindowsRuntime.InteropServices;

/// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/ne-combaseapi-agilereferenceoptions"/>
[SupportedOSPlatform("windows6.3")]
internal enum AgileReferenceOptions
{
    AGILEREFERENCE_DEFAULT = 0,
    AGILEREFERENCE_DELAYEDMARSHAL = 1
}
#endif
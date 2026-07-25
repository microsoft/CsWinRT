// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents the trust level of an activatable class.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/ne-inspectable-trustlevel"/>
[WindowsRuntimeImplementationOnlyMember]
public enum TrustLevel
{
    /// <summary>
    /// The component has access to resources that are not protected.
    /// </summary>
    BaseTrust = 0,

    /// <summary>
    /// The component has access to resources requested in the app manifest and approved by the user.
    /// </summary>
    PartialTrust = BaseTrust + 1,

    /// <summary>
    /// The component requires the full privileges of the user.
    /// </summary>
    FullTrust = PartialTrust + 1
}

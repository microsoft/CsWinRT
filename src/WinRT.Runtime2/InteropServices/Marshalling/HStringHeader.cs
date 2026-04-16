// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// Represents a header for a fast-pass <c>HSTRING</c> value (passed without copying).
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public struct HStringHeader
{
    /// <summary>
    /// Reserved for future use.
    /// </summary>
    /// <remarks>
    /// Using this field to mirror the layout of <see cref="HSTRING_HEADER"/> with one fewer indirection.
    /// </remarks>
    internal HSTRING_HEADER._Reserved_e__Union _reserved;
}

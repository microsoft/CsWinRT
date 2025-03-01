// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Enumeration of flags for <see cref="WindowsRuntimeObjectReference"/>.
/// </summary>
/// <seealso cref="CreateComInterfaceFlags"/>
[Flags]
internal enum CreateObjectReferenceFlags
{
    /// <summary>
    /// No additional flags.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// The object is aggregated.
    /// </summary>
    IsAggregated = 0x1 << 0,

    /// <summary>
    /// The object should not be released when disposed. This is the case for aggregated objects with a reference tracker.
    /// </summary>
    PreventReleaseOnDispose = 0x1 << 1,

    /// <summary>
    /// The object should not decremenet the reference count on the reference tracker when disposed.
    /// This is the case when the real reference count has been handled outside of this managed wrapper.
    /// </summary>
    PreventReleaseFromTrackerSourceOnDispose = 0x1 << 2
}

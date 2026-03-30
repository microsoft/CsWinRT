// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The type map group placeholder for all Windows Runtime types that need to support <see cref="Type"/> marshalling.
/// </summary>
/// <remarks>
/// This type is only meant to be used as type map group for <see cref="System.Runtime.InteropServices.TypeMapping"/> APIs.
/// </remarks>
public abstract class WindowsRuntimeMetadataTypeMapGroup
{
    /// <summary>
    /// This type should never be instantiated (it just can't be static because it needs to be used as a type argument).
    /// </summary>
    private WindowsRuntimeMetadataTypeMapGroup()
    {
    }
}
#endif
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for <see cref="System.Collections.Generic.IEnumerator{T}"/> adapter types.
/// </summary>
internal interface IEnumeratorAdapter
{
    /// <summary>
    /// Gets the wrapped <see cref="IEnumerator"/> instance.
    /// </summary>
    IEnumerator Enumerator { get; }
}
#endif
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
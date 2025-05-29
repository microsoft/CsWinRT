// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface providing information on a given Windows Runtime interface.
/// </summary>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IWindowsRuntimeInterface
{
    /// <summary>
    /// Gets the IID for the implemented interface.
    /// </summary>
    static abstract ref readonly Guid IID { get; }
}

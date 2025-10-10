// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface providing information on a given Windows Runtime interface.
/// </summary>
/// <remarks>
/// This interface is only meant to be used to support marshalling code for generic instantiations.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IWindowsRuntimeInterface
{
    /// <summary>
    /// Gets the IID for the implemented interface.
    /// </summary>
    static abstract ref readonly Guid IID { get; }
}

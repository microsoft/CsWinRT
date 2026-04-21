// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Enables a class to be activated by the Windows Runtime.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/activation/nn-activation-iactivationfactory"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[Guid("00000035-0000-0000-C000-000000000046")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IActivationFactory
{
    /// <summary>
    /// Creates a new instance of the Windows Runtime class that is associated with the current activation factory.
    /// </summary>
    /// <returns>The newly activated instance.</returns>
    object ActivateInstance();
}

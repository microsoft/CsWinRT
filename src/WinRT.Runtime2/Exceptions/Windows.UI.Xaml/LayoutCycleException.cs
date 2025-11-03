// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Windows.UI.Xaml;

/// <summary>
/// Represents an exception that occurs when a layout cycle is detected during GUI layout.
/// </summary>
/// <remarks>
/// Sets <see cref="Exception.HResult"/> to <see cref="WellKnownErrorCodes.E_LAYOUTCYCLE"/>
/// to enable consistent interop/error mapping.
/// </remarks>
internal sealed class LayoutCycleException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutCycleException"/> class
    /// with a default error message indicating a layout cycle occurred.
    /// </summary>
    public LayoutCycleException()
        : base("A cycle occurred while laying out the GUI.")
    {
        HResult = WellKnownErrorCodes.E_LAYOUTCYCLE;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutCycleException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public LayoutCycleException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_LAYOUTCYCLE;
    }
}

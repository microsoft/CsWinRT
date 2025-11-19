// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Windows.UI.Xaml;

/// <summary>
/// Represents an exception that occurs when an element is not enabled.
/// </summary>
/// <remarks>
/// Sets <see cref="Exception.HResult"/> to <see cref="WellKnownErrorCodes.E_ELEMENTNOTENABLED"/>
/// to enable consistent interop/error mapping.
/// </remarks>
internal sealed class ElementNotEnabledException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElementNotEnabledException"/> class
    /// with a default error message indicating the element is not enabled.
    /// </summary>
    public ElementNotEnabledException()
        : base("The element is not enabled.")
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTENABLED;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementNotEnabledException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ElementNotEnabledException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTENABLED;
    }
}
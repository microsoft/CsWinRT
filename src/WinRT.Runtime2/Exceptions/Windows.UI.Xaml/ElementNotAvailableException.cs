// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Windows.UI.Xaml;

/// <summary>
/// Represents an exception that occurs when an element is not available.
/// </summary>
/// <remarks>
/// Sets <see cref="Exception.HResult"/> to <see cref="WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE"/>
/// to enable consistent interop/error mapping.
/// </remarks>
internal sealed class ElementNotAvailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElementNotAvailableException"/> class
    /// with a default error message indicating the element is not available.
    /// </summary>
    public ElementNotAvailableException()
        : base("The element is not available.")
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementNotAvailableException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ElementNotAvailableException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE;
    }
}
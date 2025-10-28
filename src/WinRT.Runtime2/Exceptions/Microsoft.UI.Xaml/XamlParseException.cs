// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents an exception that occurs when XAML parsing fails.
/// </summary>
/// <remarks>
/// Sets <see cref="Exception.HResult"/> to <see cref="WellKnownErrorCodes.E_XAMLPARSEFAILED"/>
/// to enable consistent interop/error mapping.
/// </remarks>
internal sealed class XamlParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XamlParseException"/> class
    /// with a default error message indicating XAML parsing failure.
    /// </summary>
    public XamlParseException()
        : base("XAML parsing failed.")
    {
        HResult = WellKnownErrorCodes.E_XAMLPARSEFAILED;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlParseException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public XamlParseException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_XAMLPARSEFAILED;
    }
}

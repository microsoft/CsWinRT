// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Microsoft.UI.Xaml;

internal sealed class ElementNotAvailableException : Exception
{
    public ElementNotAvailableException()
        : base("The element is not available.")
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE;
    }

    public ElementNotAvailableException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE;
    }

    public ElementNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE;
    }
}
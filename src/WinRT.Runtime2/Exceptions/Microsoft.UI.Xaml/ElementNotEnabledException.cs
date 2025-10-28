// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Microsoft.UI.Xaml;

internal sealed class ElementNotEnabledException : Exception
{
    public ElementNotEnabledException()
        : base("The element is not enabled.")
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTENABLED;
    }

    public ElementNotEnabledException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTENABLED;
    }

    public ElementNotEnabledException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = WellKnownErrorCodes.E_ELEMENTNOTENABLED;
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Microsoft.UI.Xaml;

internal sealed class LayoutCycleException : Exception
{
    public LayoutCycleException()
        : base("A cycle occurred while laying out the GUI.")
    {
        HResult = WellKnownErrorCodes.E_LAYOUTCYCLE;
    }

    public LayoutCycleException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_LAYOUTCYCLE;
    }

    public LayoutCycleException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = WellKnownErrorCodes.E_LAYOUTCYCLE;
    }
}


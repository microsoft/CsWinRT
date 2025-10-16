// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Microsoft.UI.Xaml;

internal class ElementNotAvailableException : Exception
{
    public ElementNotAvailableException()
        : base("The element is not available.")
    {
        HResult = ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
    }

    public ElementNotAvailableException(string message)
        : base(message)
    {
        HResult = ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
    }

    public ElementNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
    }
}

internal class ElementNotEnabledException : Exception
{
    public ElementNotEnabledException()
        : base("The element is not enabled.")
    {
        HResult = ExceptionHelpers.E_ELEMENTNOTENABLED;
    }

    public ElementNotEnabledException(string message)
        : base(message)
    {
        HResult = ExceptionHelpers.E_ELEMENTNOTENABLED;
    }

    public ElementNotEnabledException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = ExceptionHelpers.E_ELEMENTNOTENABLED;
    }
}

internal class XamlParseException : Exception
{
    public XamlParseException()
        : base("XAML parsing failed.")
    {
        HResult = ExceptionHelpers.E_XAMLPARSEFAILED;
    }

    public XamlParseException(string message)
        : base(message)
    {
        HResult = ExceptionHelpers.E_XAMLPARSEFAILED;
    }

    public XamlParseException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = ExceptionHelpers.E_XAMLPARSEFAILED;
    }
}

internal class LayoutCycleException : Exception
{
    public LayoutCycleException()
        : base("A cycle occurred while laying out the GUI.")
    {
        HResult = ExceptionHelpers.E_LAYOUTCYCLE;
    }

    public LayoutCycleException(string message)
        : base(message)
    {
        HResult = ExceptionHelpers.E_LAYOUTCYCLE;
    }

    public LayoutCycleException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = ExceptionHelpers.E_LAYOUTCYCLE;
    }
}


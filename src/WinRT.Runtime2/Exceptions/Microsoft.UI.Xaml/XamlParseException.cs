// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace Microsoft.UI.Xaml;

internal sealed class XamlParseException : Exception
{
    public XamlParseException()
        : base("XAML parsing failed.")
    {
        HResult = WellKnownErrorCodes.E_XAMLPARSEFAILED;
    }

    public XamlParseException(string message)
        : base(message)
    {
        HResult = WellKnownErrorCodes.E_XAMLPARSEFAILED;
    }

    public XamlParseException(string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = WellKnownErrorCodes.E_XAMLPARSEFAILED;
    }
}
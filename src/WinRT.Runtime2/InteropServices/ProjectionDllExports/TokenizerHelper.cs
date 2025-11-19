// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Helper class for tokenizing used by generated projections.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenizerHelper
{
    /// <summary>
    /// Gets the numeric list separator for a given provider.
    /// </summary>
    /// <param name="provider">The input provider to use for formatting.</param>
    /// <returns>The numeric list separator to use.</returns>
    public static char GetNumericListSeparator(IFormatProvider? provider)
    {
        const char CommaSeparator = ',';
        const char SemicolonSeparator = ';';

        NumberFormatInfo numberFormat = NumberFormatInfo.GetInstance(provider);

        // If the decimal separator is ',', use ';', otherwise use ','
        return
            numberFormat.NumberDecimalSeparator.Length > 0 &&
            numberFormat.NumberDecimalSeparator[0] == CommaSeparator
            ? SemicolonSeparator
            : CommaSeparator;
    }
}
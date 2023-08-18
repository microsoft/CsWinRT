using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    internal enum UiXamlMode
    {
        MicrosoftUiXaml,
        WindowsUiXaml,
    }

    internal static class OptionsHelper
    {
        public static UiXamlMode GetUiXamlMode(this AnalyzerConfigOptions options)
        {
            if (options.TryGetValue("build_property.CsWinRTUiXamlMode", out var value) && Enum.TryParse<UiXamlMode>(value, out UiXamlMode mode))
            {
                return mode;
            }

            return UiXamlMode.MicrosoftUiXaml;
        }
    }
}

using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    internal enum UiXamlMode
    {
        MicrosoftUIXaml,
        WindowsUIXaml,
    }

    internal static class OptionsHelper
    {
        public static UiXamlMode GetUiXamlMode(this AnalyzerConfigOptions options)
        {
            if (options.TryGetValue("build_property.CsWinRTUIXamlProjections", out var value) && Enum.TryParse<UiXamlMode>(value, out UiXamlMode mode))
            {
                return mode;
            }

            return UiXamlMode.MicrosoftUIXaml;
        }
    }
}

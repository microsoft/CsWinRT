using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    internal enum UIXamlProjectionsMode
    {
        MicrosoftUIXaml,
        WindowsUIXaml,
    }

    internal static class OptionsHelper
    {
        public static UIXamlProjectionsMode GetUIXamlProjectionsMode(this AnalyzerConfigOptions options)
        {
            if (options.TryGetValue("build_property.CsWinRTUIXamlProjections", out var value) && Enum.TryParse<UIXamlProjectionsMode>(value, out UIXamlProjectionsMode mode))
            {
                return mode;
            }

            return UIXamlProjectionsMode.MicrosoftUIXaml;
        }
    }
}

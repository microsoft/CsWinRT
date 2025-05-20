using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;
using static Windows.Win32.PInvoke;

namespace ComServerHelpers.Internal.Windows;

internal static class HSTRING_
{
    [SupportedOSPlatform("windows8.0")]
    public unsafe static string AsString(this HSTRING @this)
    {
        if (@this.IsNull)
        {
            return string.Empty;
        }

        uint characterCount;
        PCWSTR characters = WindowsGetStringRawBuffer(@this, &characterCount);

        return characters.ToString();
    }
}

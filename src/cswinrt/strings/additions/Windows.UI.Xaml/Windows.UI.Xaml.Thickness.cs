
namespace Windows.UI.Xaml
{
    using global::Windows.Foundation;

    partial struct Thickness
    {
        public Thickness(double uniformLength)
        {
            Left = Top = Right = Bottom = uniformLength;
        }

        public override string ToString()
        {
            return ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        internal string ToString(global::System.Globalization.CultureInfo cultureInfo)
        {
            char listSeparator = global::WindowsRuntime.InteropServices.TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  4 = 4x separator characters
            global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder(64);

            sb.Append(InternalToString(Left, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(Top, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(Right, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(Bottom, cultureInfo));
            return sb.ToString();
        }

        internal string InternalToString(double l, global::System.Globalization.CultureInfo cultureInfo)
        {
            if (double.IsNaN(l)) return "Auto";
            return Convert.ToString(l, cultureInfo);
        }
    }
}
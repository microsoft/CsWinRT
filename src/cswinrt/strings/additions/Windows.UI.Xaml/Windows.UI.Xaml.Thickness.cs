
namespace Windows.UI.Xaml
{
    using global::Windows.Foundation;

    partial struct Thickness
    {
        public Thickness(double uniformLength)
        {
            Left = Top = Right = Bottom = uniformLength;
        }

        public readonly override string ToString()
        {
            return ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        private readonly string ToString(global::System.Globalization.CultureInfo cultureInfo)
        {
            char listSeparator = global::WindowsRuntime.InteropServices.TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  4 = 4x separator characters
            DefaultInterpolatedStringHandler handler = new(0, 7, cultureInfo, stackalloc char[64]);
            InternalAddToHandler(Left, ref handler);
            handler.AppendFormatted(listSeparator);
            InternalAddToHandler(Top, ref handler);
            handler.AppendFormatted(listSeparator);
            InternalAddToHandler(Right, ref handler);
            handler.AppendFormatted(listSeparator);
            InternalAddToHandler(Bottom, ref handler);
            return handler.ToStringAndClear();
        }

        private static void InternalAddToHandler(double l, ref DefaultInterpolatedStringHandler handler)
        {
            if (double.IsNaN(l))
            {
                handler.AppendFormatted("Auto");
            }
            else
            {
                handler.AppendFormatted(l);
            }
        }
    }
}
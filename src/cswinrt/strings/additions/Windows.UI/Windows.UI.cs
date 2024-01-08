
namespace Windows.UI
{
    using global::System;
    using global::System.Globalization;

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.UniversalApiContract")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.UI.Color))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<Color, Color>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Color : IFormattable
    {
        private byte _A;
        private byte _R;
        private byte _G;
        private byte _B;

        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            Color c1 = default;

            c1.A = a;
            c1.R = r;
            c1.G = g;
            c1.B = b;

            return c1;
        }

        public byte A
        {
            get { return _A; }
            set { _A = value; }
        }

        public byte R
        {
            get { return _R; }
            set { _R = value; }
        }

        public byte G
        {
            get { return _G; }
            set { _G = value; }
        }

        public byte B
        {
            get { return _B; }
            set { _B = value; }
        }

        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null, null);
        }

        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        internal string ConvertToString(string format, IFormatProvider provider)
        {
            global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();

            if (format == null)
            {
                sb.AppendFormat(provider, "#{0:X2}", _A);
                sb.AppendFormat(provider, "{0:X2}", _R);
                sb.AppendFormat(provider, "{0:X2}", _G);
                sb.AppendFormat(provider, "{0:X2}", _B);
            }
            else
            {
                // Helper to get the numeric list separator for a given culture.
                char separator = Windows.Foundation.TokenizerHelper.GetNumericListSeparator(provider);

                sb.AppendFormat(provider,
                    "sc#{1:" + format + "}{0} {2:" + format + "}{0} {3:" + format + "}{0} {4:" + format + "}",
                    separator, _A, _R, _G, _B);
            }

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return _A.GetHashCode() ^ _R.GetHashCode() ^ _G.GetHashCode() ^ _B.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return o is Color && this == (Color)o;
        }

        public bool Equals(Color color)
        {
            return this == color;
        }

        public static bool operator ==(Color color1, Color color2)
        {
            return
                color1.R == color2.R &&
                color1.G == color2.G &&
                color1.B == color2.B &&
                color1.A == color2.A;
        }

        public static bool operator !=(Color color1, Color color2)
        {
            return (!(color1 == color2));
        }
    }
}

namespace ABI.Windows.UI
{
#if EMBED
    internal
#else
    public
#endif
    static class Color
    {
        public static string GetGuidSignature()
        {
            return "struct(Windows.UI.Color;u1;u1;u1;u1)";
        }
    }
}

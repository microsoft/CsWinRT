
namespace Windows.UI
{
    using global::System;
    using global::System.Globalization;

    partial struct Color : IFormattable
    {
        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            Color c1 = default;

            c1.A = a;
            c1.R = r;
            c1.G = g;
            c1.B = b;

            return c1;
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
                sb.AppendFormat(provider, "#{0:X2}", A);
                sb.AppendFormat(provider, "{0:X2}", R);
                sb.AppendFormat(provider, "{0:X2}", G);
                sb.AppendFormat(provider, "{0:X2}", B);
            }
            else
            {
                // Helper to get the numeric list separator for a given culture.
                char separator = global::ABI.Windows.Foundation.TokenizerHelper.GetNumericListSeparator(provider);

                sb.AppendFormat(provider,
                    "sc#{1:" + format + "}{0} {2:" + format + "}{0} {3:" + format + "}{0} {4:" + format + "}",
                    separator, A, R, G, B);
            }

            return sb.ToString();
        }
    }
}
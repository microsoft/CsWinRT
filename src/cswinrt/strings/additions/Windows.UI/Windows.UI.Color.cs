
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
            if (format == null)
            {
                DefaultInterpolatedStringHandler handler = new(1, 4, provider, stackalloc char[32]);
                handler.AppendLiteral("#");
                handler.AppendFormatted(A, "X2");
                handler.AppendFormatted(R, "X2");
                handler.AppendFormatted(G, "X2");
                handler.AppendFormatted(B, "X2");
                return handler.ToStringAndClear();
            }
            else
            {
                // Helper to get the numeric list separator for a given culture.
                char separator = global::WindowsRuntime.InteropServices.TokenizerHelper.GetNumericListSeparator(provider);

                DefaultInterpolatedStringHandler handler = new(6, 7, provider, stackalloc char[32]);
                handler.AppendLiteral("sc#");
                handler.AppendFormatted(A, format);
                handler.AppendFormatted(separator);
                handler.AppendLiteral(" ");
                handler.AppendFormatted(R, format);
                handler.AppendFormatted(separator);
                handler.AppendLiteral(" ");
                handler.AppendFormatted(G, format);
                handler.AppendFormatted(separator);
                handler.AppendLiteral(" ");
                handler.AppendFormatted(B, format);
                return handler.ToStringAndClear();
            }
        }
    }
}
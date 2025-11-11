
namespace Windows.UI.Xaml.Media
{
    using global::Windows.Foundation;

    partial struct Matrix : IFormattable
    {
        // the transform is identity by default
        private static Matrix s_identity = CreateIdentity();

        public static Matrix Identity
        {
            get
            {
                return s_identity;
            }
        }

        public readonly bool IsIdentity
        {
            get
            {
                return M11 == 1 && M12 == 0 && M21 == 0 && M22 == 1 && OffsetX == 0 && OffsetY == 0;
            }
        }

        public readonly override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        public readonly string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        readonly string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        private readonly string ConvertToString(string format, IFormatProvider provider)
        {
            if (IsIdentity)
            {
                return "Identity";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = global::WindowsRuntime.InteropServices.TokenizerHelper.GetNumericListSeparator(provider);
            DefaultInterpolatedStringHandler handler = new(0, 11, provider, stackalloc char[64]);
            handler.AppendFormatted(M11, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M12, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M21, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M22, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(OffsetX, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(OffsetY, format);
            return handler.ToStringAndClear();
        }

        public readonly Point Transform(Point point)
        {
            float x = (float)point.X;
            float y = (float)point.Y;
            this.MultiplyPoint(ref x, ref y);
            Point point2 = new Point(x, y);
            return point2;
        }

        private static Matrix CreateIdentity()
        {
            return new Matrix(1, 0,
                              0, 1,
                              0, 0);
        }

        private readonly void MultiplyPoint(ref float x, ref float y)
        {
            double num = (y * M21) + OffsetX;
            double num2 = (x * M12) + OffsetY;
            x *= (float)M11;
            x += (float)num;
            y *= (float)M22;
            y += (float)num2;
        }
    }
}
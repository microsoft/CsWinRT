
namespace Microsoft.UI.Xaml.Media
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

        public bool IsIdentity
        {
            get
            {
                return (M11 == 1 && M12 == 0 && M21 == 0 && M22 == 1 && OffsetX == 0 && OffsetY == 0);
            }
        }

        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        private string ConvertToString(string format, IFormatProvider provider)
        {
            if (IsIdentity)
            {
                return "Identity";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = global::ABI.Windows.Foundation.TokenizerHelper.GetNumericListSeparator(provider);
            return string.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}{0}{5:" + format + "}{0}{6:" + format + "}",
                                 separator,
                                 M11,
                                 M12,
                                 M21,
                                 M22,
                                 OffsetX,
                                 OffsetY);
        }

        public Point Transform(Point point)
        {
            float x = (float)point.X;
            float y = (float)point.Y;
            this.MultiplyPoint(ref x, ref y);
            Point point2 = new Point(x, y);
            return point2;
        }

        private static Matrix CreateIdentity()
        {
            Matrix matrix = default;
            matrix.SetMatrix(1, 0,
                             0, 1,
                             0, 0);
            return matrix;
        }

        private void SetMatrix(double m11, double m12,
                               double m21, double m22,
                               double offsetX, double offsetY)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }

        private void MultiplyPoint(ref float x, ref float y)
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

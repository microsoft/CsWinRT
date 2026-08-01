
namespace Microsoft.UI.Xaml
{
    using global::Windows.Foundation;

#if !CSWINRT_REFERENCE_PROJECTION
    [WindowsRuntimeType]
    [WindowsRuntimeClassName("Windows.Foundation.IReference`1<Microsoft.UI.Xaml.CornerRadius>")]
    [ABI.Microsoft.UI.Xaml.CornerRadiusComWrappersMarshaller]
#endif
    public struct CornerRadius : IEquatable<CornerRadius>
    {
        public double TopLeft;
        public double TopRight;
        public double BottomRight;
        public double BottomLeft;

        public CornerRadius(double uniformRadius)
        {
            Validate(uniformRadius, uniformRadius, uniformRadius, uniformRadius);
            TopLeft = TopRight = BottomRight = BottomLeft = uniformRadius;
        }

        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            Validate(topLeft, topRight, bottomRight, bottomLeft);

            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        private static void Validate(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            if (topLeft < 0.0 || double.IsNaN(topLeft))
                throw new ArgumentException(string.Format(SR.DirectUI_CornerRadius_InvalidMember, "TopLeft"));

            if (topRight < 0.0 || double.IsNaN(topRight))
                throw new ArgumentException(string.Format(SR.DirectUI_CornerRadius_InvalidMember, "TopRight"));

            if (bottomRight < 0.0 || double.IsNaN(bottomRight))
                throw new ArgumentException(string.Format(SR.DirectUI_CornerRadius_InvalidMember, "BottomRight"));

            if (bottomLeft < 0.0 || double.IsNaN(bottomLeft))
                throw new ArgumentException(string.Format(SR.DirectUI_CornerRadius_InvalidMember, "BottomLeft"));
        }

        public readonly override string ToString()
        {
            return ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        private readonly string ToString(global::System.Globalization.CultureInfo cultureInfo)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            char listSeparator = global::WindowsRuntime.InteropServices.TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  3 = 3x separator characters
            DefaultInterpolatedStringHandler handler = new(0, 7, cultureInfo, stackalloc char[64]);
            InternalAddToHandler(TopLeft, ref handler);
            handler.AppendFormatted(listSeparator);
            InternalAddToHandler(TopRight, ref handler);
            handler.AppendFormatted(listSeparator);
            InternalAddToHandler(BottomRight, ref handler);
            handler.AppendFormatted(listSeparator);
            InternalAddToHandler(BottomLeft, ref handler);
            return handler.ToStringAndClear();
#endif
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

        public readonly override bool Equals(object obj)
        {
            if (obj is CornerRadius cornerRadius)
            {
                return this == cornerRadius;
            }
            return false;
        }

        public readonly bool Equals(CornerRadius cornerRadius)
        {
            return this == cornerRadius;
        }

        public readonly override int GetHashCode()
        {
            return TopLeft.GetHashCode() ^ TopRight.GetHashCode() ^ BottomLeft.GetHashCode() ^ BottomRight.GetHashCode();
        }

        public static bool operator ==(CornerRadius cr1, CornerRadius cr2)
        {
            return cr1.TopLeft == cr2.TopLeft && cr1.TopRight == cr2.TopRight && cr1.BottomRight == cr2.BottomRight && cr1.BottomLeft == cr2.BottomLeft;
        }

        public static bool operator !=(CornerRadius cr1, CornerRadius cr2)
        {
            return !(cr1 == cr2);
        }
    }
}
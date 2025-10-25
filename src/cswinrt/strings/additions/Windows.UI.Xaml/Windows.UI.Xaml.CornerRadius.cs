
namespace Windows.UI.Xaml
{
    using global::Windows.Foundation;

    [WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.CornerRadius>")]
    [ABI.Windows.UI.Xaml.CornerRadiusComWrappersMarshaller]
    [StructLayout(LayoutKind.Sequential)]
    public struct CornerRadius : IEquatable<CornerRadius>
    {
        private double _TopLeft;
        private double _TopRight;
        private double _BottomRight;
        private double _BottomLeft;

        public CornerRadius(double uniformRadius)
        {
            Validate(uniformRadius, uniformRadius, uniformRadius, uniformRadius);
            _TopLeft = _TopRight = _BottomRight = _BottomLeft = uniformRadius;
        }

        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            Validate(topLeft, topRight, bottomRight, bottomLeft);

            _TopLeft = topLeft;
            _TopRight = topRight;
            _BottomRight = bottomRight;
            _BottomLeft = bottomLeft;
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

        public override string ToString()
        {
            return ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        internal string ToString(global::System.Globalization.CultureInfo cultureInfo)
        {
            char listSeparator = global::ABI.Windows.Foundation.TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  4 = 4x separator characters
            global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder(64);

            sb.Append(InternalToString(_TopLeft, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_TopRight, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_BottomRight, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_BottomLeft, cultureInfo));
            return sb.ToString();
        }

        internal string InternalToString(double l, global::System.Globalization.CultureInfo cultureInfo)
        {
            if (double.IsNaN(l)) return "Auto";
            return Convert.ToString(l, cultureInfo);
        }

        public override bool Equals(object obj)
        {
            if (obj is CornerRadius)
            {
                CornerRadius otherObj = (CornerRadius)obj;
                return (this == otherObj);
            }
            return (false);
        }

        public bool Equals(CornerRadius cornerRadius)
        {
            return (this == cornerRadius);
        }

        public override int GetHashCode()
        {
            return _TopLeft.GetHashCode() ^ _TopRight.GetHashCode() ^ _BottomLeft.GetHashCode() ^ _BottomRight.GetHashCode();
        }

        public static bool operator ==(CornerRadius cr1, CornerRadius cr2)
        {
            return cr1._TopLeft == cr2._TopLeft && cr1._TopRight == cr2._TopRight && cr1._BottomRight == cr2._BottomRight && cr1._BottomLeft == cr2._BottomLeft;
        }

        public static bool operator !=(CornerRadius cr1, CornerRadius cr2)
        {
            return (!(cr1 == cr2));
        }

        public double TopLeft
        {
            readonly get { return _TopLeft; }
            set
            {
                Validate(value, 0, 0, 0);
                _TopLeft = value;
            }
        }

        public double TopRight
        {
            readonly get { return _TopRight; }
            set
            {
                Validate(0, value, 0, 0);
                _TopRight = value;
            }
        }

        public double BottomRight
        {
            readonly get { return _BottomRight; }
            set
            {
                Validate(0, 0, value, 0);
                _BottomRight = value;
            }
        }

        public double BottomLeft
        {
            readonly get { return _BottomLeft; }
            set
            {
                Validate(0, 0, 0, value);
                _BottomLeft = value;
            }
        }
    }
}
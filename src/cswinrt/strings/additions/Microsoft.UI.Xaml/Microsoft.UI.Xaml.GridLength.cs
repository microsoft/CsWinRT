
namespace Microsoft.UI.Xaml
{
    using global::Windows.Foundation;

    [WindowsRuntimeMetadata("Microsoft.UI")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Microsoft.UI.Xaml.GridLength>")]
    [ABI.Microsoft.UI.Xaml.GridLengthComWrappersMarshaller]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct GridLength : IEquatable<GridLength>
    {
        private readonly double _unitValue;
        private readonly GridUnitType _unitType;

        private const double Default = 1.0;
        private static readonly GridLength s_auto = new(Default, GridUnitType.Auto);

        public GridLength(double pixels)
            : this(pixels, GridUnitType.Pixel)
        {
        }

        internal static bool IsFinite(double value)
        {
            return !(double.IsNaN(value) || double.IsInfinity(value));
        }

        public GridLength(double value, GridUnitType type)
        {
            if (!IsFinite(value) || value < 0.0)
            {
                throw new ArgumentException(SR.DirectUI_InvalidArgument, nameof(value));
            }
            if (type != GridUnitType.Auto && type != GridUnitType.Pixel && type != GridUnitType.Star)
            {
                throw new ArgumentException(SR.DirectUI_InvalidArgument, nameof(type));
            }

            _unitValue = (type == GridUnitType.Auto) ? Default : value;
            _unitType = type;
        }


        public readonly double Value { get { return (_unitType == GridUnitType.Auto) ? s_auto._unitValue : _unitValue; } }
        public readonly GridUnitType GridUnitType { get { return _unitType; } }


        public readonly bool IsAbsolute { get { return _unitType == GridUnitType.Pixel; } }
        public readonly bool IsAuto { get { return _unitType == GridUnitType.Auto; } }
        public readonly bool IsStar { get { return _unitType == GridUnitType.Star; } }

        public static GridLength Auto
        {
            get { return s_auto; }
        }

        public static bool operator ==(GridLength gl1, GridLength gl2)
        {
            return gl1.GridUnitType == gl2.GridUnitType
                    && gl1.Value == gl2.Value;
        }

        public static bool operator !=(GridLength gl1, GridLength gl2)
        {
            return gl1.GridUnitType != gl2.GridUnitType
                    || gl1.Value != gl2.Value;
        }

        public readonly override bool Equals(object oCompare)
        {
            if (oCompare is GridLength gridLength)
            {
                return this == gridLength;
            }

            return false;
        }

        public readonly bool Equals(GridLength gridLength)
        {
            return this == gridLength;
        }

        public readonly override int GetHashCode()
        {
            return (int)_unitValue + (int)_unitType;
        }

        public readonly override string ToString()
        {
            if (_unitType == GridUnitType.Auto)
            {
                return "Auto";
            }

            bool isStar = (_unitType == GridUnitType.Star);
            DefaultInterpolatedStringHandler handler = new(isStar ? 1 : 0, 1, global::System.Globalization.CultureInfo.InvariantCulture, stackalloc char[32]);
            handler.AppendFormatted(_unitValue);
            if (isStar)
            {
                handler.AppendLiteral("*");
            }
            return handler.ToStringAndClear();
        }
    }
}

namespace Windows.UI.Xaml
{
    using global::Windows.Foundation;

#if !CSWINRT_REFERENCE_PROJECTION
    [WindowsRuntimeType]
    [WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.UI.Xaml.GridLength>")]
    [ABI.Windows.UI.Xaml.GridLengthComWrappersMarshaller]
#endif
    public struct GridLength : IEquatable<GridLength>
    {
        public double Value;
        public GridUnitType GridUnitType;

        private const double Default = 1.0;
        private static readonly GridLength s_auto = new(Default, GridUnitType.Auto);

        public GridLength(double pixels)
            : this(pixels, GridUnitType.Pixel)
        {
        }

        public GridLength(double value, GridUnitType type)
        {
            if (type is not (GridUnitType.Auto or GridUnitType.Pixel or GridUnitType.Star))
            {
                throw new ArgumentException(SR.DirectUI_InvalidArgument, nameof(type));
            }

            Value = (type == GridUnitType.Auto) ? Default : value;
            GridUnitType = type;
        }

        public readonly bool IsAbsolute { get { return GridUnitType == GridUnitType.Pixel; } }
        public readonly bool IsAuto { get { return GridUnitType == GridUnitType.Auto; } }
        public readonly bool IsStar { get { return GridUnitType == GridUnitType.Star; } }

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
            return (int)Value + (int)GridUnitType;
        }

        public readonly override string ToString()
        {
            if (GridUnitType == GridUnitType.Auto)
            {
                return "Auto";
            }

            bool isStar = (GridUnitType == GridUnitType.Star);
            DefaultInterpolatedStringHandler handler = new(isStar ? 1 : 0, 1, global::System.Globalization.CultureInfo.InvariantCulture, stackalloc char[32]);
            handler.AppendFormatted(Value);
            if (isStar)
            {
                handler.AppendLiteral("*");
            }
            return handler.ToStringAndClear();
        }
    }
}
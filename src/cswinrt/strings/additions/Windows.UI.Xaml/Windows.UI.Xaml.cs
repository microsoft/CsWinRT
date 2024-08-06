
namespace Windows.UI.Xaml
{
    using global::Windows.Foundation;

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.UI.Xaml.CornerRadius))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<CornerRadius, CornerRadius>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct CornerRadius
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
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

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
            get { return _TopLeft; }
            set
            {
                Validate(value, 0, 0, 0);
                _TopLeft = value;
            }
        }

        public double TopRight
        {
            get { return _TopRight; }
            set
            {
                Validate(0, value, 0, 0);
                _TopRight = value;
            }
        }

        public double BottomRight
        {
            get { return _BottomRight; }
            set
            {
                Validate(0, 0, value, 0);
                _BottomRight = value;
            }
        }

        public double BottomLeft
        {
            get { return _BottomLeft; }
            set
            {
                Validate(0, 0, 0, value);
                _BottomLeft = value;
            }
        }
    }

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.EnumTypeDetails<GridUnitType>))]
#endif
#if EMBED
    internal
#else
    public
#endif
    enum GridUnitType
    {
        Auto = 0,
        Pixel,
        Star,
    }

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.UI.Xaml.GridLength))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<GridLength, GridLength>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct GridLength
    {
        private readonly double _unitValue;
        private readonly GridUnitType _unitType;

        private const double Default = 1.0;
        private static readonly GridLength s_auto = new GridLength(Default, GridUnitType.Auto);

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


        public double Value { get { return ((_unitType == GridUnitType.Auto) ? s_auto._unitValue : _unitValue); } }
        public GridUnitType GridUnitType { get { return (_unitType); } }


        public bool IsAbsolute { get { return (_unitType == GridUnitType.Pixel); } }
        public bool IsAuto { get { return (_unitType == GridUnitType.Auto); } }
        public bool IsStar { get { return (_unitType == GridUnitType.Star); } }

        public static GridLength Auto
        {
            get { return (s_auto); }
        }


        public static bool operator ==(GridLength gl1, GridLength gl2)
        {
            return (gl1.GridUnitType == gl2.GridUnitType
                    && gl1.Value == gl2.Value);
        }

        public static bool operator !=(GridLength gl1, GridLength gl2)
        {
            return (gl1.GridUnitType != gl2.GridUnitType
                    || gl1.Value != gl2.Value);
        }

        public override bool Equals(object oCompare)
        {
            if (oCompare is GridLength)
            {
                GridLength l = (GridLength)oCompare;
                return (this == l);
            }
            else
                return false;
        }

        public bool Equals(GridLength gridLength)
        {
            return (this == gridLength);
        }

        public override int GetHashCode()
        {
            return ((int)_unitValue + (int)_unitType);
        }

        public override string ToString()
        {
            return this.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        internal string ToString(global::System.Globalization.CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 12 = 1x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  2 = 2x separator characters

            if (_unitType == GridUnitType.Auto)
            {
                return "Auto";
            }
            else if (_unitType == GridUnitType.Pixel)
            {
                return Convert.ToString(_unitValue, cultureInfo);
            }
            else
            {
                return Convert.ToString(_unitValue, cultureInfo) + "*";
            }
        }
    }

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.UI.Xaml.Thickness))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<Thickness, Thickness>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Thickness
    {
        private double _Left;
        private double _Top;
        private double _Right;
        private double _Bottom;

        public Thickness(double uniformLength)
        {
            _Left = _Top = _Right = _Bottom = uniformLength;
        }

        public Thickness(double left, double top, double right, double bottom)
        {
            _Left = left;
            _Top = top;
            _Right = right;
            _Bottom = bottom;
        }

        public double Left
        {
            get { return _Left; }
            set { _Left = value; }
        }

        public double Top
        {
            get { return _Top; }
            set { _Top = value; }
        }

        public double Right
        {
            get { return _Right; }
            set { _Right = value; }
        }

        public double Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
        }

        public override string ToString()
        {
            return ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        internal string ToString(global::System.Globalization.CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  4 = 4x separator characters
            global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder(64);

            sb.Append(InternalToString(_Left, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_Top, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_Right, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_Bottom, cultureInfo));
            return sb.ToString();
        }

        internal string InternalToString(double l, global::System.Globalization.CultureInfo cultureInfo)
        {
            if (double.IsNaN(l)) return "Auto";
            return Convert.ToString(l, cultureInfo);
        }

        public override bool Equals(object obj)
        {
            if (obj is Thickness)
            {
                Thickness otherObj = (Thickness)obj;
                return (this == otherObj);
            }
            return (false);
        }

        public bool Equals(Thickness thickness)
        {
            return (this == thickness);
        }

        public override int GetHashCode()
        {
            return _Left.GetHashCode() ^ _Top.GetHashCode() ^ _Right.GetHashCode() ^ _Bottom.GetHashCode();
        }

        public static bool operator ==(Thickness t1, Thickness t2)
        {
            return t1._Left == t2._Left && t1._Top == t2._Top && t1._Right == t2._Right && t1._Bottom == t2._Bottom;
        }

        public static bool operator !=(Thickness t1, Thickness t2)
        {
            return (!(t1 == t2));
        }
    }

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.EnumTypeDetails<DurationType>))]
#endif
#if EMBED
    internal
#else
    public
#endif
    enum DurationType
    {
        Automatic,
        TimeSpan,
        Forever
    }

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.UI.Xaml.Duration))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<Duration, Duration>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Duration
    {
        private readonly TimeSpan _timeSpan;
        private DurationType _durationType;

        public Duration(TimeSpan timeSpan)
        {
            _durationType = DurationType.TimeSpan;
            _timeSpan = timeSpan;
        }

        public static implicit operator Duration(TimeSpan timeSpan)
        {
            return new Duration(timeSpan);
        }

        public static Duration operator +(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan + t2._timeSpan);
            }
            else if (t1._durationType != DurationType.Automatic && t2._durationType != DurationType.Automatic)
            {
                return Duration.Forever;
            }
            else
            {
                // Automatic + anything is Automatic
                return Duration.Automatic;
            }
        }

        public static Duration operator -(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan - t2._timeSpan);
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                return Duration.Forever;
            }
            else
            {
                return Duration.Automatic;
            }
        }

        public static bool operator ==(Duration t1, Duration t2)
        {
            return t1.Equals(t2);
        }

        public static bool operator !=(Duration t1, Duration t2)
        {
            return !(t1.Equals(t2));
        }

        public static bool operator >(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan > t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                return false;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator >=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                return false;
            }
            else
            {
                return !(t1 < t2);
            }
        }

        public static bool operator <(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan < t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        public static bool operator <=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                return false;
            }
            else
            {
                return !(t1 > t2);
            }
        }

        public static int Compare(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic)
            {
                if (t2._durationType == DurationType.Automatic)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (t2._durationType == DurationType.Automatic)
            {
                return 1;
            }
            else
            {
                if (t1 < t2)
                {
                    return -1;
                }
                else if (t1 > t2)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static Duration operator +(Duration duration)
        {
            return duration;
        }

        public bool HasTimeSpan
        {
            get
            {
                return _durationType == DurationType.TimeSpan;
            }
        }

        public static Duration Automatic
        {
            get
            {
                Duration duration = default;
                duration._durationType = DurationType.Automatic;

                return duration;
            }
        }

        public static Duration Forever
        {
            get
            {
                Duration duration = default;
                duration._durationType = DurationType.Forever;

                return duration;
            }
        }

        public TimeSpan TimeSpan
        {
            get
            {
                if (HasTimeSpan)
                {
                    return _timeSpan;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public Duration Add(Duration duration)
        {
            return this + duration;
        }

        public override bool Equals(object value)
        {
            return value is Duration && Equals((Duration)value);
        }

        public bool Equals(Duration duration)
        {
            if (HasTimeSpan)
            {
                if (duration.HasTimeSpan)
                {
                    return _timeSpan == duration._timeSpan;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _durationType == duration._durationType;
            }
        }

        public static bool Equals(Duration t1, Duration t2)
        {
            return t1.Equals(t2);
        }

        public override int GetHashCode()
        {
            if (HasTimeSpan)
            {
                return _timeSpan.GetHashCode();
            }
            else
            {
                return _durationType.GetHashCode() + 17;
            }
        }

        public Duration Subtract(Duration duration)
        {
            return this - duration;
        }

        public override string ToString()
        {
            if (HasTimeSpan)
            {
                return _timeSpan.ToString(); // "00"; //TypeDescriptor.GetConverter(_timeSpan).ConvertToString(_timeSpan);
            }
            else if (_durationType == DurationType.Forever)
            {
                return "Forever";
            }
            else // IsAutomatic
            {
                return "Automatic";
            }
        }
    }
}

namespace ABI.Windows.UI.Xaml
{
#if EMBED
    internal
#else
    public
#endif
    static class CornerRadius
    {
        public static string GetGuidSignature() => $"struct(Windows.UI.Xaml.CornerRadius;f8;f8;f8;f8)";
    }

#if EMBED
    internal
#else
    public
#endif
    static class Duration
    {
        public static string GetGuidSignature()
        {
            string timeSpanSignature = global::WinRT.GuidGenerator.GetSignature(typeof(global::System.TimeSpan));
            string durationTypeSignature = global::WinRT.GuidGenerator.GetSignature(typeof(global::Windows.UI.Xaml.DurationType));
            return $"struct(Windows.UI.Xaml.Duration;{timeSpanSignature};{durationTypeSignature})";
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class DurationType
    {
        public static string GetGuidSignature() => "enum(Windows.UI.Xaml.DurationType;i4)";
    }

#if EMBED
    internal
#else
    public
#endif
    static class GridLength
    {
        public static string GetGuidSignature()
        {
            string unitTypeSignature = global::WinRT.GuidGenerator.GetSignature(typeof(global::Windows.UI.Xaml.GridUnitType));
            return $"struct(Windows.UI.Xaml.GridLength;f8;{unitTypeSignature})";
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class GridUnitType
    {
        public static string GetGuidSignature() => "enum(Windows.UI.Xaml.GridUnitType;i4)";
    }

#if EMBED
    internal
#else
    public
#endif
    static class Thickness
    {
        public static string GetGuidSignature() => $"struct(Windows.UI.Xaml.Thickness;f8;f8;f8;f8)";
    }
}

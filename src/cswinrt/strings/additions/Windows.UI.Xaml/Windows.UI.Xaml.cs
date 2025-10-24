
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


    [WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.GridLength>")]
    [ABI.Windows.UI.Xaml.GridLengthComWrappersMarshaller]
    [StructLayout(LayoutKind.Sequential)]
    public struct GridLength : IEquatable<GridLength>
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

    partial struct Thickness
    {
        public Thickness(double uniformLength)
        {
            Left = Top = Right = Bottom = uniformLength;
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

            sb.Append(InternalToString(Left, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(Top, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(Right, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(Bottom, cultureInfo));
            return sb.ToString();
        }

        internal string InternalToString(double l, global::System.Globalization.CultureInfo cultureInfo)
        {
            if (double.IsNaN(l)) return "Auto";
            return Convert.ToString(l, cultureInfo);
        }
    }

    [WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.Duration>")]
    [ABI.Windows.UI.Xaml.GridLengthComWrappersMarshaller]
    [StructLayout(LayoutKind.Sequential)]
    public struct Duration
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
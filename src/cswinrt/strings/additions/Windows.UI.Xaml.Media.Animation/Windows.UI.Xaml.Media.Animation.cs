
namespace Windows.UI.Xaml.Media.Animation
{
    using global::Windows.Foundation;

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.UI.Xaml.Media.Animation.KeyTime))]
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct KeyTime
    {
        private TimeSpan _timeSpan;

        public static KeyTime FromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan));
            }

            KeyTime keyTime = default;

            keyTime._timeSpan = timeSpan;

            return keyTime;
        }

        public static bool Equals(KeyTime keyTime1, KeyTime keyTime2)
        {
            return (keyTime1._timeSpan == keyTime2._timeSpan);
        }

        public static bool operator ==(KeyTime keyTime1, KeyTime keyTime2)
        {
            return KeyTime.Equals(keyTime1, keyTime2);
        }

        public static bool operator !=(KeyTime keyTime1, KeyTime keyTime2)
        {
            return !KeyTime.Equals(keyTime1, keyTime2);
        }

        public bool Equals(KeyTime value)
        {
            return KeyTime.Equals(this, value);
        }

        public override bool Equals(object value)
        {
            return value is KeyTime && this == (KeyTime)value;
        }

        public override int GetHashCode()
        {
            return _timeSpan.GetHashCode();
        }

        public override string ToString()
        {
            return _timeSpan.ToString();
        }

        public static implicit operator KeyTime(TimeSpan timeSpan)
        {
            return KeyTime.FromTimeSpan(timeSpan);
        }

        public TimeSpan TimeSpan
        {
            get
            {
                return _timeSpan;
            }
        }
    }

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
#if EMBED
    internal
#else
    public
#endif
    enum RepeatBehaviorType
    {
        Count,
        Duration,
        Forever
    }

    [global::WinRT.WindowsRuntimeType("Windows.UI.Xaml")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.UI.Xaml.Media.Animation.RepeatBehavior))]
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct RepeatBehavior : IFormattable
    {
        private double _Count;
        private TimeSpan _Duration;
        private RepeatBehaviorType _Type;

        internal static bool IsFinite(double value)
        {
            return !(double.IsNaN(value) || double.IsInfinity(value));
        }

        public RepeatBehavior(double count)
        {
            if (!IsFinite(count) || count < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _Duration = new TimeSpan(0);
            _Count = count;
            _Type = RepeatBehaviorType.Count;
        }

        public RepeatBehavior(TimeSpan duration)
        {
            if (duration < new TimeSpan(0))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            _Duration = duration;
            _Count = 0.0;
            _Type = RepeatBehaviorType.Duration;
        }

        public static RepeatBehavior Forever
        {
            get
            {
                RepeatBehavior forever = default;
                forever.Type = RepeatBehaviorType.Forever;

                return forever;
            }
        }

        public bool HasCount
        {
            get
            {
                return Type == RepeatBehaviorType.Count;
            }
        }

        public bool HasDuration
        {
            get
            {
                return Type == RepeatBehaviorType.Duration;
            }
        }

        public double Count
        {
            get { return _Count; }
            set { _Count = value; }
        }

        public TimeSpan Duration
        {
            get { return _Duration; }
            set { _Duration = value; }
        }

        public RepeatBehaviorType Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public override string ToString()
        {
            return InternalToString(null, null);
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return InternalToString(null, formatProvider);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return InternalToString(format, formatProvider);
        }

        internal string InternalToString(string format, IFormatProvider formatProvider)
        {
            switch (_Type)
            {
                case RepeatBehaviorType.Forever:

                    return "Forever";

                case RepeatBehaviorType.Count:

                    global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();

                    sb.AppendFormat(
                        formatProvider,
                        "{0:" + format + "}x",
                        _Count);

                    return sb.ToString();

                case RepeatBehaviorType.Duration:

                    return _Duration.ToString();

                default:
                    return string.Empty;
            }
        }

        public override bool Equals(object value)
        {
            if (value is RepeatBehavior)
            {
                return this.Equals((RepeatBehavior)value);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(RepeatBehavior repeatBehavior)
        {
            if (_Type == repeatBehavior._Type)
            {
                return _Type switch
                {
                    RepeatBehaviorType.Forever => true,
                    RepeatBehaviorType.Count => _Count == repeatBehavior._Count,
                    RepeatBehaviorType.Duration => _Duration == repeatBehavior._Duration,
                    _ => false,
                };
            }
            else
            {
                return false;
            }
        }

        public static bool Equals(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        public override int GetHashCode()
        {
            return _Type switch
            {
                RepeatBehaviorType.Count => _Count.GetHashCode(),
                RepeatBehaviorType.Duration => _Duration.GetHashCode(),

                // We try to choose an unlikely hash code value for Forever.
                // All Forevers need to return the same hash code value.
                RepeatBehaviorType.Forever => int.MaxValue - 42,

                _ => base.GetHashCode(),
            };
        }

        public static bool operator ==(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        public static bool operator !=(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return !repeatBehavior1.Equals(repeatBehavior2);
        }
    }
}

namespace ABI.Windows.UI.Xaml.Media.Animation
{
#if EMBED
    internal
#else
    public
#endif
    static class KeyTime
    {
        public static string GetGuidSignature()
        {
            string timeSpanSignature = global::WinRT.GuidGenerator.GetSignature(typeof(global::System.TimeSpan));
            return $"struct(Windows.UI.Xaml.Media.Animation.KeyTime;{timeSpanSignature})";
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class RepeatBehavior
    {
        public static string GetGuidSignature()
        {
            string timeSpanSignature = global::WinRT.GuidGenerator.GetSignature(typeof(global::System.TimeSpan));
            return $"struct(Windows.UI.Xaml.Media.Animation.RepeatBehavior;f8;{timeSpanSignature};{RepeatBehaviorType.GetGuidSignature()})";
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class RepeatBehaviorType
    {
        public static string GetGuidSignature() => "enum(Windows.UI.Xaml.Media.Animation.RepeatBehaviorType;i4)";
    }
}


namespace Windows.UI.Xaml.Media.Animation
{
    using global::Windows.Foundation;

    [WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.Media.Animation.KeyTime>")]
    [ABI.Windows.UI.Xaml.Media.Animation.KeyTimeComWrappersMarshaller]
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyTime : IEquatable<KeyTime>
    {
        public static KeyTime FromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan));
            }

            return new KeyTime() { TimeSpan = timeSpan };
        }

        public static bool Equals(KeyTime keyTime1, KeyTime keyTime2)
        {
            return (keyTime1.TimeSpan == keyTime2.TimeSpan);
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
            return TimeSpan.GetHashCode();
        }

        public override string ToString()
        {
            return TimeSpan.ToString();
        }

        public static implicit operator KeyTime(TimeSpan timeSpan)
        {
            return KeyTime.FromTimeSpan(timeSpan);
        }

        public TimeSpan TimeSpan
        {
            readonly get; private init;
        }
    }

    [WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.Media.Animation.RepeatBehavior>")]
    [ABI.Windows.UI.Xaml.Media.Animation.RepeatBehaviorComWrappersMarshaller]
    [StructLayout(LayoutKind.Sequential)]
    public struct RepeatBehavior : IFormattable, IEquatable<RepeatBehavior>
    {
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

            Duration = new TimeSpan(0);
            Count = count;
            Type = RepeatBehaviorType.Count;
        }

        public RepeatBehavior(TimeSpan duration)
        {
            if (duration < new TimeSpan(0))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Duration = duration;
            Count = 0.0;
            Type = RepeatBehaviorType.Duration;
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
            readonly get; set;
        }

        public TimeSpan Duration
        {
            readonly get; set;
        }

        public RepeatBehaviorType Type
        {
            readonly get; set;
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
            switch (Type)
            {
                case RepeatBehaviorType.Forever:

                    return "Forever";

                case RepeatBehaviorType.Count:

                    global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();

                    sb.AppendFormat(
                        formatProvider,
                        "{0:" + format + "}x",
                        Count);

                    return sb.ToString();

                case RepeatBehaviorType.Duration:

                    return Duration.ToString();

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
            if (Type == repeatBehavior.Type)
            {
                return Type switch
                {
                    RepeatBehaviorType.Forever => true,
                    RepeatBehaviorType.Count => Count == repeatBehavior.Count,
                    RepeatBehaviorType.Duration => Duration == repeatBehavior.Duration,
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
            return Type switch
            {
                RepeatBehaviorType.Count => Count.GetHashCode(),
                RepeatBehaviorType.Duration => Duration.GetHashCode(),

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
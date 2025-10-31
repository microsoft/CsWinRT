
namespace Windows.UI.Xaml.Media.Animation
{
    using global::Windows.Foundation;

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
            ArgumentOutOfRangeException.ThrowIfLessThan(duration, new TimeSpan(0), nameof(duration));

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

        public readonly bool HasCount
        {
            get
            {
                return Type == RepeatBehaviorType.Count;
            }
        }

        public readonly bool HasDuration
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

        public readonly override string ToString()
        {
            return InternalToString(null, null);
        }

        public readonly string ToString(IFormatProvider formatProvider)
        {
            return InternalToString(null, formatProvider);
        }

        readonly string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return InternalToString(format, formatProvider);
        }

        internal readonly string InternalToString(string format, IFormatProvider formatProvider)
        {
            switch (Type)
            {
                case RepeatBehaviorType.Forever:

                    return "Forever";

                case RepeatBehaviorType.Count:

                    DefaultInterpolatedStringHandler handler = new(1, 1, formatProvider, stackalloc char[64]);
                    handler.AppendFormatted(Count, format);
                    handler.AppendLiteral("x");
                    return handler.ToStringAndClear();

                case RepeatBehaviorType.Duration:

                    return Duration.ToString();

                default:
                    return string.Empty;
            }
        }

        public readonly override bool Equals(object value)
        {
            if (value is RepeatBehavior behavior)
            {
                return Equals(behavior);
            }
            else
            {
                return false;
            }
        }

        public readonly bool Equals(RepeatBehavior repeatBehavior)
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

        public readonly override int GetHashCode()
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
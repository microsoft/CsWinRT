
namespace Microsoft.UI.Xaml
{
    using global::Windows.Foundation;

    [WindowsRuntimeMetadata("Microsoft.UI")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Microsoft.UI.Xaml.Duration>")]
#if !CSWINRT_REFERENCE_PROJECTION
    [ABI.Microsoft.UI.Xaml.DurationComWrappersMarshaller]
#endif
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Duration : IEquatable<Duration>
    {
        private readonly TimeSpan _timeSpan;
        private readonly DurationType _durationType;

        public Duration(TimeSpan timeSpan)
        {
            _durationType = DurationType.TimeSpan;
            _timeSpan = timeSpan;
        }

        private Duration(DurationType durationType)
        {
            _durationType = durationType;
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

        public readonly bool HasTimeSpan
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
                return new Duration(DurationType.Automatic);
            }
        }

        public static Duration Forever
        {
            get
            {
                return new Duration(DurationType.Forever);
            }
        }

        public readonly TimeSpan TimeSpan
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

        public readonly Duration Add(Duration duration)
        {
            return this + duration;
        }

        public readonly override bool Equals(object value)
        {
            return value is Duration duration && Equals(duration);
        }

        public readonly bool Equals(Duration duration)
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

        public readonly override int GetHashCode()
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

        public readonly Duration Subtract(Duration duration)
        {
            return this - duration;
        }

        public readonly override string ToString()
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
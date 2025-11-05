
namespace Windows.UI.Xaml.Media.Animation
{
    using global::Windows.Foundation;

    [WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
    [WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.Media.Animation.KeyTime>")]
    [ABI.Windows.UI.Xaml.Media.Animation.KeyTimeComWrappersMarshaller]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct KeyTime : IEquatable<KeyTime>
    {
        public static KeyTime FromTimeSpan(TimeSpan timeSpan)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(timeSpan, TimeSpan.Zero, nameof(timeSpan));

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

        public readonly bool Equals(KeyTime value)
        {
            return KeyTime.Equals(this, value);
        }

        public readonly override bool Equals(object value)
        {
            return value is KeyTime keyTime && this == keyTime;
        }

        public readonly override int GetHashCode()
        {
            return TimeSpan.GetHashCode();
        }

        public readonly override string ToString()
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
}
using System;
using Windows.Foundation;

namespace AuthoringTest2
{
    internal sealed class LocalEventArgs : EventArgs
    {
        public int Value { get; set; }
    }

    internal sealed class LocalHelper
    {
        public int Value { get; set; }
    }

    internal sealed class LocalStringable : IStringable
    {
        public override string ToString()
        {
            return "local helper";
        }
    }
}

// Both the ABI namespace itself and its descendants are excluded from authored metadata.
namespace ABI
{
    public struct ManagedValue : IStringable
    {
        public int Value;

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}

namespace ABI.AuthoringTest2
{
    public interface IManagedHelper
    {
        int GetValue();
    }

    public sealed class ManagedHelper : IManagedHelper
    {
        public int Value { get; set; }

        public int GetValue()
        {
            return Value;
        }
    }

    public delegate int ManagedCallback(int value);

    public enum ManagedKind
    {
        Seven = 7,
    }
}

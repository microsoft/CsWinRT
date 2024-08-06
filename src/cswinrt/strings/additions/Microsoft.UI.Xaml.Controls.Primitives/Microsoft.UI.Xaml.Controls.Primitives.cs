
namespace Microsoft.UI.Xaml.Controls.Primitives
{
    using global::Windows.Foundation;

    [global::WinRT.WindowsRuntimeType("Microsoft.UI")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Controls.Primitives.GeneratorPosition))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<GeneratorPosition, GeneratorPosition>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct GeneratorPosition
    {
        private int _index;
        private int _offset;

        public int Index { get { return _index; } set { _index = value; } }
        public int Offset { get { return _offset; } set { _offset = value; } }

        public GeneratorPosition(int index, int offset)
        {
            _index = index;
            _offset = offset;
        }

        public override int GetHashCode()
        {
            return _index.GetHashCode() + _offset.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat("GeneratorPosition (", _index.ToString(global::System.Globalization.CultureInfo.InvariantCulture), ",", _offset.ToString(global::System.Globalization.CultureInfo.InvariantCulture), ")");
        }

        public override bool Equals(object o)
        {
            if (o is GeneratorPosition)
            {
                GeneratorPosition that = (GeneratorPosition)o;
                return _index == that._index &&
                        _offset == that._offset;
            }
            return false;
        }

        public static bool operator ==(GeneratorPosition gp1, GeneratorPosition gp2)
        {
            return gp1._index == gp2._index &&
                    gp1._offset == gp2._offset;
        }

        public static bool operator !=(GeneratorPosition gp1, GeneratorPosition gp2)
        {
            return !(gp1 == gp2);
        }
    }
}

namespace ABI.Microsoft.UI.Xaml.Controls.Primitives
{
#if EMBED
    internal
#else
    public
#endif
    static class GeneratorPosition
    {
        public static string GetGuidSignature() => $"struct(Microsoft.UI.Xaml.Controls.Primitives.GeneratorPosition;i4;i4)";
    }
}

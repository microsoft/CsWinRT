
namespace Microsoft.UI.Xaml.Controls.Primitives
{
    using global::Windows.Foundation;

    partial struct GeneratorPosition
    {
        public override string ToString()
        {
            return string.Concat("GeneratorPosition (", Index.ToString(global::System.Globalization.CultureInfo.InvariantCulture), ",", Offset.ToString(global::System.Globalization.CultureInfo.InvariantCulture), ")");
        }
    }
}
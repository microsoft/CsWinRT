
namespace Windows.UI.Xaml.Controls.Primitives
{
    using global::Windows.Foundation;

    partial struct GeneratorPosition
    {
        public readonly override string ToString()
        {
            DefaultInterpolatedStringHandler handler = new(21, 2, global::System.Globalization.CultureInfo.InvariantCulture, stackalloc char[64]);
            handler.AppendLiteral("GeneratorPosition (");
            handler.AppendFormatted(Index);
            handler.AppendLiteral(",");
            handler.AppendFormatted(Offset);
            handler.AppendLiteral(")");
            return handler.ToStringAndClear();
        }
    }
}
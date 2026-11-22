using Windows.Foundation;
using Foundation = Windows.Foundation;

[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(MigrationProbe))]

int result = 0;
TypedEventHandler<object, int> handler = (sender, value) => result = value;
handler(new object(), 42);

int aliasResult = 0;
Foundation::TypedEventHandler<object, int> aliasHandler = (sender, value) => aliasResult = value;
aliasHandler(new object(), 43);

return result == 42 && aliasResult == 43 && MigrationProbe.Run() == 44 &&
    MigrationProbe.GeometryProbe.Run() && new MigrationProbe.ArrayDerivedProbe().Length == 2 &&
    new MigrationProbe.ArrayCollectionProbe { new[] { 3, 4 } }.Length == 2 ? 0 : 1;

[WinRT.GeneratedWinRTExposedType]
[WinRT.WinRTRuntimeClassName("Contoso.MigrationProbe")]
public class MigrationProbe
{
    [WinRT.DynamicWindowsRuntimeCast(typeof(System.IDisposable))]
    public static int Run()
    {
        int[] values = { 3, 4 };
        object[] objects = new object[2];
        FillObjects(objects);
        Fill(values);
        return (string)objects[0] == "x" && (string)objects[1] == "x" ? Sum(values) : -1;
    }

    public class ArrayConstructorProbe
    {
        public ArrayConstructorProbe([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values)
        {
            Length = values.Length;
        }

        public int Length { get; }
    }

    public class ArrayDerivedProbe() : ArrayConstructorProbe(new[] { 3, 4 });

    public class ArrayCollectionProbe : System.Collections.IEnumerable
    {
        public int Length { get; private set; }

        public void Add([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values)
        {
            Length = values.Length;
        }

        public System.Collections.IEnumerator GetEnumerator() => System.Array.Empty<int>().GetEnumerator();
    }

    public static class GeometryProbe
    {
        private static int calls;

        private static double Next() => ++calls / 3d;

        public static bool Run()
        {
            Point point = new(Next(), Next());
            point.X = 1d / 3;
            long value = 4611686293305294849L;
            var rounded = new Point(1d, value);
            var size = new Size(1d / 3, -1e-100);
            var rect = new Rect(double.NaN, double.NegativeInfinity, 1d / 3, -1e-100);
            bool negativeSizeRejected = false;
            try
            {
                var invalid = new Size(-1d, 2d);
            }
            catch (System.ArgumentOutOfRangeException error)
            {
                negativeSizeRejected = error.ParamName == "width";
            }

            return calls == 2 && point.X == (float)(1d / 3) && point.Y == (float)(2d / 3) &&
                rounded.Y == (float)(double)value && size.Width == (float)(1d / 3) &&
                System.BitConverter.SingleToInt32Bits(size.Height) == int.MinValue &&
                float.IsNaN(rect.X) && float.IsNegativeInfinity(rect.Y) &&
                rect.Width == (float)(1d / 3) && System.BitConverter.SingleToInt32Bits(rect.Height) == int.MinValue &&
                negativeSizeRejected;
        }
    }

    private static int Sum([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values)
    {
        int result = 0;
        foreach (int value in values)
        {
            result += value;
        }
        return result;
    }

    private static void Fill([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = 22;
        }
    }

    private static void FillObjects([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] object[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = "x";
        }
    }
}

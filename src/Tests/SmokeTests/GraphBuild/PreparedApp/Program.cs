using System;
using Windows.Data.Json;

partial class View
{
    public partial int Value { get; }

    public partial int CompileInput { get; }
}

class Program
{
    static int Main(string[] args)
    {
        View view = new();
        int expected = args.Length == 0 ? 42 : int.Parse(args[0]);

        return view.Value == expected && view.CompileInput == 7 && JsonObject.Parse("{}").Stringify() == "{}" ? 0 : 1;
    }
}

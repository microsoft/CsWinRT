using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DeterminismTarget;

public static class TestClass
{
    public static Point GetPoint() => new(1, 2);

    public static Rect GetRect() => new(0, 0, 100, 100);

    public static void Test()
    {
        List<string> a = [];
        int[] b = [];
        string[] c = [];
        string[][] d = [];
        IDisposable[] e = [];
        Action[] actions = [];
        List<Action> actionList = [];
        Dictionary<string, object> dictionary = [];

        Console.WriteLine(a);
        Console.WriteLine(b);
        Console.WriteLine(c);
        Console.WriteLine(d);
        Console.WriteLine(e);
        Console.WriteLine(actions);
        Console.WriteLine(actionList);
        Console.WriteLine(dictionary);
        Console.WriteLine(new Foo<Guid>());
        Console.WriteLine(new Foo<double>());
        Console.WriteLine(new Bar1());
        Console.WriteLine(new Bar2());

    }

}
class Foo<T> : List<T>, IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

class Bar1 : Foo<float>;
class Bar2 : Foo<object>;

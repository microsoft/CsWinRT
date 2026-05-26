using System.Collections.Generic;

namespace AuthoringTest2;

public sealed class Greeter
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    public int Add(int a, int b)
    {
        return a + b;
    }

    // Generic collection returns; exercise the merged interop's marshalling closure.
    public IList<int> GetNumbers()
    {
        return new List<int> { 1, 2, 3, 5, 8, 13 };
    }

    public IDictionary<string, int> GetCounts()
    {
        return new Dictionary<string, int>
        {
            { "alpha", 1 },
            { "beta", 2 },
            { "gamma", 3 },
        };
    }
}

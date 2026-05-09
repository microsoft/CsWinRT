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

    // Generic collection instantiations exposed across the WinRT ABI boundary.
    // The aggregator must produce a single deduplicated marshalling closure that covers
    // these instantiations alongside any others used by sibling components in the same
    // exe. If per-component marshalling stubs were generated independently, the merged
    // TypeMap registration would fail with a duplicate-key error at publish time, and
    // these calls would either fail to activate or produce wrong values at runtime.
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

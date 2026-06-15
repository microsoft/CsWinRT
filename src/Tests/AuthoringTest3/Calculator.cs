using System;
using System.Collections.Generic;

namespace AuthoringTest3;

public sealed class Calculator
{
    public static int GetDefaultFactor() => 1;
    public static int GetDefaultNumber() => 2;

    public int GetFactor() => 1;

    public int Multiply(int a, int b) => a * b;

    // Generic collection returns; exercise the merged interop's marshalling closure.
    public IList<bool> GetBools()
    {
        return new List<bool> { true, false, true };
    }

    public IList<Uri> GetUris()
    {
        return new List<Uri>
        {
            new Uri("https://example.com/a"),
            new Uri("https://example.com/b"),
        };
    }
}

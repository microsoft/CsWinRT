using System.Collections.Generic;

namespace PrivateAssetsCompiler;

public sealed class Symbol;

public readonly struct LinePositionSpan
{
    public LinePositionSpan(int value) => Value = value;

    public int Value { get; }
}

public sealed class SymbolEqualityComparer : IEqualityComparer<Symbol>
{
    public static readonly SymbolEqualityComparer Default = new();

    public bool Equals(Symbol? x, Symbol? y) => ReferenceEquals(x, y);

    public int GetHashCode(Symbol obj) => obj.GetHashCode();
}

public static class Factory
{
    public static T? Create<T>() => default;
}

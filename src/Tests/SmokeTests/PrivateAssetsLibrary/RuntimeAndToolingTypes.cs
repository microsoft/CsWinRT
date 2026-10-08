using System;
using System.Collections;
using System.Collections.Generic;
using PrivateAssetsCompiler;

namespace PrivateAssetsLibrary;

internal sealed class GeneratorLike
{
    private readonly HashSet<Symbol> symbols = new(SymbolEqualityComparer.Default);

    public object? CreateFromGenericMethod() => Factory.Create<int>();

    public object CreateCompilerValue() => new LinePositionSpan(0);

    public object CreateCompilerValues() => new LinePositionSpan[1];

    public KeyValuePair<string, Symbol>[] CreateCompilerPairs() => [];
}

internal sealed class CompilerEnumerable : IEnumerable<Symbol>
{
    public IEnumerator<Symbol> GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class RuntimeModel
{
    public string Text { get; set; } = "runtime";
}

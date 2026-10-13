# CsWinRT warning CSWINRT2023

A by-value `Span<T>` parameter of an authored Windows Runtime method is projected as a **fill array**: the caller provides storage for output, not initialized input data. Reading the buffer's existing contents is not supported.

For example, the following method reads an element before initializing it:

```csharp
using System;

public sealed class Component
{
    public void Fill(Span<int> destination)
    {
        destination[0] = destination[0] + 1; // CSWINRT2023
    }
}
```

## Recommended action

Write the output without depending on the caller's buffer contents. If the method needs input data, use a `ReadOnlySpan<T>` parameter, which is projected as a pass array.

Reading values the implementation has already initialized is valid. The analyzer recognizes common definite writes, including `Clear()`, `Fill(value)`, element assignments, and `out` element arguments:

```csharp
public void Fill(Span<int> destination)
{
    destination.Clear();
    destination[0] = 42;
    int result = destination[0]; // Valid: the implementation initialized it.
}
```

Writes through nested struct fields, inline-array elements, deconstruction targets, writable `ref` aliases, and writable ref-conditional targets do not themselves read the old value. A compound assignment or increment, however, reads the value it updates.

## Scope and limitations

The warning is enabled by default when `CsWinRTComponent` is `true`. It applies to by-value `Span<T>` parameters on methods and constructors exported from public, top-level classes, including explicit implementations of exported Windows Runtime interfaces. Classes and interfaces in `ABI.*` namespaces, nested interfaces, and explicit implementations of ordinary .NET interfaces such as `ISpanFormattable` and `IUtf8SpanFormattable` are not exported fill-array contracts.

Calls erased by the C# compiler, such as disabled `[Conditional]` calls and unimplemented optional partial methods, do not evaluate their arguments and are not reads. `nameof` expressions are also unevaluated.

The analysis is intentionally conservative. It does not perform general interprocedural or alias analysis, and only recognizes preceding writes when it can establish that they cover the read. Conditional writes and code that can invalidate that reasoning may still produce warnings.

Conversions to `ReadOnlySpan<T>`, creation of readonly element references, and taking an element's address remain potential reads for this rule. Consequently, a readonly view used only for `Length`, an address-range comparison such as `Overlaps`, or an unused `ref readonly` alias can still warn. These conservative cases are not distinguished from content-reading uses.

## Additional resources

- [Windows Runtime array parameter conventions](https://devblogs.microsoft.com/oldnewthing/20200205-00/?p=103398)
- [First-class span projections in CsWinRT 3.0](../cswinrt3.0-spec.md#first-class-spant-projections-for-t-parameters)

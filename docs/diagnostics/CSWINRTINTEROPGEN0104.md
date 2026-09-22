# CSWINRTINTEROPGEN0104: Generic member discovery depth exceeded

The interop generator limits transitive member discovery to 32 steps from a type found in the input metadata or a closed generic factory. This prevents methods such as the following from causing unbounded generic expansion:

```csharp
class Node<T>
{
    public Node<Node<T>> Next() => new();
}
```

Starting with `Node<int>`, analyzing `Next` reveals `Node<Node<int>>`, then `Node<Node<Node<int>>>`, and so on. Indirect cycles through other types and growing array arguments can have the same effect.

The generator retains discovered types but stops following their members beyond the limit, and emits this warning. Types reachable only through those unexamined members might not receive all required marshalling support. Other discovery branches continue normally.

Ordinary cycles that revisit the same closed type are deduplicated without this warning. Explicitly referenced closed types are discovery roots, regardless of their generic nesting.

If the warning affects a required interop path, simplify the expanding generic pattern rather than suppressing the warning. Setting `CsWinRTGeneratorTreatWarningsAsErrors` to `true` (or passing `--treat-warnings-as-errors true` to the tool) makes reaching the limit fail generation instead.

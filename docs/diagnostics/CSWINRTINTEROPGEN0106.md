# CSWINRTINTEROPGEN0106: Transitive generic discovery limit exceeded

The interop generator follows at most 1024 distinct generic type instantiations with members eligible for scanning in each discovery pass. This prevents branching return types such as `Node<A<T>>` and `Node<B<T>>` from creating exponentially many work items even when each signature stays below the [complexity limit](CSWINRTINTEROPGEN0105.md) and the traversal stays below the [depth limit](CSWINRTINTEROPGEN0104.md). Types with no eligible members, such as framework generics excluded by the marshalling mode that have no static initializer, do not consume this budget. Their signatures are still considered for marshalling.

Explicit signatures in the input assembly are discovery roots and do not count toward this limit. After the limit is reached, additional types found through member traversal can still be discovered, but their members are not scanned. Other discovery branches continue, and types only reachable through unscanned members may not receive all required marshalling support.

Simplify the expanding generic pattern if this warning affects a required interop path. Setting `CsWinRTGeneratorTreatWarningsAsErrors` to `true` (or passing `--treat-warnings-as-errors true` to the tool) makes reaching the limit fail generation.

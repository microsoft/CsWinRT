# CSWINRTINTEROPGEN0105: Expanded generic signature too complex

The interop generator stops following an expanded member signature when it contains more than 256 type-signature elements. This bounds exponential growth, such as repeatedly substituting `Node<Pair<T, T>>`, which can become prohibitively expensive before reaching the [member-discovery depth limit](CSWINRTINTEROPGEN0104.md).

The limit is checked before recursively visiting, hashing, or formatting the expanded signature. Already discovered types and other discovery branches are preserved, but types reachable only through the skipped signature might not receive all required marshalling support.

If the warning affects a required interop path, simplify the expanding generic pattern rather than suppressing the warning. Setting `CsWinRTGeneratorTreatWarningsAsErrors` to `true` (or passing `--treat-warnings-as-errors true` to the tool) makes reaching the limit fail generation instead.

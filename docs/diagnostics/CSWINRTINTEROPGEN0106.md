# CSWINRTINTEROPGEN0106: Transitive generic discovery limit exceeded

The interop generator reached its limit of 1024 reservations for transitive generic type or method instantiations eligible for member analysis in a discovery pass. This prevents expanding generic patterns from causing excessive build time or memory use, even when they stay within the [depth limit](CSWINRTINTEROPGEN0104.md) and [signature complexity limit](CSWINRTINTEROPGEN0105.md).

The message starts with `Generic member discovery reached` for ordinary discovery or `Supplemental generic discovery reached` for additional discovery, including interface implementations and virtual overrides.

Already discovered types and other discovery branches are preserved. However, types reachable only through members that were not analyzed might not receive all required marshalling support, which can cause runtime interop failures. Explicitly referenced types do not count toward this limit.

For discovery across assemblies, the warning can name the application assembly even when the expanding generic pattern is in a library.

If this warning affects a required interop path, simplify the expanding generic pattern rather than suppressing the warning. Explicitly referencing required closed types in an analyzed assembly can also reduce reliance on indirect discovery. Setting `CsWinRTGeneratorTreatWarningsAsErrors` to `true` (or passing `--treat-warnings-as-errors true` to the tool) makes reaching the limit fail generation.

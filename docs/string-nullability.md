# String nullability in reference projections

CsWinRT-generated reference signatures annotate strings as non-nullable, including string elements in arrays and generic types. Other generated types retain their existing nullability; for example, a projected `string[]` has non-nullable elements but an oblivious array container. Handwritten projection resources use explicit nullable-aware C# contracts, including nullable formatting inputs and async result payloads.

This affects compiler annotations only. Runtime behavior and string marshalling are unchanged.

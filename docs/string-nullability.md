# String nullability in reference projections

CsWinRT reference projections annotate strings as non-nullable, including string elements in arrays and generic types. Other types retain their existing nullability; for example, a projected `string[]` has non-nullable elements but an oblivious array container.

This affects compiler annotations only. Merged projections, runtime behavior, and string marshalling are unchanged.

; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.1.1

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CsWinRT1000 | Usage | Error | Property should have public `get` method
CsWinRT1001 | Usage | Error | Namespaces should match the assembly namespace or be child namespaces of the assembly namespace
CsWinRT1002 | Usage | Error | Namespaces cannot differ only by case 
CsWinRT1003 | Usage | Error | Component must have at least on public type 
CsWinRT1004 | Usage | Error | Public types cannot be generic
CsWinRT1005 | Usage | Error | Classes exposed to CsWinRT should be sealed
CsWinRT1006 | Usage | Error | Do not expose unsupported type
CsWinRT1007 | Usage | Error | Structs should contain at least one public field
CsWinRT1008 | Usage | Error | Interfaces should not inherit interfaces that are not valid in Windows Runtime
CsWinRT1009 | Usage | Error | Class should not have multiple constructors that take the same amount of parameters
CsWinRT1010 | Usage | Error | Methods should not use parameter names that conflict with generated parameter names
CsWinRT1011 | Usage | Error | Structs should not have private fields
CsWinRT1012 | Usage | Error | Structs should not have a constant field
CsWinRT1013 | Usage | Error | Structs should only contain basic types or other structs
CsWinRT1014 | Usage | Error | Types should not overload an operator
CsWinRT1015 | Usage | Error | Do not use `DefaultOverloadAttribute` more than once for a set of overloads
CsWinRT1016 | Usage | Error | Exactly one overload should be marked as DefaultOverload
CsWinRT1017 | Usage | Error | Array types should be one dimensional, not jagged
CsWinRT1018 | Usage | Error | Array types should be one dimensional
CsWinRT1020 | Usage | Error | Do not pass parameters by `ref`
CsWinRT1021 | Usage | Error | Array parameters should not be marked `InAttribute` or `OutAttribute` 
CsWinRT1022 | Usage | Error | Parameters should not be marked `InAttribute` or `OutAttribute`
CsWinRT1023 | Usage | Error | Array parameters should not be marked both `ReadOnlyArrayAttribute` and `WriteOnlyArrayAttribute`
CsWinRT1024 | Usage | Error | Array parameter marked `out` should not be declared `ReadOnlyArrayAttribute`
CsWinRT1025 | Usage | Error | Array parameter should be marked either `ReadOnlyArrayAttribute` or `WriteOnlyArrayAttribute`
CsWinRT1026 | Usage | Error | Non-array parameter should not be marked `ReadOnlyArrayAttribute` or `WriteOnlyArrayAttribute`
CsWinRT1027 | Usage | Error | Class incorrectly implements an interface

## Release 2.1.0

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CsWinRT1028 | Usage | Warning | Class should be marked partial
CsWinRT1029 | Usage | Warning | Class implements WinRT interfaces generated using an older version of CsWinRT.
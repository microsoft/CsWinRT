; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 3.0.0

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CSWINRT2000 | WindowsRuntime.SourceGenerator | Error | Invalid '[GeneratedCustomPropertyProvider]' target type
CSWINRT2001 | WindowsRuntime.SourceGenerator | Error | Missing 'partial' for '[GeneratedCustomPropertyProvider]' target type
CSWINRT2002 | WindowsRuntime.SourceGenerator | Error | 'ICustomPropertyProvider' interface type not available
CSWINRT2003 | WindowsRuntime.SourceGenerator | Error | Existing 'ICustomPropertyProvider' member implementation
CSWINRT2004 | WindowsRuntime.SourceGenerator | Error | Null property name in '[GeneratedCustomPropertyProvider]'
CSWINRT2005 | WindowsRuntime.SourceGenerator | Error | Null indexer type in '[GeneratedCustomPropertyProvider]'
CSWINRT2006 | WindowsRuntime.SourceGenerator | Error | Property name not found for '[GeneratedCustomPropertyProvider]'
CSWINRT2007 | WindowsRuntime.SourceGenerator | Error | Indexer type not found for '[GeneratedCustomPropertyProvider]'
CSWINRT2008 | WindowsRuntime.SourceGenerator | Error | Static indexer for '[GeneratedCustomPropertyProvider]'
CSWINRT2009 | WindowsRuntime.SourceGenerator | Warning | Cast to '[ComImport]' type not supported
CSWINRT2010 | WindowsRuntime.SourceGenerator | Warning | API contract enum type with enum cases
CSWINRT2011 | WindowsRuntime.SourceGenerator | Warning | Invalid 'ContractVersionAttribute' target for version-only constructor
CSWINRT2012 | WindowsRuntime.SourceGenerator | Warning | Invalid 'ContractVersionAttribute' target for contract-type constructor
CSWINRT2013 | WindowsRuntime.SourceGenerator | Warning | Invalid 'ContractVersionAttribute' contract type argument
CSWINRT2014 | WindowsRuntime.SourceGenerator | Warning | API contract type missing 'ContractVersionAttribute'
CSWINRT2015 | WindowsRuntime.SourceGenerator | Info | Public authored type missing version metadata
CSWINRT2016 | WindowsRuntime.SourceGenerator | Warning | Public authored type missing 'ContractVersionAttribute'
CSWINRT2017 | WindowsRuntime.SourceGenerator | Warning | Public authored type mixing '[ContractVersion]' and '[Version]'
CSWINRT2018 | WindowsRuntime.SourceGenerator | Warning | '[WindowsRuntimeNativeExposedType]' target type cannot be instantiated
CSWINRT2019 | WindowsRuntime.SourceGenerator | Warning | '[WindowsRuntimeNativeExposedType]' target type is not a projected class
CSWINRT2020 | WindowsRuntime.SourceGenerator | Warning | Duplicate '[WindowsRuntimeNativeExposedType]' target type
CSWINRT2021 | WindowsRuntime.SourceGenerator | Warning | Unsupported '[Experimental]' target for Windows Runtime metadata
CSWINRT2022 | WindowsRuntime.SourceGenerator | Warning | '[Obsolete]' on an authored API without '[Deprecated]'
CSWINRT2023 | WindowsRuntime.SourceGenerator | Error | Invalid '[WindowsRuntimeActivationFactory]' target type
CSWINRT2024 | WindowsRuntime.SourceGenerator | Error | Mismatched '[WindowsRuntimeActivationFactory]' runtime class type
CSWINRT2025 | WindowsRuntime.SourceGenerator | Error | Duplicate activation factory for a Windows Runtime class
CSWINRT2026 | WindowsRuntime.SourceGenerator | Warning | Implemented Windows Runtime class is not activatable
CSWINRT2027 | WindowsRuntime.SourceGenerator | Error | Invalid '[GeneratedWindowsRuntimeActivationFactory]' target type
CSWINRT2028 | WindowsRuntime.SourceGenerator | Error | Authored property requires a public getter
CSWINRT2029 | WindowsRuntime.SourceGenerator | Error | Invalid authored type namespace
CSWINRT2030 | WindowsRuntime.SourceGenerator | Error | Authored namespaces differ only by case
CSWINRT2031 | WindowsRuntime.SourceGenerator | Error | Generic authored declaration
CSWINRT2032 | WindowsRuntime.SourceGenerator | Error | Authored runtime class must be sealed
CSWINRT2033 | WindowsRuntime.SourceGenerator | Error | Unsupported authored signature type
CSWINRT2034 | WindowsRuntime.SourceGenerator | Error | Authored struct requires a public instance field
CSWINRT2035 | WindowsRuntime.SourceGenerator | Error | Authored constructors have the same arity
CSWINRT2036 | WindowsRuntime.SourceGenerator | Error | Non-public authored struct field
CSWINRT2037 | WindowsRuntime.SourceGenerator | Error | Constant authored struct field
CSWINRT2038 | WindowsRuntime.SourceGenerator | Error | Invalid authored struct field type
CSWINRT2039 | WindowsRuntime.SourceGenerator | Error | Authored operator is not supported
CSWINRT2040 | WindowsRuntime.SourceGenerator | Error | Multiple default authored overloads
CSWINRT2041 | WindowsRuntime.SourceGenerator | Error | Authored overload group requires a default
CSWINRT2042 | WindowsRuntime.SourceGenerator | Error | Jagged authored array
CSWINRT2043 | WindowsRuntime.SourceGenerator | Error | Multidimensional authored array
CSWINRT2044 | WindowsRuntime.SourceGenerator | Error | Unsupported authored inheritance
CSWINRT2045 | WindowsRuntime.SourceGenerator | Error | Invalid by-reference authored array
CSWINRT2046 | WindowsRuntime.SourceGenerator | Error | Invalid by-reference authored span
CSWINRT2047 | WindowsRuntime.SourceGenerator | Error | Authored parameter name conflicts with generated code
CSWINRT2048 | WindowsRuntime.SourceGenerator | Warning | Reading from a write-only 'Span<T>' parameter

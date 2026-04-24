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
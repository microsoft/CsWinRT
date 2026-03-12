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
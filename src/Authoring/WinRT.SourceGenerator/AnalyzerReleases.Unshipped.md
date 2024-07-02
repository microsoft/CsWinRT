; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CsWinRT1028 | Usage | Info | Class should be marked partial
CsWinRT1029 | Usage | Error | Missing 'UseUwp' property when referencing Windows.UI.Xaml projections
CsWinRT1030 | Usage | Error | Using 'UseUwp' when referencing WinAppSDK
CsWinRT1031 | Usage | Error | Using 'UseWinUI' when referencing Windows.UI.Xaml projections
CsWinRT1032 | Usage | Error | Using 'UseUwp' and 'UseWinUI' together 
# Microsoft.Windows.CsWinRT NuGet Package

## Overview

Please visit [Microsoft.Windows.CsWinRT](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/) for official Microsoft-signed builds of the NuGet package.

The Microsoft.Windows.CsWinRT NuGet package provides WinRT projection support for the C# language.  Adding a reference to this package causes C# projection sources to be generated for any Windows metadata referenced by the project.  For details on package use, please consult the Microsoft.Windows.CsWinRT github repo's [readme.md](https://github.com/microsoft/cswinrt/blob/master/README.md).

C#/WinRT detects Windows metadata from:
* Platform winmd files in the SDK 
* NuGet package references containing winmd files
* Other project references producing winmd files
* Raw winmd file references

For any winmd file discovered above, C#/WinRT creates reference (consuming) projection sources (*.cs files).  These sources can then be compiled into a projection assembly for client projects to reference, or compiled directly into an app.

## Details

C#/WinRT sets the following C# compiler properties:

| Property | Value | Description |
|-|-|-|
| AllowUnsafeBlocks | true | Permits compilation of generated unsafe C# blocks |

## Customizing

C#/WinRT behavior can be customized with these project properties:

| Property | Value(s) | Description |
|-|-|-|
| CsWinRTEnabled | true \| *false | Enables projection-related targets, defaults to true if CsWinRTFilters or CsWinRTParams are defined |
| CsWinRTMessageImportance | low \| *normal \| high | Sets the [importance](https://docs.microsoft.com/en-us/visualstudio/msbuild/message-task?view=vs-2017) of C++/WinRT build messages (see below) |
| CsWinRTInputs | *@(ReferencePath) | Specifies WinMD files (beyond the Windows SDK) to read metadata from |
| CsWinRTFilters | "" | Specifies the -includes and -excludes to include in projection output |
| CsWinRTParams | "" | Custom cswinrt.exe command-line parameters, replacing default settings below |
| CsWinRTComponent | true \| *false | Specifies whether to generate a component (producing) projection from project sources |
| CsWinRTWindowsMetadata | \<path\> \| "local" \| "sdk" \| *$(WindowsSDKVersion) | Specifies the source for Windows metadata |
| CsWinRTGenerateProjection | *true \| false | Indicates whether to generate and compile projection sources (true), or only to compile them (false) |
| CsWinRTGeneratedFilesDir | *"$(IntermediateOutputPath)\Generated Files" | Specifies the location for generated project source files |
\*Default value

If CsWinRTParam is not defined, the following effective values are used:
* -target $(TargetFramework)
* -input $(CsWinRTWindowsMetadata)
* -input @(CsWinRTInputs)
* -output $(CsWinRTGeneratedFilesDir)
* $(CsWinRTFilters)

## Consuming and Producing 

The C#/WinRT package can be used either to consume WinRT interfaces (CsWinRTFilters), to produce them (CsWinRTComponent).  It is also possible to combine these settings and do both.  For example, a developer might want to *produce* a library that's implemented in terms of another WinRT runtime class (*consuming* it).

## Troubleshooting

The MSBuild verbosity level maps to MSBuild message importance as follows:

| Verbosity | Importance |
|-|-|
| q[uiet] | n/a |
| m[inimal] | *high |
| n[ormal] | normal+ |
| d[etailed], diag[nostic] | low+ |
*"high" also enables the cswinrt.exe -verbosity switch

For example, if the verbosity is set to minimal, then only messages with high importance are generated.  However, if the verbosity is set to diagnostic, then all messages are generated.  

The default importance of C#/WinRT build messages is 'normal', but this can be overridden with the CsWinRTMessageImportance property to enable throttling of C#/WinRT messages independent of the overall verbosity level.

Example:
> msbuild project.vcxproj /verbosity:minimal /property:CsWinRTVerbosity=high ...

For more complex analysis of build errors, the [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/) is highly recommended.


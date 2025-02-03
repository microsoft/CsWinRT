# Microsoft.Windows.CsWinRT NuGet Package

## Overview

Please visit [Microsoft.Windows.CsWinRT](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/) for official Microsoft-signed builds of the NuGet package.

The Microsoft.Windows.CsWinRT NuGet package provides WinRT projection support for the C# language.  Adding a reference to this package causes C# projection sources to be generated for any Windows metadata referenced by the project.  For details on package use, please consult the Microsoft.Windows.CsWinRT github repo's [readme.md](https://github.com/microsoft/cswinrt/blob/master/README.md).

C#/WinRT detects Windows metadata from:
* Platform winmd files in the SDK 
* NuGet package references containing winmd files
* Other project references producing winmd files
* Raw winmd file references

For WinRT types defined in winmd files discovered above, C#/WinRT creates C# projection source files for consuming those WinRT types.  These sources can then be compiled into a projection assembly for client projects to reference, or compiled directly into an app.

## Details

C#/WinRT sets the following C# compiler properties:

| Property | Value | Description |
|-|-|-|
| AllowUnsafeBlocks | true | Permits compilation of generated unsafe C# blocks |

## Customizing

C#/WinRT behavior can be customized with these project properties:

| Property | Value(s) | Description |
|-|-|-|
| CsWinRTEnabled | *true \| false | Enables projection-related targets, can be set to false to customize build behavior |
| CsWinRTMessageImportance | low \| *normal \| high | Sets the [importance](https://docs.microsoft.com/en-us/visualstudio/msbuild/message-task?view=vs-2017) of C#/WinRT build messages (see below) |
| CsWinRTInputs | *@(ReferencePath) | Specifies WinMD files (beyond the Windows SDK) to read metadata from |
| CsWinRTExcludes | "Windows;Microsoft" | Specifies types or namespaces to exclude from projection output |
| CsWinRTExcludesPrivate | "Windows;Microsoft" | Specifies types or namespaces to exclude from projection output |
| CsWinRTIncludes | "" | Specifies types or namespaces to include in projection output |
| CsWinRTIncludesPrivate | "" | Specifies types or namespaces to include in projection output as internal types |
| CsWinRTFilters | "" | **Specifies the -includes and -excludes to include in projection output |
| CsWinRTPrivateFilters | "" | **Specifies the -includes and -excludes to include in projection output as internal types |
| CsWinRTParams | "" | ***Custom cswinrt.exe command-line parameters, replacing default settings below |
| CsWinRTComponent | true \| *false | Specifies whether to generate a component (producing) projection from project sources |
| CsWinRTEnableLogging | true \| *false | Generates a log.txt file to help with diagnosing issues with generating the metadata file and sources for a C#/WinRT authoring component |
| CsWinRTWindowsMetadata | \<path\> \| "local" \| "sdk" \| *$(WindowsSDKVersion) | Specifies the source for Windows metadata |
| CsWinRTGenerateProjection | *true \| false | Indicates whether to generate and compile projection sources (true), or only to compile them (false) |
| CsWinRTPrivateProjection | true \| *false | Indicates if a projection based on `CsWinRTIncludesPrivate` and related 'private' properties should be generated as `internal` |
| CsWinRTGeneratedFilesDir | *"$(IntermediateOutputPath)\Generated Files" | Specifies the location for generated project source files |
| CsWinRTIIDOptimizerOptOut | true \| *false | Determines whether to run the IIDOptimizer on the projection assembly |
| CsWinRTRcwFactoryFallbackGeneratorForceOptIn | true \| *false | Forces the RCW factory fallback generator to be enabled (it only runs on .exe projects by default)  |
| CsWinRTRcwFactoryFallbackGeneratorForceOptOut | true \| *false | Forces the RCW factory fallback generator to be disabled (overrides "ForceOptIn" as well)  |
| CsWinRTMergeReferencedActivationFactories | true \| *false | Makes the native `DllGetActivationFactory` exported function for AOT scenarios also forward the activation call to all referenced WinRT components, allowing them to all be merged into a single executable or shared library |
| CsWinRTAotOptimizerEnabled | *true \| false | Enables the C#/WinRT AOT optimizer |
| CsWinRTAotWarningLevel | 0 \| *1 \| 2 | Specifies the warning level used by the code fixer for trimming and AOT compat (0 - no warnings, 1 - warnings for scenarios involving non built-in types, 2 - warnings for all scenarios) |
\*Default value

**If CsWinRTFilters is not defined, the following effective value is used:
* -exclude $(CsWinRTExcludes)
* -include $(CsWinRTIncludes)

***If CsWinRTParam is not defined, the following effective value is used:
* -target $(TargetFramework)
* -input $(CsWinRTWindowsMetadata)
* -input @(CsWinRTInputs)
* -output $(CsWinRTGeneratedFilesDir)
* $(CsWinRTFilters)

## Advanced size saving options

C#/WinRT supports several feature switches to opt-out of some features available by default, like projection of built-in .NET types. These features are often required by applications, but might be unnecessary in scenarios such as minimal WinRT components authoring. In those cases, it's possible to use these properties to opt-out of any of these features, which can provide binary size savings and performance improvements.

| Property | Value(s) | Description |
|-|-|-|
| CsWinRTEnableDynamicObjectsSupport | \*true \| false | Enables support for marshalling WinRT types to dynamic objects (ie. `ExpandoObject`). Setting this to **false** allows all of `System.Linq.Expressions` to be trimmed out. |
| CsWinRTUseExceptionResourceKeys | true \| \*false | Uses minimal, non-localized exception messages to save binary size. Setting this to **true** allows stripping all of the localization .resw files that are bundled as embedded resources into C#/WinRT. |
| CsWinRTEnableDefaultCustomTypeMappings | \*true \| false | Enables all available custom type mappings of built-in .NET types by default (eg. `INotifyPropertyChanged`). Setting this to **false** allows trimming all code requires to support those features. If you only want to opt-in into a specific subset of built-in type mappings, you can enable just the ones you need via the `WinRT.Projections.Register*Mapping()` methods. Note that some type mappings will also rely on other configurable features in order to work. For instance, the type mappings for nullable primitive types will not work correctly if the `CsWinRTEnableIReferenceSupport` feature switch is set to **false**. |
| CsWinRTEnableICustomPropertyProviderSupport | \*true \| false | Enables marshalling of `ICustomPropertyProvider` objects. Setting this to **false** allows trimming all supporting code for this interface, and will make it not available on marshalled CCW types. |
| CsWinRTEnableIReferenceSupport | \*true \| false | Enables support and marshalling of `IReference<T>`, `IReferenceArray<T>` and `IPropertyValue` objects. Setting this to **false** allows trimming all supporting code for all three interfaces, and will make all of them not available on marshalled CCW types. |
| CsWinRTEnableIDynamicInterfaceCastableSupport | \*true \| false | Enables support for the [`IDynamicInterfaceCastable`](https://devblogs.microsoft.com/dotnet/improvements-in-native-code-interop-in-net-5-0/#idynamicinterfacecastable) infrastructure for RCW types. Setting this to **false** allows trimming of all related code, and will disallow casting RCW types to interface types they don't directly implement in metadata. |
\*Default value

### Windows Metadata

Windows Metadata is required for all C#/WinRT projections, and can be supplied by:
* A package reference, such as to [Microsoft.Windows.SDK.Contracts]( https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts/)
* An explicit value, supplied by $(CsWinRTWindowsMetadata)
* The default value, assigned from $(WindowsSDKVersion)

If a Windows SDK is installed, $(WindowsSDKVersion) is defined when building from a VS command prompt.

## Consuming and Producing 

The C#/WinRT package can be used both to consume WinRT types, and to produce them (via CsWinRTComponent).  It is also possible to combine these settings and do both.  For example, a developer might want to *produce* a library that's implemented in terms of another WinRT runtime class (*consuming* it).

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
> msbuild project.vcxproj /verbosity:minimal /property:CsWinRTMessageImportance=high ...

For more complex analysis of build errors, the [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/) is highly recommended.


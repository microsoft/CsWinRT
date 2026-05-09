# Microsoft.Windows.CsWinRT NuGet Package

## Overview

Please visit [Microsoft.Windows.CsWinRT](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/) for official Microsoft-signed builds of the NuGet package.

The Microsoft.Windows.CsWinRT NuGet package provides WinRT projection support for the C# language. Adding a reference to this package causes C# projection sources to be generated for any Windows metadata referenced by the project. For details on package use, please consult the Microsoft.Windows.CsWinRT github repo's [readme.md](https://github.com/microsoft/cswinrt/blob/master/README.md).

C#/WinRT detects Windows metadata from:
* Platform winmd files in the SDK
* NuGet package references containing winmd files
* Other project references producing winmd files
* Raw winmd file references

For WinRT types defined in winmd files discovered above, C#/WinRT creates C# projection source files for consuming those WinRT types. These sources can then be compiled into a projection assembly for client projects to reference, or compiled directly into an app.

## Details

C#/WinRT sets the following C# compiler properties:

| Property | Value | Description |
|-|-|-|
| AllowUnsafeBlocks | true | Permits compilation of generated unsafe C# blocks |

## Customizing

C#/WinRT behavior can be customized with these project properties:

| Property | Value(s) | Description |
|-|-|-|
| CsWinRTEnabled | *true \| false | Master switch — enables/disables all CsWinRT processing |
| CsWinRTGenerateProjection | *true \| false | Generate C# projection sources from `.winmd` metadata |
| CsWinRTGenerateReferenceProjection | true \| *false | Generate reference-only projections (for NuGet distribution) |
| CsWinRTGenerateInteropAssembly | auto | Generate interop assemblies at build time (defaults to `true` for Exe/WinExe, or Library with `PublishAot=true`) |
| CsWinRTComponent | true \| *false | Enable Windows Runtime component authoring mode |
| CsWinRTUseWindowsUIXamlProjections | true \| *false | Use UWP XAML (`Windows.UI.Xaml`) instead of WinUI (`Microsoft.UI.Xaml`) |
| CsWinRTMergeReferencedActivationFactories | true \| *false | Makes `DllGetActivationFactory` forward activation calls to all referenced WinRT components, allowing them to be merged into a single executable |
| CsWinRTIncludes | "" | Semicolon-separated namespaces to include in projection output |
| CsWinRTExcludes | "Windows;Microsoft" | Semicolon-separated namespaces to exclude from projection output |
| CsWinRTFilters | "" | **Specifies the -includes and -excludes to include in projection output |
| CsWinRTInputs | *@(ReferencePath) | Specifies WinMD files (beyond the Windows SDK) to read metadata from |
| CsWinRTParams | "" | ***Custom cswinrt.exe command-line parameters, replacing default settings below |
| CsWinRTWindowsMetadata | \<path\> \| "local" \| "sdk" \| *$(WindowsSDKVersion) | Specifies the source for Windows metadata |
| CsWinRTGeneratedFilesDir | *"$(IntermediateOutputPath)\Generated Files" | Specifies the location for generated project source files |
| CsWinRTMessageImportance | low \| *normal \| high | Sets the [importance](https://docs.microsoft.com/en-us/visualstudio/msbuild/message-task?view=vs-2017) of C#/WinRT build messages (see below) |

\*Default value

**If CsWinRTFilters is not defined, the following effective value is used:
* -exclude $(CsWinRTExcludes)
* -include $(CsWinRTIncludes)

***If CsWinRTParams is not defined, the following effective value is used:
* -target $(TargetFramework)
* -input $(CsWinRTWindowsMetadata)
* -input @(CsWinRTInputs)
* -output $(CsWinRTGeneratedFilesDir)
* $(CsWinRTFilters)

## Runtime feature switches

CsWinRT provides runtime feature switches that allow opt-in/opt-out of specific functionality. When a feature is disabled, all code behind that switch is dead-code-eliminated by the trimmer, making features fully pay-for-play. See the [AOT and trimming documentation](../docs/aot-trimming.md) for more details.

| Property | Value(s) | Description |
|-|-|-|
| CsWinRTEnableIDynamicInterfaceCastableSupport | \*true \| false | Enables `IDynamicInterfaceCastable` for casting to interfaces not listed as implemented. Setting to **false** trims all related code. |
| CsWinRTEnableManifestFreeActivation | \*true \| false | Enables activation without manifest registration. Setting to **false** trims all manifest-free activation code. |
| CsWinRTEnableXamlTypeMarshalling | \*true \| false | Enables type marshalling support for XAML scenarios. |
| CsWinRTEnableMarshalingTypeMetadataSupport | \*true \| false | Enables using `MarshalingType` as part of marshalling optimizations. |
| CsWinRTEnableMarshalingTypeValidation | true \| \*false | Validates the `MarshalingType` to determine whether the metadata matches the implementation. |

\*Default value

### Windows Metadata

Windows Metadata is required for all C#/WinRT projections, and can be supplied by:
* A package reference, such as to [Microsoft.Windows.SDK.Contracts]( https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts/)
* An explicit value, supplied by $(CsWinRTWindowsMetadata)
* The default value, assigned from $(WindowsSDKVersion)

If a Windows SDK is installed, $(WindowsSDKVersion) is defined when building from a VS command prompt.

## Consuming and Producing

The C#/WinRT package can be used both to consume WinRT types, and to produce them (via CsWinRTComponent). It is also possible to combine these settings and do both. For example, a developer might want to *produce* a library that's implemented in terms of another WinRT runtime class (*consuming* it).

## Consuming CsWinRT components from a native (C++) app

When a native (C++) `.vcxproj` references one or more managed CsWinRT components, the CsWinRT package's
`build/native/Microsoft.Windows.CsWinRT.targets` walks the component references and synthesizes a temporary
aggregator project. That aggregator runs the cswinrt projection, interop and component pipeline once across
the union of all referenced components, producing a single deduplicated `WinRT.Interop.dll` plus
`WinRT.Component.dll`, `WinRT.Projection.dll` and `WinRT.Sdk.Projection.dll`. The merged hosting bundle is
copied to the consumer's output directory next to `WinRT.Host.dll` / `WinRT.Host.Shim.dll`.

Component references are picked up from two sources, treated as equivalent inputs:

* `<ProjectReference>` items whose `CsWinRTComponent` metadata is `true`. Components built with
  `CsWinRTComponent=true` automatically expose this metadata.
* `@(CsWinRTNativeComponent)` items contributed by component-package targets files (see below).

Consumer-side properties:

| Property | Description |
|-|-|
| `CsWinRTDisableNativeComponentInterop` | Set to `true` to disable aggregator generation. |
| `CsWinRTComponentTargetFrameworkOverride` | TFM to use for the aggregator project. Required when referenced components disagree on TFM, or when no referenced component supplies a TFM. |
| `CsWinRTNativeConsumerWindowsSdkPackageVersion` | `WindowsSdkPackageVersion` to pass to the aggregator. Falls back to a value picked from any component reference, then to the SDK default. |

### Distributing a CsWinRT component for native consumers

A CsWinRT component is built with `CsWinRTComponent=true` in its `.csproj`. The build produces a managed
`{AssemblyName}.dll` plus a `.winmd`. Two distribution shapes are supported:

#### Option 1 — Ship the managed component .dll (JIT hosting, supports merged multi-component aggregation)

Native consumers reference the managed component and the consumer's own `WinRT.Host.dll`/`WinRT.Host.Shim.dll`
load it at runtime. Component packages opt in by shipping a small `build/native/{PackageId}.targets` that
contributes the component .dll to `@(CsWinRTNativeComponent)` and its `.winmd` to `@(CsWinRTInputs)`:

```xml
<!-- build/native/Contoso.Foo.targets, packaged inside the NuGet -->
<Project>
  <ItemGroup>
    <CsWinRTNativeComponent Include="$(MSBuildThisFileDirectory)..\..\lib\net10.0-windows10.0.26100.1\Contoso.Foo.dll">
      <CsWinRTComponentTargetFramework>net10.0-windows10.0.26100.1</CsWinRTComponentTargetFramework>
    </CsWinRTNativeComponent>
    <CsWinRTInputs Include="$(MSBuildThisFileDirectory)..\..\lib\net10.0-windows10.0.26100.1\Contoso.Foo.winmd" />
  </ItemGroup>
</Project>
```

The `Identity` is the path to the component .dll. `CsWinRTComponentTargetFramework` is required and is used
by the consumer to derive the aggregator's target framework.

#### Option 2 — AOT-publish the component and ship the resulting native binary

The component author runs `dotnet publish -p:PublishAot=true` on the component themselves and distributes
the resulting native dll. Native consumers load that binary directly through whatever activation mechanism
they prefer; they do not go through the aggregator.

## Troubleshooting

The MSBuild verbosity level maps to MSBuild message importance as follows:

| Verbosity | Importance |
|-|-|
| q[uiet] | n/a |
| m[inimal] | *high |
| n[ormal] | normal+ |
| d[etailed], diag[nostic] | low+ |
*"high" also enables the cswinrt.exe -verbosity switch

For example, if the verbosity is set to minimal, then only messages with high importance are generated. However, if the verbosity is set to diagnostic, then all messages are generated.

The default importance of C#/WinRT build messages is 'normal', but this can be overridden with the CsWinRTMessageImportance property to enable throttling of C#/WinRT messages independent of the overall verbosity level.

Example:
> msbuild project.vcxproj /verbosity:minimal /property:CsWinRTMessageImportance=high ...

For more complex analysis of build errors, the [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/) is highly recommended.

